using CrouchAlert.UI;
using Zenject;

namespace CrouchAlert.Installers;

internal class GameInstaller : Installer
{
    private readonly PluginConfig _config;

    public GameInstaller(PluginConfig config)
    {
        _config = config;
    }

    public override void InstallBindings()
    {
        if (_config.Enable)
        {
            Container.Bind<AlertView>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<CrouchAlertController>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
        }
    }
}