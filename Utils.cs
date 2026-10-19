namespace CrouchAlert;

internal static class Utils
{
    public static string FormatFloatSecond(float input)
    {
        return $"{input.ToString("0.00")} s";
    }
}