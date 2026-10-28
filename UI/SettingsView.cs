using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using CrouchAlert.Utils;
using TMPro;
using Zenject;

namespace CrouchAlert.UI;

[HotReload(RelativePathToLayout = @"settings.bsml")]
[ViewDefinition("CrouchAlert.UI.settings.bsml")]
internal class SettingsView : BSMLAutomaticViewController
{
    private PluginConfig _config = null!;
    private AudioClipLoader _audioClipLoader = null!;

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
        UpdateUI();
    }

    #region Control: Enable Toggle

    private bool EnableValue
    {
        get => _config.Enable;
        set
        {
            _config.Enable = value;
            _config.Changed();
            UpdateUI();
        }
    }

    #endregion

    #region Control: Alert First Only Toggle

    [UIComponent("alertFirstOnlyToggle")] private readonly ToggleSetting? _alertFirstOnlyToggle = null;

    private bool AlertFirstOnlyValue
    {
        get => _config.AlertFirstOnly;
        set
        {
            _config.AlertFirstOnly = value;
            _config.Changed();
        }
    }

    #endregion

    #region Control: Strict Mode Toggle

    [UIComponent("strictModeToggle")] private readonly ToggleSetting? _strictModeToggle = null;

    private bool StrictModeValue
    {
        get => _config.StrictMode;
        set
        {
            _config.StrictMode = value;
            _config.Changed();
        }
    }

    #endregion

    #region Control: Alert Duration Unit Dropdown

    [UIComponent("alertDurationUnitDropdown")]
    private readonly DropDownListSetting? _alertDurationUnitDropdown = null;

    private List<string> AlertDurationUnitOptions => _alertDurationUnitOptions;

    private string AlertDurationUnitValue
    {
        get => AlertDurationUnitOptions[Convert.ToInt16(_config.UseBeatsAsAlertDurationUnit)];
        set
        {
            _config.UseBeatsAsAlertDurationUnit = Convert.ToBoolean(AlertDurationUnitOptions.IndexOf(value));
            _config.Changed();
            UpdateUI();
        }
    }

    #endregion

    #region Control: Alert Duration In Beats Slider

    [UIComponent("alertDurationInBeatsSlider")]
    private readonly SliderSetting? _alertDurationInBeatsSlider = null;

    private float AlertDurationInBeatsValue
    {
        get => _config.AlertDurationInBeats;
        set
        {
            _config.AlertDurationInBeats = value;
            _config.Changed();
        }
    }

    #endregion

    #region Control: Alert Duration In Seconds Slider

    [UIComponent("alertDurationInSecondsSlider")]
    private readonly SliderSetting? _alertDurationInSecondsSlider = null;

    private float AlertDurationInSecondsValue
    {
        get => _config.AlertDurationInSeconds;
        set
        {
            _config.AlertDurationInSeconds = value;
            _config.Changed();
        }
    }

    #endregion

    #region Control: Crouch Wall Grouping Threshold Dropdown

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
            _config.Changed();
        }
    }

    #endregion

    #region Control: Enable Alert Sound Toggle

    [UIComponent("alertSoundToggle")] private readonly ToggleSetting? _alertSoundToggle = null;

    private bool EnableAlertSoundValue
    {
        get => _config.EnableAlertSound;
        set
        {
            _config.EnableAlertSound = value;
            _config.Changed();
            UpdateUI();
        }
    }

    #endregion

    #region Control: Audio Clip Dropdown

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

            _config.Changed();
            UpdateUI();
        }
    }

    #endregion

    #region Control: Audio Not Found Text

    [UIComponent("alertSoundAudioClipNotFoundText")]
    private readonly TextMeshProUGUI? _audioNotFoundText = null;

    private bool AlertSoundAudioClipNotFound => _config.AlertSoundAudioClip != string.Empty &&
                                                !_loadedAudioClips.Contains(AlertSoundAudioClipValue);

    #endregion

    #region Control: Audio Volume Slider

    [UIComponent("alertSoundVolumeSlider")]
    private readonly SliderSetting? _volumeSlider = null;

    private float AlertSoundVolumeValue
    {
        get => _config.AlertSoundVolume;
        set
        {
            _config.AlertSoundVolume = value;
            _config.Changed();
        }
    }

    #endregion

    private string VersionText => $"v{Plugin.Version.ToString()}";

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

    private void UpdateUI()
    {
        // Enable
        _alertFirstOnlyToggle?.Interactable = EnableValue;
        _strictModeToggle?.Interactable = EnableValue;
        _alertDurationUnitDropdown?.Interactable = EnableValue;
        _alertDurationInBeatsSlider?.Interactable = EnableValue;
        _alertDurationInSecondsSlider?.Interactable = EnableValue;
        _crouchWallGroupingThresholdDropdown?.Interactable = EnableValue;
        _alertSoundToggle?.Interactable = EnableValue;
        _audioClipDropdown?.Interactable = EnableValue;
        _volumeSlider?.Interactable = EnableValue;
        // Alert Duration
        _alertDurationInBeatsSlider?.gameObject.SetActive(_config.UseBeatsAsAlertDurationUnit);
        _alertDurationInSecondsSlider?.gameObject.SetActive(!_config.UseBeatsAsAlertDurationUnit);
        _crouchWallGroupingThresholdDropdown?.UpdateChoices();
        // Audio
        _audioNotFoundText?.rectTransform.gameObject.SetActive(EnableAlertSoundValue && AlertSoundAudioClipNotFound);
        _audioClipDropdown?.UpdateChoices();
        _audioClipDropdown?.transform.parent.gameObject.SetActive(EnableAlertSoundValue);
        _volumeSlider?.gameObject.SetActive(EnableAlertSoundValue);
    }
}