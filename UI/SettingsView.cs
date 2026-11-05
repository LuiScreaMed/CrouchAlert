using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
using CrouchAlert.Data.AlertUI;
using CrouchAlert.Utils;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CrouchAlert.UI;

[HotReload(RelativePathToLayout = @"settings.bsml")]
[ViewDefinition("CrouchAlert.UI.settings.bsml")]
internal class SettingsView : BSMLAutomaticViewController
{
    private PluginConfig _config = null!;
    private AudioClipLoader _audioClipLoader = null!;

    private AudioSource? _previewAudioSource = null;

    // Alert Duration
    private readonly List<string> _alertDurationUnitOptions = new()
    {
        LocalizedTexts.AlertDurationUnit.Seconds,
        LocalizedTexts.AlertDurationUnit.Beats
    };

    // Crouch Wall Grouping Threshold
    private readonly Dictionary<float, string> _crouchWallGroupingThresholdMap = new()
    {
        { .25f, LocalizedTexts.CrouchWallGroupingThresholdOption.OneQuarter },
        { .5f, LocalizedTexts.CrouchWallGroupingThresholdOption.OneHalf },
        { 1f, LocalizedTexts.CrouchWallGroupingThresholdOption.One }
    };

    // Alert Audio
    private readonly List<string> _audioClipOptions = new();
    private readonly List<string> _loadedAudioClips = new();

    // Alert UI
    private readonly Dictionary<AlertUIPlacement, string> _alertUIPlacementMap = new()
    {
        { AlertUIPlacement.Floor, LocalizedTexts.AlertUI.Placement.Floor },
        { AlertUIPlacement.Above, LocalizedTexts.AlertUI.Placement.Above },
        { AlertUIPlacement.Custom, LocalizedTexts.AlertUI.Placement.Custom },
    };

    private readonly Dictionary<AlertUICountdownDisplayType, string> _alertUICountdownDisplayTypeMap = new()
    {
        { AlertUICountdownDisplayType.ProgressBar, LocalizedTexts.AlertUI.CountdownDisplayType.ProgressBar },
        { AlertUICountdownDisplayType.Timer, LocalizedTexts.AlertUI.CountdownDisplayType.Timer },
    };

    private bool _isInUITab = false;

    private FloatingScreen? _floatingScreen = null;
    private AlertView? _alertView = null;
    private bool _isAlertUIGrabbed = false;

    [Inject]
    public void Construct(PluginConfig config, AudioClipLoader audioClipLoader)
    {
        _config = config;

        // Init crouch wall grouping threshold dropdown
        if (!_crouchWallGroupingThresholdMap.ContainsKey(_config.CrouchWallGroupingThreshold))
        {
            _crouchWallGroupingThresholdMap.Add(_config.CrouchWallGroupingThreshold,
                LocalizedTexts.CrouchWallGroupingThresholdOption.N(_config.CrouchWallGroupingThreshold));
        }

        // Init audio clip dropdown
        _audioClipLoader = audioClipLoader;
        _audioClipOptions.Add(LocalizedTexts.AudioClipNone);
        foreach (string audioClipName in _audioClipLoader.AudioClipNames)
        {
            _audioClipOptions.Add(audioClipName);
            _loadedAudioClips.Add(audioClipName);
        }

        if (_config.AlertSoundAudioClip != string.Empty && _config.AlertSoundAudioClip != LocalizedTexts.AudioClipNone)
        {
            if (!_audioClipOptions.Contains(_config.AlertSoundAudioClip))
                _audioClipOptions.Add(_config.AlertSoundAudioClip);
        }
    }

    [UIAction("#post-parse")]
    public void PostParse()
    {
        UpdateAllUI();
    }

    #region General

    private bool EnableValue
    {
        get => _config.Enable;
        set
        {
            _config.Enable = value;
            UpdateAllUI();
        }
    }

    [UIComponent("alertFirstOnlyToggle")] private readonly ToggleSetting? _alertFirstOnlyToggle = null;

    private bool AlertFirstOnlyValue
    {
        get => _config.AlertFirstOnly;
        set { _config.AlertFirstOnly = value; }
    }

    [UIComponent("strictModeToggle")] private readonly ToggleSetting? _strictModeToggle = null;

    private bool StrictModeValue
    {
        get => _config.StrictMode;
        set => _config.StrictMode = value;
    }

    [UIComponent("alertDurationUnitListSetting")]
    private readonly ListSetting? _alertDurationUnitListSetting = null;

    private List<string> AlertDurationUnitOptions => _alertDurationUnitOptions;

    private string AlertDurationUnitValue
    {
        get => AlertDurationUnitOptions[Convert.ToInt16(_config.UseBeatsAsAlertDurationUnit)];
        set
        {
            _config.UseBeatsAsAlertDurationUnit = Convert.ToBoolean(AlertDurationUnitOptions.IndexOf(value));
            UpdateGeneralTabUI();
        }
    }

    [UIComponent("alertDurationInBeatsSlider")]
    private readonly SliderSetting? _alertDurationInBeatsSlider = null;

    private float AlertDurationInBeatsValue
    {
        get => _config.AlertDurationInBeats;
        set { _config.AlertDurationInBeats = value; }
    }

    [UIComponent("alertDurationInSecondsSlider")]
    private readonly SliderSetting? _alertDurationInSecondsSlider = null;

    private float AlertDurationInSecondsValue
    {
        get => _config.AlertDurationInSeconds;
        set { _config.AlertDurationInSeconds = value; }
    }

    [UIComponent("crouchWallGroupingThresholdDropdown")]
    private readonly DropDownListSetting? _crouchWallGroupingThresholdDropdown = null;

    private List<string> CrouchWallGroupingThresholdOptions => _crouchWallGroupingThresholdMap.Values.ToList();

    private string CrouchWallGroupingThresholdValue
    {
        get => _crouchWallGroupingThresholdMap[_config.CrouchWallGroupingThreshold];
        set
        {
            _config.CrouchWallGroupingThreshold =
                _crouchWallGroupingThresholdMap.Keys.First(key => _crouchWallGroupingThresholdMap[key] == value);
        }
    }

    private void UpdateGeneralTabUI()
    {
        _alertFirstOnlyToggle?.Interactable = EnableValue;
        _strictModeToggle?.Interactable = EnableValue;
        _alertDurationUnitListSetting?.Interactable = EnableValue;
        _alertDurationInBeatsSlider?.Interactable = EnableValue;
        _alertDurationInBeatsSlider?.gameObject.SetActive(_config.UseBeatsAsAlertDurationUnit);
        _alertDurationInSecondsSlider?.Interactable = EnableValue;
        _alertDurationInSecondsSlider?.gameObject.SetActive(!_config.UseBeatsAsAlertDurationUnit);
        _crouchWallGroupingThresholdDropdown?.Interactable = EnableValue;
        _crouchWallGroupingThresholdDropdown?.UpdateChoices();
    }

    #endregion

    #region Audio

    [UIComponent("enableAlertSoundToggle")]
    private readonly ToggleSetting? _alertSoundToggle = null;

    private bool EnableAlertSoundValue
    {
        get => _config.EnableAlertSound;
        set
        {
            _config.EnableAlertSound = value;
            UpdateAudioTabUI();
        }
    }

    [UIComponent("alertSoundAudioClipDropdown")]
    private readonly DropDownListSetting? _audioClipDropdown = null;

    private List<string> AlertSoundAudioClipOptions => _audioClipOptions;

    private string AlertSoundAudioClipValue
    {
        get => _config.AlertSoundAudioClip == string.Empty ? LocalizedTexts.AudioClipNone : _config.AlertSoundAudioClip;
        set
        {
            if (AlertSoundAudioClipNotFound)
            {
                int removeIndex = _audioClipOptions.IndexOf(AlertSoundAudioClipValue);
                _audioClipOptions.RemoveAt(removeIndex);
            }

            _config.AlertSoundAudioClip = value == LocalizedTexts.AudioClipNone ? string.Empty : value;
            UpdateAudioTabUI();
        }
    }

    [UIComponent("alertSoundAudioClipNotFoundText")]
    private readonly TextMeshProUGUI? _audioNotFoundText = null;

    private bool AlertSoundAudioClipNotFound => _config.AlertSoundAudioClip != string.Empty &&
                                                !_loadedAudioClips.Contains(_config.AlertSoundAudioClip);

    private bool AlertSoundAudioClipValid => _config.AlertSoundAudioClip != string.Empty &&
                                             _loadedAudioClips.Contains(_config.AlertSoundAudioClip);


    [UIComponent("alertSoundVolumeSlider")]
    private readonly SliderSetting? _volumeSlider = null;

    private float AlertSoundVolumeValue
    {
        get => _config.AlertSoundVolume;
        set => _config.AlertSoundVolume = value;
    }

    [UIComponent("previewAudioWrapper")] private readonly LayoutElement? _previewAudioWrapper = null;

    [UIAction("PreviewAudio")]
    private void PreviewAudio()
    {
        AudioClip? audioClip = _audioClipLoader.GetAudioClipByName(_config.AlertSoundAudioClip);
        if (audioClip == null) return;
        if (_previewAudioSource == null)
        {
            _previewAudioSource = gameObject.AddComponent<AudioSource>();
        }

        _previewAudioSource.volume = _config.AlertSoundVolume;
        _previewAudioSource.PlayOneShot(audioClip);
    }

    private void UpdateAudioTabUI()
    {
        _alertSoundToggle?.Interactable = EnableValue;
        bool interactable = EnableValue && EnableAlertSoundValue;
        _audioClipDropdown?.Interactable = interactable;
        _volumeSlider?.Interactable = interactable;
        _previewAudioWrapper?.gameObject.SetActive(interactable && AlertSoundAudioClipValid);
        _audioNotFoundText?.rectTransform.gameObject.SetActive(EnableAlertSoundValue && AlertSoundAudioClipNotFound);
        _audioClipDropdown?.UpdateChoices();
    }

    #endregion

    #region UI

    private Coroutine? _countdownCoroutine = null;

    [UIAction("OnTabSelected")]
    private void OnTabSelected(SegmentedControl _, int tagIndex)
    {
        if (tagIndex == 2 && !_isInUITab)
        {
            _isInUITab = true;
            CreateAlertUI();
        }
        else if (tagIndex != 2 && _isInUITab)
        {
            _isInUITab = false;
            DestroyAlertUI();
        }
    }

    [UIComponent("alertUICountdownDisplayTypeListSetting")]
    private readonly ListSetting? _alertUICountdownDisplayTypeListSetting = null;

    private List<string> AlertUICountdownDisplayTypeOptions => _alertUICountdownDisplayTypeMap.Values.ToList();

    private string AlertUICountdownDisplayTypeValue
    {
        get => _alertUICountdownDisplayTypeMap[_config.AlertUICountdownDisplayType];
        set
        {
            _config.AlertUICountdownDisplayType =
                _alertUICountdownDisplayTypeMap.Keys.First(key => _alertUICountdownDisplayTypeMap[key] == value);
            UpdateAlertUICountdownType();
        }
    }

    [UIComponent("alertUIPlacementDropdown")]
    private readonly DropDownListSetting? _alertUIPlacementDropdown = null;

    private string AlertUIPlacementValue
    {
        get => _alertUIPlacementMap[_config.AlertUIPlacement];
        set
        {
            _config.AlertUIPlacement = _alertUIPlacementMap.Keys.First(key => _alertUIPlacementMap[key] == value);
            UpdateAlertUITransform();
            UpdateUITabUI();
        }
    }

    [UIComponent("alertUITransparencySlider")]
    private readonly SliderSetting? _alertUITransparencySlider = null;

    private float AlertUITransparencyValue
    {
        get => _config.AlertUITransparency;
        set
        {
            _config.AlertUITransparency = value;
            _alertView?.Alpha = 1 - value;
        }
    }

    [UIComponent("alertUIAllowDraggingToggle")]
    private readonly ToggleSetting? _alertUIAllowDraggingToggle = null;

    private bool _alertUIAllowDragging = true;

    private bool AlertUIAllowDraggingValue
    {
        get => _alertUIAllowDragging;
        set
        {
            _alertUIAllowDragging = value;
            _floatingScreen?.ShowHandle = value;
        }
    }

    private List<string> AlertUIPlacementOptions => _alertUIPlacementMap.Values.ToList();

    [UIComponent("alertUICustomPlacementPositionXSlider")]
    private readonly SliderSetting? _alertUICustomPlacementPositionXSlider = null;

    private float AlertUICustomPlacementPositionXValue
    {
        get => _config.CustomAlertUITransform.Position.x;
        set
        {
            _config.CustomAlertUITransform.Position =
                new Vector3(
                    value,
                    _config.CustomAlertUITransform.Position.y,
                    _config.CustomAlertUITransform.Position.z
                );
            UpdateAlertUITransform();
        }
    }

    [UIComponent("alertUICustomPlacementPositionYSlider")]
    private readonly SliderSetting? _alertUICustomPlacementPositionYSlider = null;

    private float AlertUICustomPlacementPositionYValue
    {
        get => _config.CustomAlertUITransform.Position.y;
        set
        {
            _config.CustomAlertUITransform.Position =
                new Vector3(
                    _config.CustomAlertUITransform.Position.x,
                    value,
                    _config.CustomAlertUITransform.Position.z
                );
            UpdateAlertUITransform();
        }
    }

    [UIComponent("alertUICustomPlacementPositionZSlider")]
    private readonly SliderSetting? _alertUICustomPlacementPositionZSlider = null;

    private float AlertUICustomPlacementPositionZValue
    {
        get => _config.CustomAlertUITransform.Position.z;
        set
        {
            _config.CustomAlertUITransform.Position =
                new Vector3(
                    _config.CustomAlertUITransform.Position.x,
                    _config.CustomAlertUITransform.Position.y,
                    value
                );
            UpdateAlertUITransform();
        }
    }

    [UIComponent("alertUICustomPlacementRotationXSlider")]
    private readonly SliderSetting? _alertUICustomPlacementRotationXSlider = null;

    private float AlertUICustomPlacementRotationXValue
    {
        get => _config.CustomAlertUITransform.Rotation.x;
        set
        {
            _config.CustomAlertUITransform.Rotation =
                new Vector3(
                    value,
                    _config.CustomAlertUITransform.Rotation.y,
                    _config.CustomAlertUITransform.Rotation.z
                );
            UpdateAlertUITransform();
        }
    }

    [UIComponent("alertUICustomPlacementRotationYSlider")]
    private readonly SliderSetting? _alertUICustomPlacementRotationYSlider = null;

    private float AlertUICustomPlacementRotationYValue
    {
        get => _config.CustomAlertUITransform.Rotation.y;
        set
        {
            _config.CustomAlertUITransform.Rotation =
                new Vector3(
                    _config.CustomAlertUITransform.Rotation.x,
                    value,
                    _config.CustomAlertUITransform.Rotation.z
                );
            UpdateAlertUITransform();
        }
    }

    [UIComponent("alertUICustomPlacementRotationZSlider")]
    private readonly SliderSetting? _alertUICustomPlacementRotationZSlider = null;

    private float AlertUICustomPlacementRotationZValue
    {
        get => _config.CustomAlertUITransform.Rotation.z;
        set
        {
            _config.CustomAlertUITransform.Rotation =
                new Vector3(
                    _config.CustomAlertUITransform.Rotation.x,
                    _config.CustomAlertUITransform.Rotation.y,
                    value
                );
            UpdateAlertUITransform();
        }
    }

    [UIComponent("alertUICustomPlacementScaleSlider")]
    private readonly SliderSetting? _alertUICustomPlacementScaleSlider = null;

    private float AlertUICustomPlacementScaleValue
    {
        get => _config.CustomAlertUITransform.Scale;
        set
        {
            _config.CustomAlertUITransform.Scale = value;
            UpdateAlertUITransform();
        }
    }

    private void CreateAlertUI()
    {
        DestroyAlertUI();
        _alertView = BeatSaberUI.CreateViewController<AlertView>();
        _floatingScreen = FloatingScreen.CreateFloatingScreen(AlertUIConstants.FloatingScreenSize, true,
            new Vector3(0f, 0f, 0f), Quaternion.identity);
        _floatingScreen.HandleSide = FloatingScreen.Side.Full;
        _floatingScreen.HandleGrabbed += OnAlertUIGrabbed;
        _floatingScreen.HandleReleased += OnAlertUIReleased;
        _floatingScreen.SetRootViewController(_alertView, AnimationType.None);
        _alertView.SetCountdownDisplayType(_config.AlertUICountdownDisplayType);
        _alertView.Alpha = 1 - _config.AlertUITransparency;
        UpdateAlertUITransform();
        UpdateAlertUICountdownType();
    }

    private void DestroyAlertUI()
    {
        if (_countdownCoroutine != null) StopCoroutine(_countdownCoroutine);
        if (_floatingScreen != null)
        {
            _floatingScreen.SetRootViewController(null, AnimationType.None);
            _floatingScreen.HandleGrabbed -= OnAlertUIGrabbed;
            _floatingScreen.HandleReleased -= OnAlertUIReleased;
            DestroyImmediate(_floatingScreen);
            _floatingScreen = null;
        }

        if (_alertView != null)
        {
            DestroyImmediate(_alertView);
            _alertView = null;
        }
    }

    private void UpdateAlertUICountdownType()
    {
        _alertView?.SetCountdownDisplayType(_config.AlertUICountdownDisplayType);
        if (_countdownCoroutine != null) StopCoroutine(_countdownCoroutine);
        _countdownCoroutine = StartCoroutine(LoopCountdown());
    }

    private IEnumerator LoopCountdown()
    {
        float timeLeft = 2f;
        while (true)
        {
            if (timeLeft == 0f) timeLeft = 2f;

            timeLeft = Math.Max(0f, timeLeft - 0.016f);

            _alertView?.UpdateProgress(timeLeft, 1 - (timeLeft / 2f));
            _alertView?.BackgroundAlpha = (float)((Math.Sin(Time.time * 3) + 1) * .5);

            yield return new WaitForSecondsRealtime(0.016f);
        }
    }

    private void UpdateAlertUITransform()
    {
        if (_floatingScreen == null) return;
        AlertUITransform uiTransform = _config.AlertUIPlacement switch
        {
            AlertUIPlacement.Floor => AlertUIConstants.FloorTransform,
            AlertUIPlacement.Above => AlertUIConstants.AboveTransform,
            AlertUIPlacement.Custom => _config.CustomAlertUITransform,
            _ => AlertUIConstants.FloorTransform
        };
        _floatingScreen.transform.SetPositionAndRotation(uiTransform.Position, Quaternion.Euler(uiTransform.Rotation));
        _floatingScreen.transform.localScale = AlertUIConstants.FloatingScreenScale * uiTransform.Scale;
    }

    private void OnAlertUIGrabbed(object sender, FloatingScreenHandleEventArgs e)
    {
        _isAlertUIGrabbed = true;
        UpdateUITabUI();
    }

    private void OnAlertUIReleased(object sender, FloatingScreenHandleEventArgs e)
    {
        Vector3 position = e.Position;
        Vector3 rotation = e.Rotation.eulerAngles;
        _config.AlertUIPlacement = AlertUIPlacement.Custom;
        _config.CustomAlertUITransform.Position = position;
        _config.CustomAlertUITransform.Rotation = new Vector3(
            Mathf.DeltaAngle(0f, rotation.x),
            Mathf.DeltaAngle(0f, rotation.y),
            Mathf.DeltaAngle(0f, rotation.z)
        );
        _config.CustomAlertUITransform.Scale =
            _floatingScreen!.transform.localScale.x / AlertUIConstants.FloatingScreenScale.x;
        _isAlertUIGrabbed = false;
        UpdateUITabUI();
    }

    private void UpdateUITabUI()
    {
        _alertUICountdownDisplayTypeListSetting?.Interactable = EnableValue && !_isAlertUIGrabbed;
        _alertUIPlacementDropdown?.Interactable = EnableValue && !_isAlertUIGrabbed;
        _alertUIPlacementDropdown?.Value = AlertUIPlacementValue;
        _alertUITransparencySlider?.Interactable = EnableValue && !_isAlertUIGrabbed;
        _alertUIAllowDraggingToggle?.Interactable = EnableValue && !_isAlertUIGrabbed;
        bool interactive = EnableValue && _config.AlertUIPlacement == AlertUIPlacement.Custom && !_isAlertUIGrabbed;
        _alertUICustomPlacementPositionXSlider?.Interactable = interactive;
        _alertUICustomPlacementPositionXSlider?.Value = AlertUICustomPlacementPositionXValue;
        _alertUICustomPlacementPositionYSlider?.Interactable = interactive;
        _alertUICustomPlacementPositionYSlider?.Value = AlertUICustomPlacementPositionYValue;
        _alertUICustomPlacementPositionZSlider?.Interactable = interactive;
        _alertUICustomPlacementPositionZSlider?.Value = AlertUICustomPlacementPositionZValue;
        _alertUICustomPlacementRotationXSlider?.Interactable = interactive;
        _alertUICustomPlacementRotationXSlider?.Value = AlertUICustomPlacementRotationXValue;
        _alertUICustomPlacementRotationYSlider?.Interactable = interactive;
        _alertUICustomPlacementRotationYSlider?.Value = AlertUICustomPlacementRotationYValue;
        _alertUICustomPlacementRotationZSlider?.Interactable = interactive;
        _alertUICustomPlacementRotationZSlider?.Value = AlertUICustomPlacementRotationZValue;
        _alertUICustomPlacementScaleSlider?.Interactable = interactive;
        _alertUICustomPlacementScaleSlider?.Value = AlertUICustomPlacementScaleValue;
    }

    #endregion

    #region About

    private string VersionText => $"v{Plugin.Version.ToString()}";

    [UIAction("OpenGithubPage")]
    private void OpenGithubPage()
    {
        Application.OpenURL(Plugin.SourceURL);
    }

    #endregion

    [UIAction("FormatDuration")]
    private string FormatDuration(float value)
    {
        return Formatter.FormatFloatSecond(value);
    }

    [UIAction("FormatBeats")]
    private string FormatBeats(float value)
    {
        return Formatter.FormatBeats(value);
    }

    private void UpdateAllUI()
    {
        UpdateGeneralTabUI();
        UpdateAudioTabUI();
        UpdateUITabUI();
    }

    public void OnExit()
    {
        Destroy(_previewAudioSource);
        _previewAudioSource = null;
        DestroyAlertUI();
        _config.Changed();
    }

    public void OnEnter()
    {
        if (_isInUITab) CreateAlertUI();
    }
}