namespace CrouchAlert.Utils;

internal static class Formatter
{
    public static string FormatFloatSecond(float input) => $"{input.ToString("0.00")} s";

    public static string FormatBeats(float beats)
    {
        return beats == 1f ? LocalizedTexts.FormatBeat.One : LocalizedTexts.FormatBeat.N(beats);
    }
}