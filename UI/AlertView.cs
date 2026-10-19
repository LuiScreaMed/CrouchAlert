using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;

namespace CrouchAlert.UI;

[HotReload(RelativePathToLayout = @"alert.bsml")]
[ViewDefinition("CrouchAlert.UI.alert.bsml")]
internal class AlertView : BSMLAutomaticViewController
{
    private string _countDownText = "";

    public string CountDownText
    {
        get => _countDownText;
        set
        {
            _countDownText = value;
            NotifyPropertyChanged();
        }
    }
}