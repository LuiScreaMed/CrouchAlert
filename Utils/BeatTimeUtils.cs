using System;
using CrouchAlert.Data;

namespace CrouchAlert.Utils;

internal static class BeatTimeUtils
{
    public static float BeatsToDuration(float bpm, float beats)
    {
        return 60.0f / bpm * beats;
    }

    public static float DurationToBeats(float bpm, float duration)
    {
        return duration / 60 * bpm;
    }

    public static float BeatsBetween(float startTime, float endTime, BPMChangeItem[] bpmChanges)
    {
        if (bpmChanges == null)
            throw new ArgumentNullException(nameof(bpmChanges));
        if (bpmChanges.Length == 0)
            throw new ArgumentException("Value cannot be an empty collection.", nameof(bpmChanges));

        int index = 0;
        float bpm = bpmChanges[index].BPM;
        float beats = 0f;

        foreach (BPMChangeItem item in bpmChanges)
        {
            if (startTime < item.Time) break;
            if (index < bpmChanges.Length - 1)
            {
                index++;
                bpm = bpmChanges[index].BPM;
            }
        }

        while (index < bpmChanges.Length - 1 && bpmChanges[index + 1].Time < endTime)
        {
            float duration = bpmChanges[index + 1].Time - startTime;
            startTime = bpmChanges[index + 1].Time;
            beats += DurationToBeats(bpm, duration);
            index++;
            bpm = bpmChanges[index].BPM;
        }

        beats += DurationToBeats(bpm, endTime - startTime);

        return beats;
    }

    public static float TimeBeforeBeats(float time, float beats, BPMChangeItem[] bpmChanges)
    {
        if (bpmChanges == null)
            throw new ArgumentNullException(nameof(bpmChanges));
        if (bpmChanges.Length == 0)
            throw new ArgumentException("Value cannot be an empty collection.", nameof(bpmChanges));

        int index = 0;
        float bpm = 0f;
        foreach (BPMChangeItem item in bpmChanges)
        {
            if (time < item.Time) break;
            index++;
            bpm = item.BPM;
        }

        float remainingBeats = beats;
        float duration = BeatsToDuration(bpm, remainingBeats);
        while (true)
        {
            if (index > 0 && (time - duration) < bpmChanges[index - 1].Time)
            {
                index--;
                float durationToPrevBPMChange = time - bpmChanges[index].Time;
                float durationInBeats = DurationToBeats(bpm, durationToPrevBPMChange);
                remainingBeats -= durationInBeats;
                time = bpmChanges[index].Time;
                bpm = bpmChanges[index].BPM;
                duration = BeatsToDuration(bpm, remainingBeats);
            }
            else
            {
                time -= duration;
                break;
            }
        }

        return time;
    }

    public static float GetBPM(float time, BPMChangeItem[] bpmChanges)
    {
        if (bpmChanges == null)
            throw new ArgumentNullException(nameof(bpmChanges));
        if (bpmChanges.Length == 0)
            throw new ArgumentException("Value cannot be an empty collection.", nameof(bpmChanges));

        float bpm = bpmChanges[0].BPM;
        for (int i = 0; i < bpmChanges.Length; i++)
        {
            if (bpmChanges[i].Time < time) continue;
            bpm = bpmChanges[i].BPM;
            break;
        }

        return bpm;
    }

    public static float GetBeatsFromStart(float time, BPMChangeItem[] bpmChanges)
    {
        if (bpmChanges == null)
            throw new ArgumentNullException(nameof(bpmChanges));
        if (bpmChanges.Length == 0)
            throw new ArgumentException("Value cannot be an empty collection.", nameof(bpmChanges));

        float beats = 0;

        for (int i = 0; i < bpmChanges.Length - 1; i++)
        {
            if (time > bpmChanges[i + 1].Time)
            {
                beats += DurationToBeats(bpmChanges[i].BPM, bpmChanges[i + 1].Time - bpmChanges[i].Time);
                continue;
            }

            beats += DurationToBeats(bpmChanges[i].BPM, time - bpmChanges[i].Time);
            break;
        }

        return beats;
    }
}