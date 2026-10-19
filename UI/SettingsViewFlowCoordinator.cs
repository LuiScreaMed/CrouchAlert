using System;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using HMUI;
using Zenject;

namespace CrouchAlert.UI;

internal class SettingsViewFlowCoordinator : FlowCoordinator, IDisposable
{
    private SettingsView _settingsView = null!;
    private MenuButton _menuButton = null!;

    [Inject]
    public void Construct(SettingsView settingsView)
    {
        _settingsView = settingsView;
        _menuButton = new MenuButton(BGLib.Polyglot.Localization.Get("CROUCH_ALERT_TITLE"),
            () => { BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(this); });
        MenuButtons.Instance.RegisterButton(_menuButton);
    }

    protected override void BackButtonWasPressed(ViewController topViewController)
    {
        BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this);
    }

    public void Dispose()
    {
        MenuButtons.Instance.UnregisterButton(_menuButton);
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        if (firstActivation)
        {
            SetTitle(BGLib.Polyglot.Localization.Get("CROUCH_ALERT_TITLE"));
            showBackButton = true;
            ProvideInitialViewControllers(_settingsView);
        }
    }
}