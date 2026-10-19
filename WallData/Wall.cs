namespace CrouchAlert.WallData;

internal readonly struct Wall
{
    public float EndTime { get; }

    public Wall(float endTime)
    {
        EndTime = endTime;
    }
}