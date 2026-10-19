using System;

namespace CrouchAlert.WallData;

internal struct Walls
{
    private WallLine Line0 { get; set; }
    private WallLine Line1 { get; set; }
    private WallLine Line2 { get; set; }
    private WallLine Line3 { get; set; }

    public Walls(WallLine line0, WallLine line1, WallLine line2, WallLine line3)
    {
        Line0 = line0;
        Line1 = line1;
        Line2 = line2;
        Line3 = line3;
    }

    public WallLine this[int index]
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

    public void ApplyNewObstacle(ObstacleData obstacle, float addDuration = 0.3f)
    {
        int startLine = Math.Max(0, obstacle.lineIndex);
        int endLine = Math.Min(obstacle.lineIndex + obstacle.width - 1, 3);
        if (startLine > endLine) return;

        float endTime = obstacle.time + obstacle.duration + addDuration;

        WallType wallType = WallTypeUtils.GetWallType(obstacle);

        for (int i = 0; i <= 3; i++)
        {
            WallLine line = this[i];
            line.ClearExpired(obstacle.time);
            if (i >= startLine && i <= endLine)
            {
                line.ApplyWall(wallType, new Wall(endTime));
            }

            this[i] = line;
        }
    }
}