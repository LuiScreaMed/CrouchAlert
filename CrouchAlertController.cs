using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BeatSaberMarkupLanguage.FloatingScreen;
using CrouchAlert.UI;
using CrouchAlert.Utils;
using CrouchAlert.WallData;
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

    // (float time, float bpm)[]
    private (float, float)[]? _bpmChanges = null;

    // (float alertTime, float duration)[]
    private (float, float)[]? _crouches = null;
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
        CustomBeatmapData customBeatmapData = (CustomBeatmapData)beatmapData;
        Plugin.Log.Info("keys keys keys");
        foreach (string key in customBeatmapData.beatmapCustomData.Keys)
        {
            Plugin.Log.Info(key);
        }

        _audioClipLoader = audioClipLoader;
        _alertView = alertView;
        _beatmapLevel = beatmapLevel;
        _audioTimeSyncController = audioTimeSyncController;

        Init();

        _ready = true;
        Plugin.Log.Info("Initialized");
    }

    private void Init()
    {
        if (_readonlyBeatmapData.obstaclesCount <= 0) return;
        BuildBPMChanges();
        CalculateCrouches();
        if (_crouches != null)
        {
            SetupUI();
            SetupAudio();
            // RegisterEvents();
        }
    }

    private void BuildBPMChanges()
    {
        
        _bpmChanges = _readonlyBeatmapData.allBeatmapDataItems.OfType<BPMChangeBeatmapEventData>()
            .Select(data => (data.time, data.bpm)).ToArray();
        if (_bpmChanges.Length == 0 || _bpmChanges[0].Item1 != 0)
        {
            _bpmChanges = _bpmChanges.Prepend((0, _beatmapLevel.beatsPerMinute)).ToArray();
        }
    }

    private void CalculateCrouches()
    {
        Plugin.Log.Info("Reading map data, calculating crouches");
        float startTime = Time.time;
        // (float alertTime, float duration)
        HashSet<(float, float)> crouches = new();
        Walls currentWalls = new Walls();
        // Use bpm to get duration of a beat, the calculate the minimum wall duration in beats
        // (float time, float bpm)[]
        float bpm = _bpmChanges![0].Item2;
        int nextBPMChangeIndex = 0;
        // Minimum wall duration, add .005s margin
        float minimumDuration = CalculateDurationInBeats(bpm, _config.MinimumWallDurationInBeats) + 0.01f;
        // Use group crouch wall to get the first obstacle that need a crouch
        ObstacleData? groupedCrouchWallFirstObstacle = null;
        int groupedCrouchWallCount = 0;
        WallTypes lastWallTypes = new WallTypes();
        int obstacleCount = 0;
        foreach (var item in _readonlyBeatmapData.allBeatmapDataItems)
        {
            if (item is not ObstacleData obstacle) continue;
            obstacleCount++;
            if (nextBPMChangeIndex != -1 && obstacle.time >= _bpmChanges[nextBPMChangeIndex].Item1)
            {
                bpm = _bpmChanges[nextBPMChangeIndex++].Item2;
                minimumDuration = CalculateDurationInBeats(bpm, _config.MinimumWallDurationInBeats) + 0.01f;
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
                float crouchAlertTime = _config.UseBeatsAsAlertDurationUnit
                    ? CalculateAlertTimeInBeats(groupedCrouchWallFirstObstacle.time)
                    : groupedCrouchWallFirstObstacle.time - _config.AlertDurationInSeconds;
                crouchAlertTime = Math.Max(0, crouchAlertTime);
                crouches.Add((crouchAlertTime, groupedCrouchWallFirstObstacle.time - crouchAlertTime));
            }
        }

        Plugin.Log.Info(
            $"Calculating done (in {Time.time - startTime} s). {obstacleCount} obstacle(s) in total. {(crouches.Count)} crouches needed");
        if (crouches.Count > 0)
        {
            crouches.Add((_audioTimeSyncController.songLength + 100f, 0f));
            _crouches = crouches.ToArray();
            _nextCrouchIndex = 0;
        }
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
               _audioTimeSyncController.songTime < _crouches![_nextCrouchIndex - 1].Item1)
        {
            _nextCrouchIndex--;
        }

        while (_nextCrouchIndex < _crouches!.Length - 1 &&
               _audioTimeSyncController.songTime >= _crouches![_nextCrouchIndex].Item1)
        {
            _nextCrouchIndex++;
        }

        if (_nextCrouchIndex > oldNextCrouchIndex &&
            _audioTimeSyncController.state == AudioTimeSyncController.State.Playing)
        {
            StartCoroutine(Alert(_crouches![oldNextCrouchIndex].Item2 / _audioTimeSyncController.timeScale));
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

    private static float CalculateDurationInBeats(float bpm, float beats) => 60.0f / bpm * beats;

    /// <summary>
    /// Calculate alert time, handle situation when the duration in beats crosses between different BPMs
    /// </summary>
    /// <param name="time">Obstacle time</param>
    /// <returns></returns>
    private float CalculateAlertTimeInBeats(float time)
    {
        int timeBPMIndex = 0;
        float bpm = 0f;
        foreach (var (changeTime, changeBPM) in _bpmChanges!)
        {
            if (time < changeTime) break;
            timeBPMIndex++;
            bpm = changeBPM;
        }

        float remainingBeats = _config.AlertDurationInBeats;
        float duration = CalculateDurationInBeats(bpm, remainingBeats);
        while (true)
        {
            if (timeBPMIndex > 0 && (time - duration) < _bpmChanges![timeBPMIndex - 1].Item1)
            {
                timeBPMIndex--;
                float durationToPrevBPMChange = time - _bpmChanges![timeBPMIndex].Item1;
                float beats = durationToPrevBPMChange / 60 * bpm;
                remainingBeats -= beats;
                time = _bpmChanges![timeBPMIndex].Item1;
                bpm = _bpmChanges![timeBPMIndex].Item2;
                duration = CalculateDurationInBeats(bpm, remainingBeats);
            }
            else
            {
                time -= duration;
                break;
            }
        }

        return time;
    }

    private void OnDestroy()
    {
        Plugin.Log.Info($"{nameof(CrouchAlertController)} disposed");
    }
}