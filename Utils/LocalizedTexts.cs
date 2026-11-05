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

    public static class CrouchWallGroupingThresholdOption
    {
        public static string OneQuarter =>
            Get("CROUCH_ALERT_CROUCH_WALL_GROUPING_THRESHOLD_OPTION_1_4_OF_A_BEAT");

        public static string OneHalf =>
            Get("CROUCH_ALERT_CROUCH_WALL_GROUPING_THRESHOLD_OPTION_1_2_OF_A_BEAT");

        public static string One =>
            Get("CROUCH_ALERT_CROUCH_WALL_GROUPING_THRESHOLD_OPTION_1_BEAT");

        public static string N(float n) =>
            string.Format(Get("CROUCH_ALERT_CROUCH_WALL_GROUPING_THRESHOLD_OPTION_N_BEATS"), n);
    }

    public static class AlertUI
    {
        public static class Placement
        {
            public static string Floor =>
                Get("CROUCH_ALERT_ALERT_UI_PLACEMENT_OPTION_FLOOR");

            public static string Above =>
                Get("CROUCH_ALERT_ALERT_UI_PLACEMENT_OPTION_ABOVE");

            public static string Custom =>
                Get("CROUCH_ALERT_ALERT_UI_PLACEMENT_OPTION_CUSTOM");
        }

        public static class CountdownDisplayType
        {
            public static string ProgressBar =>
                Get("CROUCH_ALERT_ALERT_UI_COUNTDOWN_DISPLAY_TYPE_OPTION_PROGRESS_BAR");

            public static string Timer =>
                Get("CROUCH_ALERT_ALERT_UI_COUNTDOWN_DISPLAY_TYPE_OPTION_TIMER");
        }
    }

    public static string AudioClipNone => Get("CROUCH_ALERT_AUDIO_CLIP_NONE");

    public static string Get(string key) => BGLib.Polyglot.Localization.Get(key);
}