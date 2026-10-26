using System;

namespace CrouchAlert.Data;

internal readonly struct CrouchItem : IEquatable<CrouchItem>
{
    public float AlertTime { get; }

    public float Duration { get; }

    public CrouchItem(float alertTime, float duration)
    {
        AlertTime = alertTime;
        Duration = duration;
    }

    public bool Equals(CrouchItem other)
    {
        return AlertTime.Equals(other.AlertTime) && Duration.Equals(other.Duration);
    }

    public override bool Equals(object? obj)
    {
        return obj is CrouchItem other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(AlertTime, Duration);
    }
}