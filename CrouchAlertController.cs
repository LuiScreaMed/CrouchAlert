using System;
using System.Collections;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.FloatingScreen;
using CrouchAlert.UI;
using CrouchAlert.WallData;
using HMUI;
using UnityEngine;
using Zenject;

namespace CrouchAlert;

internal class CrouchAlertController : MonoBehaviour
{
    private PluginConfig _config = null!;
    private IReadonlyBeatmapData _readonlyBeatmapData = null!;
    private BeatmapCallbacksController _beatmapCallbacksController = null!;
    private AudioClipLoader _audioClipLoader = null!;
    private AlertView _alertView = null!;
    private AudioSource _audioSource = null!;

    private AudioClip? _remindSoundAudioClip = null;

    private HashSet<(float time, float startBeat, float endBeat, int rotation, int lineIndex, NoteLineLayer lineLayer,
        float duration, int width, int height)> _crouchWalls = null!;

    private BeatmapDataCallbackWrapper? _onBeforeObstacleSpawned;
    private FloatingScreen _floatingScreen = null!;

    private bool _ready = false;
    private bool _reminding = false;

    [Inject]
    public void Construct(PluginConfig config, IReadonlyBeatmapData beatmapData,
        BeatmapCallbacksController beatmapCallbacksController, AudioClipLoader audioClipLoader,
        AlertView alertView)
    {
        _config = config;
        _readonlyBeatmapData = beatmapData;
        _beatmapCallbacksController = beatmapCallbacksController;
        _audioClipLoader = audioClipLoader;
        _alertView = alertView;
        Plugin.Log.Info("Start processing");
        CalculateCrouches();
        if (_crouchWalls != null && _crouchWalls.Count > 0)
        {
            SetupUI();
            SetupAudio();
            RegisterWallEvents();
        }

        _ready = true;
    }

    private void CalculateCrouches()
    {
        if (_readonlyBeatmapData == null || _readonlyBeatmapData.obstaclesCount <= 0) return;

        Plugin.Log.Info("Reading map data, calculating crouches");
        float startTime = Time.time;
        _crouchWalls =
            new HashSet<(float time, float startBeat, float endBeat, int rotation, int lineIndex, NoteLineLayer
                lineLayer, float duration, int width, int height)>();
        Walls currentWalls = new Walls();
        // Use group crouch wall to get the first obstacle that need a crouch
        ObstacleData? groupCrouchWallFirstObstacle = null;
        int groupCrouchWallCount = 0;
        WallTypes lastWallTypes = new WallTypes();
        int obstacleCount = 0;
        foreach (var item in _readonlyBeatmapData.allBeatmapDataItems)
        {
            if (item is not ObstacleData obstacle) continue;
            obstacleCount++;
            Plugin.Log.Info($"Obstacle beat: {obstacle.beat}");
            // TODO: Support dynamic BPM
            // TODO: Replace "ExtendWallsByDuration" with "MinimumWallDuration", to calculate wall durations with beats and bpm
            currentWalls.ApplyNewObstacle(obstacle, _config.ExtendWallsByDuration);
            WallTypes currentWallTypes = CalculateWallTypes(currentWalls);

            // Group crouch wall operations
            for (int i = 0; i <= 3; i++)
            {
                if (currentWallTypes[i] != WallType.Crouch || currentWallTypes[i] == lastWallTypes[i]) continue;
                if (
                    groupCrouchWallFirstObstacle == null ||
                    obstacle.time - groupCrouchWallFirstObstacle.time >
                    groupCrouchWallCount * (_config.ExtendWallsByDuration)
                )
                {
                    groupCrouchWallFirstObstacle = obstacle;
                    groupCrouchWallCount = 1;
                }
                else
                {
                    groupCrouchWallCount++;
                }

                break;
            }

            if (NeedCrouch(currentWallTypes))
            {
                if (groupCrouchWallFirstObstacle != null)
                {
                    _crouchWalls.Add((groupCrouchWallFirstObstacle.time, groupCrouchWallFirstObstacle.beat,
                        groupCrouchWallFirstObstacle.endBeat, groupCrouchWallFirstObstacle.rotation,
                        groupCrouchWallFirstObstacle.lineIndex, groupCrouchWallFirstObstacle.lineLayer,
                        groupCrouchWallFirstObstacle.duration, groupCrouchWallFirstObstacle.width,
                        groupCrouchWallFirstObstacle.height));
                }
            }
        }

        Plugin.Log.Info(
            $"Calculating done (in {Time.time - startTime} s). {obstacleCount} obstacle(s) in total. {_crouchWalls.Count} crouches needed");
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
        Plugin.Log.Info("Creating UI");
        _floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(200, 160), false,
            new Vector3(0f, 4f, 6.5f), Quaternion.identity);
    }

    private void SetupAudio()
    {
        Plugin.Log.Info("Loading alert sound audio clip");
        _remindSoundAudioClip = _audioClipLoader.GetAudioClipByName(_config.AlertSoundAudioClip);
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.volume = _config.AlertSoundVolume;
    }

    private void RegisterWallEvents()
    {
        Plugin.Log.Info("Registering wall events");
        _onBeforeObstacleSpawned = _beatmapCallbacksController.AddBeatmapCallback<ObstacleData>(
            _config.AlertDuration,
            (obstacle) =>
            {
                if (!_ready) return;
                if (_crouchWalls.Contains((obstacle.time, obstacle.beat, obstacle.endBeat, obstacle.rotation,
                        obstacle.lineIndex, obstacle.lineLayer, obstacle.duration, obstacle.width, obstacle.height)))
                {
                    StartCoroutine(Remind());
                }
            }
        );
    }

    private IEnumerator Remind()
    {
        if (_reminding) yield break;
        _reminding = true;

        _floatingScreen.SetRootViewController(_alertView, ViewController.AnimationType.None);

        if (_config.EnableAlertSound && _remindSoundAudioClip)
        {
            _audioSource.PlayOneShot(_remindSoundAudioClip);
        }

        IEnumerator updateCountDownText = UpdateCountDownText(_config.AlertDuration);
        StartCoroutine(updateCountDownText);

        yield return new WaitForSeconds(_config.AlertDuration - 0.4f);

        _floatingScreen.SetRootViewController(null, ViewController.AnimationType.Out);

        yield return new WaitForSeconds(0.4f);

        StopCoroutine(updateCountDownText);
        _alertView.CountDownText = "";
        _reminding = false;
    }

    private IEnumerator UpdateCountDownText(float time)
    {
        float startTime = Time.time;
        while (true)
        {
            _alertView.CountDownText = Utils.FormatFloatSecond(Math.Max(0f, time - (Time.time - startTime)));
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void OnDestroy()
    {
        _beatmapCallbacksController.RemoveBeatmapCallback(_onBeforeObstacleSpawned);
        Plugin.Log.Debug($"{nameof(CrouchAlertController)} disposed");
    }
}