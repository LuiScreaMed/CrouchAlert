namespace CrouchAlert.Utils;

internal static class LocalizedTexts
{
    public static string Title => Get("CROUCH_ALERT_TITLE");
    public static string AudioClipNone => Get("CROUCH_ALERT_AUDIO_CLIP_NONE");

    public static string Get(string key) => BGLib.Polyglot.Localization.Get(key);
}