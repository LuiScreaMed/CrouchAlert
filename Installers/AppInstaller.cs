using Zenject;

namespace CrouchAlert.Installers;

public class AppInstaller : Installer
{
    private readonly PluginConfig _config;

    public AppInstaller(PluginConfig pluginConfig)
    {
        _config = pluginConfig;
    }

    public override void InstallBindings()
    {
        Container.BindInstance(_config).AsSingle();
        Container.BindInterfacesAndSelfTo<AudioClipLoader>().AsSingle().NonLazy();
    }
}