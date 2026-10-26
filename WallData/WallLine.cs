using System;

namespace CrouchAlert.WallData;

internal struct WallLine
{
    private Wall? NonBlockWall { get; set; }
    private Wall? CrouchWall { get; set; }
    private Wall? BlockWall { get; set; }

    public void ClearExpired(float time)
    {
        if (NonBlockWall.HasValue && NonBlockWall.Value.EndTime < time) NonBlockWall = null;
        if (BlockWall.HasValue && BlockWall.Value.EndTime < time) BlockWall = null;
        if (CrouchWall.HasValue && CrouchWall.Value.EndTime < time) CrouchWall = null;
    }

    public WallType Type => (NonBlockWall.HasValue, CrouchWall.HasValue,
            BlockWall.HasValue) switch
        {
            (true, true, _) or (_, _, true) => WallType.Block,
            (_, true, _) => WallType.Crouch,
            _ => WallType.NonBlock
        };

    public Wall? this[WallType wallType]
    {
        get => wallType switch
        {
            WallType.NonBlock => NonBlockWall,
            WallType.Crouch => CrouchWall,
            WallType.Block => BlockWall,
            _ => throw new ArgumentOutOfRangeException(nameof(wallType), wallType, null)
        };
        set
        {
            switch (wallType)
            {
                case WallType.NonBlock: NonBlockWall = value; break;
                case WallType.Crouch: CrouchWall = value; break;
                case WallType.Block: BlockWall = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(wallType), wallType, null);
            }
        }
    }

    public void ApplyWall(WallType wallType, Wall wall)
    {
        if (this[wallType].HasValue && this[wallType]!.Value.EndTime > wall.EndTime) return;
        this[wallType] = wall;
    }
}