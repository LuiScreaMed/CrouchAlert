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
    /// Alert the first crouch only in every beat map
    /// </summary>
    public virtual bool AlertFirstOnly { get; set; } = false;

    /// <summary>
    /// Enable strict mode, when enabled, only consider crouches with full lines of walls
    /// </summary>
    public virtual bool StrictMode { get; set; } = false;

    /// <summary>
    /// When enabled, alert crouches before configured beats
    /// </summary>
    public virtual bool UseBeatsAsAlertDurationUnit { get; set; } = true;

    /// <summary>
    /// Alert crouches before duration(in beats)
    /// </summary>
    public virtual float AlertDurationInBeats { get; set; } = 3f;

    /// <summary>
    /// Alert crouches before duration(in seconds)
    /// </summary>
    public virtual float AlertDurationInSeconds { get; set; } = 1.5f;

    /// <summary>
    /// Crouch walls whose spawn time difference is less than this and whose spawn lines are adjacent or overlapping are grouped together
    /// </summary>
    public virtual float CrouchWallGroupingThreshold { get; set; } = .5f;

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

    public virtual void Changed()
    {
    }
}