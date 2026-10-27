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

        int timeBPMIndex = 0;
        float bpm = 0f;
        foreach (BPMChangeItem item in bpmChanges)
        {
            if (time < item.Time) break;
            timeBPMIndex++;
            bpm = item.BPM;
        }

        float remainingBeats = beats;
        float duration = BeatsToDuration(bpm, remainingBeats);
        while (true)
        {
            if (timeBPMIndex > 0 && (time - duration) < bpmChanges[timeBPMIndex - 1].Time)
            {
                timeBPMIndex--;
                float durationToPrevBPMChange = time - bpmChanges[timeBPMIndex].Time;
                float durationInBeats = DurationToBeats(bpm, durationToPrevBPMChange);
                remainingBeats -= durationInBeats;
                time = bpmChanges[timeBPMIndex].Time;
                bpm = bpmChanges[timeBPMIndex].BPM;
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
}