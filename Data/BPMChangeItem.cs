namespace CrouchAlert.Data;

internal readonly struct BPMChangeItem
{
    public float Time { get; }
    public float BPM { get; }

    public BPMChangeItem(float time, float bpm)
    {
        Time = time;
        BPM = bpm;
    }
}