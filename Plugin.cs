using CrouchAlert.Installers;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using IPA.Loader;
using SiraUtil.Zenject;
using IpaLogger = IPA.Logging.Logger;
using IpaConfig = IPA.Config.Config;

namespace CrouchAlert;

[Plugin(RuntimeOptions.DynamicInit), NoEnableDisable]
internal class Plugin
{
    internal static IpaLogger Log { get; private set; } = null!;

    private readonly Harmony _harmony = new("com.luiscreamed.crouch-alert");

    // Methods with [Init] are called when the plugin is first loaded by IPA.
    // All the parameters are provided by IPA and are optional.
    // The constructor is called before any method with [Init]. Only use [Init] with one constructor.
    [Init]
    public Plugin(IpaLogger ipaLogger, IpaConfig ipaConfig, Zenjector zenjector, PluginMetadata pluginMetadata)
    {
        Log = ipaLogger;
        zenjector.UseLogger(Log);

        // Creates an instance of PluginConfig used by IPA to load and store config values
        var pluginConfig = ipaConfig.Generated<PluginConfig>();

        zenjector.Install<AppInstaller>(Location.App, pluginConfig);
        zenjector.Install<GameInstaller>(Location.Player);
        zenjector.Install<MenuInstaller>(Location.Menu);
        
        _harmony.PatchAll();

        Log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");
    }
}