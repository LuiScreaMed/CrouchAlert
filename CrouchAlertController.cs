using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.FloatingScreen;
using CrouchAlert.Data;
using CrouchAlert.Data.Wall;
using CrouchAlert.UI;
using CrouchAlert.Utils;
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using HMUI;
using UnityEngine;
using Zenject;

namespace CrouchAlert;

internal class CrouchAlertController : MonoBehaviour
{
    private PluginConfig _config = null!;

    private IReadonlyBeatmapData _readonlyBeatmapData = null!;

    private AudioClipLoader _audioClipLoader = null!;
    private AlertView _alertView = null!;
    private AudioSource _audioSource = null!;
    private AudioTimeSyncController _audioTimeSyncController = null!;
    private BeatmapLevel _beatmapLevel = null!;

    private AudioClip? _alertSoundAudioClip = null;

    private BPMChangeItem[]? _bpmChanges = null;
    private CrouchItem[]? _crouches = null;
    private int _nextCrouchIndex = -1;

    private FloatingScreen _floatingScreen = null!;

    private bool _ready = false;
    private bool _alerting = false;

    [Inject]
    public void Construct(PluginConfig config, IReadonlyBeatmapData beatmapData,
        AudioClipLoader audioClipLoader, AlertView alertView, BeatmapLevel beatmapLevel,
        AudioTimeSyncController audioTimeSyncController)
    {
        _config = config;
        _readonlyBeatmapData = beatmapData;
        _audioClipLoader = audioClipLoader;
        _alertView = alertView;
        _beatmapLevel = beatmapLevel;
        _audioTimeSyncController = audioTimeSyncController;

        Init();

        _ready = true;
        Plugin.Log.Info("CrouchAlertController Initialized");
    }

    private void Init()
    {
        if (_readonlyBeatmapData.obstaclesCount <= 0) return;
        BuildBPMChanges();
        BuildCrouches();
        if (_crouches == null) return;
        SetupUI();
        SetupAudio();
    }

    private void BuildBPMChanges()
    {
        // Turn beat map bpm change events into a bpmChanges array
        Plugin.Log.Debug($"Building BPM Changes");
        if (_readonlyBeatmapData is CustomBeatmapData customBeatmapData && customBeatmapData.version.IsVersion2() && customBeatmapData.customData.ContainsKey("_BPMChanges"))
        {
            CustomData customData = customBeatmapData.customData;
            List<object>? bpmChanges = customData.Get<List<object>>("_BPMChanges");
            if (bpmChanges == null)
            {
                _bpmChanges = [];
            }
            else
            {
                CustomData[] customBPMChanges = bpmChanges.Select(bpmChange => (CustomData)bpmChange).ToArray();
                _bpmChanges = customBPMChanges.Select(customBPMChange =>
                    new BPMChangeItem(
                        BeatTimeUtils.BeatsToDuration(
                            _beatmapLevel.beatsPerMinute,
                            customBPMChange.Get<float>("_time")
                        ),
                        customBPMChange.Get<float>("_BPM"))
                ).ToArray();
            }
        }
        else
        {
            _bpmChanges = _readonlyBeatmapData.allBeatmapDataItems.OfType<BPMChangeBeatmapEventData>()
                .Select(data => new BPMChangeItem(data.time, data.bpm)).ToArray();
        }

        if (_bpmChanges.Length == 0 || _bpmChanges[0].Time != 0)
        {
            _bpmChanges = _bpmChanges.Prepend(new BPMChangeItem(0, _beatmapLevel.beatsPerMinute)).ToArray();
        }

        Plugin.Log.Debug($"BPM Changes: {_bpmChanges.Length}");
    }

    private void BuildCrouches()
    {
        Plugin.Log.Info("Start Building Crouches");
        float startTime = Time.realtimeSinceStartup;
        BPMChangeItem[] bpmChanges = _bpmChanges!;
        int obstacleCount = 0;

        // Pre-process:
        // Trim obstacles, split long crouch obstacles into smaller obstacles
        Plugin.Log.Debug("Pre-processing obstacles");
        List<ObstacleData> processedObstacles = new();
        foreach (ObstacleData obstacle in _readonlyBeatmapData.allBeatmapDataItems.OfType<ObstacleData>())
        {
            obstacleCount++;
            ObstacleData? trimedObstacle = ObstacleUtils.Trim(obstacle);
            if (trimedObstacle == null) continue;

            if (obstacle.lineLayer != NoteLineLayer.Top)
            {
                processedObstacles.Add(obstacle);
                continue;
            }

            if (BeatTimeUtils.BeatsBetween(obstacle.time, obstacle.time + obstacle.duration, bpmChanges) <=
                _config.CrouchWallGroupingThreshold)
            {
                processedObstacles.Add(obstacle);
                continue;
            }

            List<ObstacleData> splitObstacles =
                ObstacleUtils.SplitObstacleByBeats(obstacle, _config.CrouchWallGroupingThreshold, bpmChanges);
            foreach (ObstacleData splitObstacle in splitObstacles)
            {
                processedObstacles.Add(splitObstacle);
            }
        }

        processedObstacles.Sort((obstacle1, obstacle2) => obstacle1.time.CompareTo(obstacle2.time));

        // Grouping crouch obstacles:
        // When obstacles' lines are overlapping or next to each other (not including identical lines obstacles)[1]
        // and with spawn time gap under the value of CrouchWallGroupingThreshold, group(merge) them together
        // [1]: To avoid grouping a very long crouch obstacle into a massive obstacle 
        Plugin.Log.Debug("Grouping crouch obstacles");
        List<ObstacleData> groupedObstacles = new();
        ObstacleData? groupedCrouchObstacle = null;
        float lastCrouchObstacleTime = 0f;
        float groupingThreshold = _config.CrouchWallGroupingThreshold + 0.016f; // Add 1/64 beats margin
        foreach (ObstacleData obstacle in processedObstacles)
        {
            if (obstacle.lineLayer != NoteLineLayer.Top)
            {
                groupedObstacles.Add(obstacle);
                continue;
            }

            if (
                groupedCrouchObstacle == null ||
                BeatTimeUtils.BeatsBetween(lastCrouchObstacleTime, obstacle.time, bpmChanges) > groupingThreshold ||
                !ObstacleUtils.AreOverlappingOrNextTo(groupedCrouchObstacle, obstacle)
            )
            {
                if (groupedCrouchObstacle != null)
                {
                    groupedObstacles.Add(groupedCrouchObstacle);
                }

                groupedCrouchObstacle = obstacle;
                lastCrouchObstacleTime = obstacle.time;
                
                // Plugin.Log.Debug(
                //     $"??? Not Grouped\nIn coming crouch wall: time={obstacle.time}, endTime={obstacle.time + obstacle.duration}, lineIndex={obstacle.lineIndex}, width={obstacle.width}\nGrouped crouch wall: {groupedCrouchObstacle.time}, endTime={groupedCrouchObstacle.time + groupedCrouchObstacle.duration}, lineIndex={groupedCrouchObstacle.lineIndex}, width={groupedCrouchObstacle.width}");
                continue;
            }

            // Merge into a bigger crouch obstacle
            int lineIndex = Math.Min(groupedCrouchObstacle.lineIndex, obstacle.lineIndex);
            float duration = Math.Max(
                groupedCrouchObstacle.time + groupedCrouchObstacle.duration,
                obstacle.time + obstacle.duration
            ) - groupedCrouchObstacle.time;
            int width = Math.Max(
                groupedCrouchObstacle.lineIndex + groupedCrouchObstacle.width,
                obstacle.lineIndex + obstacle.width
            ) - lineIndex;
            groupedCrouchObstacle = new ObstacleData(
                groupedCrouchObstacle.time,
                groupedCrouchObstacle.beat,
                Math.Max(groupedCrouchObstacle.endBeat, obstacle.endBeat),
                groupedCrouchObstacle.rotation,
                lineIndex,
                NoteLineLayer.Top,
                duration,
                width,
                groupedCrouchObstacle.height
            );
            lastCrouchObstacleTime = obstacle.time;
            
            // Plugin.Log.Debug(
            //     $"!!! Grouped!\nIn coming crouch wall: time={obstacle.time}, endTime={obstacle.time + obstacle.duration}, lineIndex={obstacle.lineIndex}, width={obstacle.width}\nGrouped crouch wall: {groupedCrouchObstacle.time}, endTime={groupedCrouchObstacle.time + groupedCrouchObstacle.duration}, lineIndex={groupedCrouchObstacle.lineIndex}, width={groupedCrouchObstacle.width}");
        }
        groupedObstacles.Add(groupedCrouchObstacle!);
        
        groupedObstacles.Sort((obstacle1, obstacle2) => obstacle1.time.CompareTo(obstacle2.time));
        
        // Calculating Crouches:
        Plugin.Log.Debug("Calculating crouches");
        HashSet<CrouchItem> crouches = new();
        Walls currentWalls = new Walls();
        foreach (ObstacleData obstacle in groupedObstacles)
        {
            currentWalls.ApplyNewObstacle(obstacle);
            if (NeedCrouch(currentWalls))
            {
                float alertTime = _config.UseBeatsAsAlertDurationUnit
                    ? BeatTimeUtils.TimeBeforeBeats(obstacle.time, _config.AlertDurationInBeats,
                        bpmChanges)
                    : obstacle.time - _config.AlertDurationInSeconds;
                alertTime = Math.Max(.05f, alertTime);
                crouches.Add(new CrouchItem(alertTime, obstacle.time - alertTime));
            }
        }

        Plugin.Log.Info(
            $"Calculate done (in {Convert.ToInt32((Time.realtimeSinceStartup - startTime) * 1000)} ms). {obstacleCount} obstacle(s) in total. {(crouches.Count)} crouch(es) needed");
        if (crouches.Count <= 0) return;
        crouches.Add(new CrouchItem(_audioTimeSyncController.songLength + 100f, 0f));
        if (_config.AlertFirstOnly)
        {
            Plugin.Log.Info(
                $"Alert First Only enabled, alert the first crouch only");
            _crouches = [crouches.First(), crouches.Last()];
        }
        else
        {
            _crouches = crouches.ToArray();
        }

        _nextCrouchIndex = 0;
    }

    private bool NeedCrouch(Walls walls)
    {
        return WallUtils.GetWallTypes(walls).ToTuple() switch
        {
            (WallType.Crouch, WallType.Crouch, WallType.Crouch, WallType.Crouch) or
                (WallType.Crouch, WallType.Crouch, WallType.Crouch, WallType.Block) or
                (WallType.Crouch, WallType.Crouch, WallType.Block, WallType.Crouch) or
                (WallType.Crouch, WallType.Crouch, WallType.Block, WallType.Block) or
                (WallType.Crouch, WallType.Block, WallType.Crouch, WallType.Crouch) or
                (WallType.Crouch, WallType.Block, WallType.Crouch, WallType.Block) or
                (WallType.Crouch, WallType.Block, WallType.Block, WallType.Crouch) or
                (WallType.Crouch, WallType.Block, WallType.Block, WallType.Block) or
                (WallType.Block, WallType.Crouch, WallType.Crouch, WallType.Crouch) or
                (WallType.Block, WallType.Crouch, WallType.Crouch, WallType.Block) or
                (WallType.Block, WallType.Crouch, WallType.Block, WallType.Crouch) or
                (WallType.Block, WallType.Crouch, WallType.Block, WallType.Block) or
                (WallType.Block, WallType.Block, WallType.Crouch, WallType.Crouch) or
                (WallType.Block, WallType.Block, WallType.Crouch, WallType.Block) or
                (WallType.Block, WallType.Block, WallType.Block, WallType.Crouch)
                => true,
            (_, WallType.Crouch, WallType.Crouch, _) => _config.StrictMode ? false : true,
            _ => false
        };
    }

    private void SetupUI()
    {
        Plugin.Log.Debug("Creating UI");
        _floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(200, 160), false,
            new Vector3(0f, 4f, 6.5f), Quaternion.identity);
    }

    private void SetupAudio()
    {
        Plugin.Log.Debug("Loading alert sound audio clip");
        _alertSoundAudioClip = _audioClipLoader.GetAudioClipByName(_config.AlertSoundAudioClip);
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.volume = _config.AlertSoundVolume;
    }

    private void LateUpdate()
    {
        if (!_ready || _nextCrouchIndex < 0) return;
        int oldNextCrouchIndex = _nextCrouchIndex;
        while (_nextCrouchIndex > 0 &&
               _audioTimeSyncController.songTime < _crouches![_nextCrouchIndex - 1].AlertTime)
        {
            _nextCrouchIndex--;
        }

        while (_nextCrouchIndex < _crouches!.Length - 1 &&
               _audioTimeSyncController.songTime >= _crouches![_nextCrouchIndex].AlertTime)
        {
            _nextCrouchIndex++;
        }

        if (_nextCrouchIndex > oldNextCrouchIndex &&
            _audioTimeSyncController.state == AudioTimeSyncController.State.Playing)
        {
            StartCoroutine(Alert(_crouches![oldNextCrouchIndex].Duration / _audioTimeSyncController.timeScale));
        }
    }

    private IEnumerator Alert(float duration)
    {
        if (_alerting) yield break;
        _alerting = true;

        _floatingScreen.SetRootViewController(_alertView, ViewController.AnimationType.None);

        if (_config.EnableAlertSound && _alertSoundAudioClip)
        {
            _audioSource.PlayOneShot(_alertSoundAudioClip);
        }

        IEnumerator updateCountDownText = UpdateCountDownText(duration);
        StartCoroutine(updateCountDownText);

        yield return new WaitForSeconds(duration);

        _floatingScreen.SetRootViewController(null, ViewController.AnimationType.None);
        StopCoroutine(updateCountDownText);
        _alertView.CountDownText = "";
        _alerting = false;
    }

    private IEnumerator UpdateCountDownText(float time)
    {
        float startTime = Time.time;
        while (true)
        {
            _alertView.CountDownText = Formatter.FormatFloatSecond(Math.Max(0f, time - (Time.time - startTime)));
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void OnDestroy()
    {
        Plugin.Log.Info($"{nameof(CrouchAlertController)} disposed");
    }
}