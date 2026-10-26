using System;

namespace CrouchAlert.Data.Wall;

internal enum WallType
{
    NonBlock,
    Crouch,
    Block
}

internal struct WallTypes
{
    private WallType Line0 { get; set; }
    private WallType Line1 { get; set; }
    private WallType Line2 { get; set; }
    private WallType Line3 { get; set; }

    public WallTypes()
    {
        Line0 = WallType.NonBlock;
        Line1 = WallType.NonBlock;
        Line2 = WallType.NonBlock;
        Line3 = WallType.NonBlock;
    }

    public WallTypes(WallType line0, WallType line1, WallType line2, WallType line3)
    {
        Line0 = line0;
        Line1 = line1;
        Line2 = line2;
        Line3 = line3;
    }

    public WallType this[int index]
    {
        get => index switch
        {
            0 => Line0,
            1 => Line1,
            2 => Line2,
            3 => Line3,
            _ => throw new IndexOutOfRangeException()
        };
        set
        {
            switch (index)
            {
                case 0: Line0 = value; break;
                case 1: Line1 = value; break;
                case 2: Line2 = value; break;
                case 3: Line3 = value; break;
                default: throw new IndexOutOfRangeException();
            }
        }
    }

    public (WallType, WallType, WallType, WallType) ToTuple()
    {
        return (Line0, Line1, Line2, Line3);
    }
}