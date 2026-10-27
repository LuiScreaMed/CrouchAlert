namespace CrouchAlert.Utils;

internal static class ObstacleUtils
{
    public static bool AreOverlappingOrNextTo(ObstacleData obstacle1, ObstacleData obstacle2)
    {
        return obstacle1.lineIndex <= obstacle2.lineIndex + obstacle2.width &&
               obstacle1.lineIndex + obstacle1.width >= obstacle2.lineIndex;
    }
}