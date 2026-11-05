using System;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using CrouchAlert.Data.AlertUI;
using CrouchAlert.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace CrouchAlert.UI;

[HotReload(RelativePathToLayout = @"alert.bsml")]
[ViewDefinition("CrouchAlert.UI.alert.bsml")]
internal class AlertView : BSMLAutomaticViewController
{
    private AlertUICountdownDisplayType _countdownDisplayType = AlertUICountdownDisplayType.ProgressBar;

    private string _countDownText = "0.00s";

    private string CountDownText
    {
        get => _countDownText;
        set
        {
            _countDownText = value;
            NotifyPropertyChanged();
        }
    }

    private float _progressBarWidth = 0f;

    private float ProgressBarWidth
    {
        get => _progressBarWidth;
        set
        {
            _progressBarWidth = value;
            NotifyPropertyChanged();
        }
    }

    private CanvasGroup? _canvasGroup = null;

    [UIComponent("background")] private Backgroundable _background = null!;
    [UIComponent("progressBarLeft")] private Image _progressBarLeft = null!;
    [UIComponent("progressBarRight")] private Image _progressBarRight = null!;

    [UIAction("#post-parse")]
    private void PostParse()
    {
        Sprite sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f)
        );
        _progressBarLeft.sprite = sprite;
        _progressBarRight.sprite = sprite;
        _canvasGroup = _background.gameObject.AddComponent<CanvasGroup>();
    }

    private float _backgroundAlpha = 0f;

    public float BackgroundAlpha
    {
        get => _backgroundAlpha;
        set
        {
            _backgroundAlpha = value;
            NotifyPropertyChanged();
        }
    }

    private float _alpha = 1f;

    public float Alpha
    {
        get => _alpha;
        set
        {
            _alpha = value;
            _canvasGroup?.alpha = _alpha;
        }
    }

    private bool DisplayProgressBar => _countdownDisplayType == AlertUICountdownDisplayType.ProgressBar;

    private bool DisplayTimer => _countdownDisplayType == AlertUICountdownDisplayType.Timer;

    public void SetCountdownDisplayType(AlertUICountdownDisplayType countdownDisplayType)
    {
        _countdownDisplayType = countdownDisplayType;
        NotifyPropertyChanged(nameof(DisplayTimer));
        NotifyPropertyChanged(nameof(DisplayProgressBar));
    }

    public void UpdateProgress(float timeLeft, float progress)
    {
        CountDownText = Formatter.FormatFloatSecond(timeLeft);
        ProgressBarWidth = (float)(Math.Ceiling(AlertUIConstants.FloatingScreenSize.x * progress / 2));
    }
}