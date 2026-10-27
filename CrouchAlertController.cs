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
        CalculateCrouches();
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

    private void CalculateCrouches()
    {
        Plugin.Log.Debug("Calculating crouches");
        float startTime = Time.time;
        HashSet<CrouchItem> crouches = new();
        Walls currentWalls = new Walls();
        // Use bpm to get duration of a beat, the calculate the minimum wall duration in beats
        float bpm = _bpmChanges![0].BPM;
        int nextBPMChangeIndex = 0;
        // Minimum wall duration, add .005s margin
        float minimumDuration = BeatTimeUtils.BeatsToDuration(bpm, _config.MinimumWallDurationInBeats) + 0.01f;
        // Use group crouch wall to get the first obstacle that need a crouch
        ObstacleData? groupedCrouchWallFirstObstacle = null;
        int groupedCrouchWallCount = 0;
        WallTypes lastWallTypes = new WallTypes();
        int obstacleCount = 0;
        foreach (var item in _readonlyBeatmapData.allBeatmapDataItems)
        {
            if (item is not ObstacleData obstacle) continue;
            obstacleCount++;
            if (nextBPMChangeIndex != -1 && obstacle.time >= _bpmChanges[nextBPMChangeIndex].Time)
            {
                bpm = _bpmChanges[nextBPMChangeIndex++].BPM;
                minimumDuration = BeatTimeUtils.BeatsToDuration(bpm, _config.MinimumWallDurationInBeats) + 0.01f;
                if (nextBPMChangeIndex >= _bpmChanges.Length) nextBPMChangeIndex = -1;
            }

            currentWalls.ApplyNewObstacle(obstacle, minimumDuration);
            WallTypes currentWallTypes = CalculateWallTypes(currentWalls);

            // Group crouch wall operations
            for (int i = 0; i <= 3; i++)
            {
                if (currentWallTypes[i] != WallType.Crouch || currentWallTypes[i] == lastWallTypes[i]) continue;
                if (
                    groupedCrouchWallFirstObstacle == null ||
                    obstacle.time - groupedCrouchWallFirstObstacle.time >
                    groupedCrouchWallCount * minimumDuration
                )
                {
                    groupedCrouchWallFirstObstacle = obstacle;
                    groupedCrouchWallCount = 1;
                }
                else
                {
                    groupedCrouchWallCount++;
                }

                break;
            }

            if (NeedCrouch(currentWallTypes) && groupedCrouchWallFirstObstacle != null)
            {
                float alertTime = _config.UseBeatsAsAlertDurationUnit
                    ? BeatTimeUtils.TimeBeforeBeats(groupedCrouchWallFirstObstacle.time, _config.AlertDurationInBeats, _bpmChanges!)
                    : groupedCrouchWallFirstObstacle.time - _config.AlertDurationInSeconds;
                alertTime = Math.Max(.05f, alertTime);
                crouches.Add(new CrouchItem(alertTime, groupedCrouchWallFirstObstacle.time - alertTime));
            }
        }

        Plugin.Log.Info(
            $"Calculating done (in {Time.time - startTime} s). {obstacleCount} obstacle(s) in total. {(crouches.Count)} crouches needed");
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

    private WallTypes CalculateWallTypes(Walls walls)
    {
        WallTypes wallTypes = new WallTypes();

        for (int i = 0; i <= 3; i++)
        {
            WallLine line = walls[i];
            wallTypes[i] = line.Type;
        }

        return wallTypes;
    }

    private bool NeedCrouch(WallTypes wallTypes)
    {
        return wallTypes.ToTuple() switch
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