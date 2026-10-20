using System.Collections.Generic;
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

    private List<string> _audioClipOptions = new();
    private List<string> _loadedAudioClips = new();

    // [UIParams] private readonly BSMLParserParams _parserParams = null!;

    [UIComponent("remindSoundAudioClipDropdown")]
    private readonly DropDownListSetting? _audioClipDropdown = null;

    [UIComponent("remindSoundVolumeSlider")]
    private readonly SliderSetting? _volumeSlider = null;

    [UIComponent("remindSoundAudioClipNotFoundText")]
    private readonly TextMeshProUGUI? _audioNotFoundText = null;

    [Inject]
    public void Construct(PluginConfig config, AudioClipLoader audioClipLoader)
    {
        _config = config;
        _audioClipLoader = audioClipLoader;
        _audioClipOptions.Add(LocalizedTexts.AudioClipNone);
        // Init audio clip drop down
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

    private bool EnableValue
    {
        get => _config.Enable;
        set
        {
            _config.Enable = value;
            _config.Changed();
        }
    }

    private bool UseStrictModeValue
    {
        get => _config.StrictMode;
        set
        {
            _config.StrictMode = value;
            _config.Changed();
        }
    }

    private float RemindBeforeDurationValue
    {
        get => _config.AlertDuration;
        set
        {
            _config.AlertDuration = value;
            _config.Changed();
        }
    }

    private float ExtendWallsByDurationValue
    {
        get => _config.ExtendWallsByDuration;
        set
        {
            _config.ExtendWallsByDuration = value;
            _config.Changed();
        }
    }

    private bool EnableRemindSoundValue
    {
        get => _config.EnableAlertSound;
        set
        {
            _config.EnableAlertSound = value;
            _config.Changed();
            NotifyPropertyChanged();
            UpdateUI();
        }
    }

    private string RemindSoundAudioClipValue
    {
        get => _config.AlertSoundAudioClip == string.Empty ? LocalizedTexts.AudioClipNone : _config.AlertSoundAudioClip;
        set
        {
            if (RemindSoundAudioClipNotFound)
            {
                int removeIndex = _audioClipOptions.IndexOf(RemindSoundAudioClipValue);
                _audioClipOptions.RemoveAt(removeIndex);
            }

            _config.AlertSoundAudioClip = value == LocalizedTexts.AudioClipNone ? string.Empty : value;

            _config.Changed();
            NotifyPropertyChanged();
            UpdateUI();
        }
    }

    private List<string> RemindSoundAudioClipOptions => _audioClipOptions;

    private bool RemindSoundAudioClipNotFound => _config.AlertSoundAudioClip != string.Empty && !_loadedAudioClips.Contains(RemindSoundAudioClipValue);

    private float RemindSoundVolumeValue
    {
        get => _config.AlertSoundVolume;
        set
        {
            _config.AlertSoundVolume = value;
            _config.Changed();
        }
    }

    [UIAction("FormatDuration")]
    private string FormatDuration(float value)
    {
        return Formatter.FormatFloatSecond(value);
    }

    private void UpdateUI()
    {
        _audioNotFoundText?.rectTransform.gameObject.SetActive(EnableRemindSoundValue && RemindSoundAudioClipNotFound);
        _audioClipDropdown?.UpdateChoices();
        _audioClipDropdown?.transform.parent.gameObject.SetActive(EnableRemindSoundValue);
        _volumeSlider?.gameObject.SetActive(EnableRemindSoundValue);
    }
}