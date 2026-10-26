using CrouchAlert.Data.Wall;

namespace CrouchAlert.Utils;

internal static class WallUtils
{
    public static WallType GetWallType(ObstacleData obstacle)
    {
        switch (obstacle.lineLayer)
        {
            case NoteLineLayer.Top:
                return WallType.Crouch;
            case NoteLineLayer.Upper when obstacle.height >= 2:
            case NoteLineLayer.Base when obstacle.height >= 3:
                return WallType.Block;
            default:
                return WallType.NonBlock;
        }
    }
}