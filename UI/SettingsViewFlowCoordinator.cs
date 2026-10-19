using System;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using CrouchAlert.Utils;
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
        _menuButton = new MenuButton(LocalizedTexts.Title,
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
            SetTitle(LocalizedTexts.Title);
            showBackButton = true;
            ProvideInitialViewControllers(_settingsView);
        }
    }
}