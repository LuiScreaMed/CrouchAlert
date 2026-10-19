namespace CrouchAlert.Utils;

internal static class Formatter
{
    public static string FormatFloatSecond(float input)
    {
        return $"{input.ToString("0.00")} s";
    }
}