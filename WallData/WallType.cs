namespace CrouchAlert.WallData;

internal enum WallType
{
    NonBlock,
    Crouch,
    Block
}

internal static class WallTypeUtils
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