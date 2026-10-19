using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace CrouchAlert;

public class PluginConfig
{
    /// <summary>
    /// Enable the mod
    /// </summary>
    public virtual bool Enable { get; set; } = true;
    
    /// <summary>
    /// Enable strict mode, when enabled, only consider crouches with full lines of walls
    /// </summary>
    public virtual bool StrictMode { get; set; } = false;
    
    /// <summary>
    /// Alert crouches before duration(in seconds)
    /// </summary>
    public virtual float AlertDuration { get; set; } = 1f;

    /// <summary>
    /// Extend the duration for walls(in seconds), to consider walls with very short durations as groups
    /// </summary>
    public virtual float ExtendWallsByDuration { get; set; } = .3f;
    
    /// <summary>
    /// Play a sound when alert
    /// </summary>
    public virtual bool EnableAlertSound { get; set; } = true;
    
    /// <summary>
    /// The audio clip to be played when alerting. Can be found/added in directory "UserData/CrouchAlert"
    /// </summary>
    public virtual string AlertSoundAudioClip { get; set; } = "default.wav";
    
    /// <summary>
    /// The volume of the alert sound
    /// </summary>
    public virtual float AlertSoundVolume { get; set; } = 0.5f;
    
    public virtual void Changed() { }
}