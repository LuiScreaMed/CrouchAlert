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
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

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
        Plugin.Log.Debug($"Building BPM Changes");
        CustomBeatmapData customBeatmapData = (_readonlyBeatmapData as CustomBeatmapData)!;
        if (customBeatmapData.version.IsVersion2() && customBeatmapData.customData.ContainsKey("_BPMChanges"))
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
        float startTime = Time.time;
        BPMChangeItem[] bpmChanges = _bpmChanges!;
        // Group up walls
        Plugin.Log.Debug("Grouping crouch obstacles");
        List<ObstacleData> groupedObstacles = new();
        ObstacleData? groupedCrouchObstacle = null;
        float lastCrouchObstacleTime = 0f;
        float groupingThreshold = _config.CrouchWallGroupingThreshold + 0.016f; // Add 1/64 beats margin
        int obstacleCount = 0;
        foreach (ObstacleData obstacle in _readonlyBeatmapData.allBeatmapDataItems.OfType<ObstacleData>())
        {
            obstacleCount++;
            if (obstacle.lineLayer != NoteLineLayer.Top)
            {
                groupedObstacles.Add(obstacle);
                continue;
            }

            // Check if the obstacles' spawn time gap is under the threshold,
            // and if the obstacle overlapping or next to grouped obstacle
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
                continue;
            }

            // Merge to a bigger crouch obstacle
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
        }

        groupedObstacles.Sort((obstacle1, obstacle2) => obstacle1.time.CompareTo(obstacle2.time));

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
            $"Calculate done (in {Time.time - startTime} s). {obstacleCount} obstacle(s) in total. {(crouches.Count)} crouch(es) needed");
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
            (WallType.Crouch, WallType.Block, WallType.Block, WallType.Block) or
                (WallType.Crouch, WallType.Crouch, WallType.Block, WallType.Block) or
                (WallType.Crouch, WallType.Crouch, WallType.Crouch, WallType.Block) or
                (WallType.Crouch, WallType.Crouch, WallType.Crouch, WallType.Crouch) or
                (WallType.Block, WallType.Crouch, WallType.Crouch, WallType.Crouch) or
                (WallType.Block, WallType.Block, WallType.Crouch, WallType.Crouch) or
                (WallType.Block, WallType.Block, WallType.Block, WallType.Crouch) or
                (WallType.Block, WallType.Crouch, WallType.Crouch, WallType.Block) or
                (WallType.Block, WallType.Crouch, WallType.Block, WallType.Block) or
                (WallType.Block, WallType.Block, WallType.Crouch, WallType.Block)
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