namespace CrouchAlert.Utils;

internal static class LocalizedTexts
{
    public static string Title => Get("CROUCH_ALERT_TITLE");

    public static class AlertDurationUnit
    {
        public static string Beats => Get("CROUCH_ALERT_ALERT_DURATION_UNIT_OPTION_BEATS");
        public static string Seconds => Get("CROUCH_ALERT_ALERT_DURATION_UNIT_OPTION_SECONDS");
    }

    public static class FormatBeat
    {
        public static string One => Get("CROUCH_ALERT_FORMAT_BEATS_1_BEAT");
        public static string N(float n) => string.Format(Get("CROUCH_ALERT_FORMAT_BEATS_N_BEATS"), n);
    }

    public static class MinimumWallDurationInBeatsOption
    {
        public static string OneQuarter =>
            Get("CROUCH_ALERT_MINIMUM_WALL_DURATION_IN_BEATS_OPTION_1_4_OF_A_BEAT");

        public static string OneHalf =>
            Get("CROUCH_ALERT_MINIMUM_WALL_DURATION_IN_BEATS_OPTION_1_2_OF_A_BEAT");

        public static string One =>
            Get("CROUCH_ALERT_MINIMUM_WALL_DURATION_IN_BEATS_OPTION_1_BEAT");

        public static string N(float n) =>
            string.Format(Get("CROUCH_ALERT_MINIMUM_WALL_DURATION_IN_BEATS_OPTION_N_BEATS"), n);
    }

    public static string AudioClipNone => Get("CROUCH_ALERT_AUDIO_CLIP_NONE");

    public static string Get(string key) => BGLib.Polyglot.Localization.Get(key);
}