using CrouchAlert.UI;
using Zenject;

namespace CrouchAlert.Installers;

internal class MenuInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.Bind<SettingsView>().FromNewComponentAsViewController().AsSingle();
        Container.BindInterfacesAndSelfTo<SettingsViewFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
    }
}