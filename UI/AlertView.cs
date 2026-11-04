using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;

namespace CrouchAlert.UI;

[HotReload(RelativePathToLayout = @"alert.bsml")]
[ViewDefinition("CrouchAlert.UI.alert.bsml")]
internal class AlertView : BSMLAutomaticViewController
{
    private string _countDownText = "0.00s";

    public string CountDownText
    {
        get => _countDownText;
        set
        {
            _countDownText = value;
            NotifyPropertyChanged();
        }
    }

    private bool _isInSettings = false;

    public void InSettings()
    {
        _isInSettings = true;
        NotifyPropertyChanged(nameof(BackgroundAlpha));
    }
    
    private float BackgroundAlpha => _isInSettings ? 1f : 0f;
}