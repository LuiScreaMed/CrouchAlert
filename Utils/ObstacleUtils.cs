using System;
using System.Collections.Generic;
using CrouchAlert.Data;

namespace CrouchAlert.Utils;

internal static class ObstacleUtils
{
    public static bool AreSpawnLinesOverlappingOrNextTo(ObstacleData obstacle1, ObstacleData obstacle2)
    {
        bool adjacent =
            obstacle1.lineIndex == obstacle2.lineIndex + obstacle2.width ||
            obstacle2.lineIndex == obstacle1.lineIndex + obstacle1.width;

        return adjacent ||
               (obstacle1.lineIndex < obstacle2.lineIndex + obstacle2.width &&
                obstacle1.lineIndex + obstacle1.width > obstacle2.lineIndex &&
                obstacle1.lineIndex != obstacle2.lineIndex &&
                obstacle1.lineIndex + obstacle1.width != obstacle2.lineIndex + obstacle2.width);
    }

    public static bool AreSpawnLinesIdentical(ObstacleData obstacle1, ObstacleData obstacle2)
    {
        return obstacle1.lineIndex == obstacle2.lineIndex && obstacle1.width == obstacle2.width;
    }

    /// <summary>
    /// Trim the obstacle to 4 lines, if the obstacle is outside the 4 lines, return null
    /// </summary>
    /// <param name="obstacle"></param>
    /// <returns></returns>
    public static ObstacleData? Trim(ObstacleData obstacle)
    {
        int startIndex = Math.Max(0, obstacle.lineIndex);
        int endIndex = Math.Min(obstacle.lineIndex + obstacle.width, 4);

        if (startIndex >= endIndex)
            return null;

        if (obstacle.lineIndex != startIndex || obstacle.lineIndex + obstacle.width != endIndex)
            return new ObstacleData(
                obstacle.time,
                obstacle.beat,
                obstacle.endBeat,
                obstacle.rotation,
                startIndex,
                obstacle.lineLayer,
                obstacle.duration,
                endIndex - startIndex,
                obstacle.height
            );

        return obstacle;
    }

    public static List<ObstacleData> SplitObstacleByBeats(ObstacleData obstacle, float beats,
        BPMChangeItem[] bpmChanges)
    {
        if (bpmChanges == null)
            throw new ArgumentNullException(nameof(bpmChanges));
        if (bpmChanges.Length == 0)
            throw new ArgumentException("Value cannot be an empty collection.", nameof(bpmChanges));

        List<ObstacleData> obstacleDatas = new();

        int bpmChangeIndex = 0;
        float bpm = bpmChanges[bpmChangeIndex].BPM;

        foreach (BPMChangeItem item in bpmChanges)
        {
            if (obstacle.time < item.Time) break;
            if (bpmChangeIndex < bpmChanges.Length - 1)
            {
                bpmChangeIndex++;
                bpm = bpmChanges[bpmChangeIndex].BPM;
            }
        }

        float time = obstacle.time;
        float endTime = obstacle.duration + obstacle.time;
        float beatsDuration = BeatTimeUtils.BeatsToDuration(bpm, beats);
        while (time < endTime)
        {
            if (bpmChangeIndex < bpmChanges.Length - 1 && time + beatsDuration >= bpmChanges[bpmChangeIndex + 1].Time)
            {
                obstacleDatas.Add(
                    new ObstacleData(
                        time,
                        BeatTimeUtils.GetBeatsFromStart(time, bpmChanges),
                        BeatTimeUtils.GetBeatsFromStart(bpmChanges[bpmChangeIndex + 1].Time, bpmChanges),
                        obstacle.rotation,
                        obstacle.lineIndex,
                        obstacle.lineLayer,
                        bpmChanges[bpmChangeIndex + 1].Time - time,
                        obstacle.width,
                        obstacle.height
                    )
                );

                bpmChangeIndex++;
                bpm = bpmChanges[bpmChangeIndex].BPM;
                beatsDuration = BeatTimeUtils.BeatsToDuration(bpm, beats);
                time = bpmChanges[bpmChangeIndex + 1].Time;
                continue;
            }

            if (time + beatsDuration >= endTime)
            {
                obstacleDatas.Add(
                    new ObstacleData(
                        time,
                        BeatTimeUtils.GetBeatsFromStart(time, bpmChanges),
                        BeatTimeUtils.GetBeatsFromStart(endTime, bpmChanges),
                        obstacle.rotation,
                        obstacle.lineIndex,
                        obstacle.lineLayer,
                        endTime - time,
                        obstacle.width,
                        obstacle.height
                    )
                );
                break;
            }

            obstacleDatas.Add(
                new ObstacleData(
                    time,
                    BeatTimeUtils.GetBeatsFromStart(time, bpmChanges),
                    BeatTimeUtils.GetBeatsFromStart(time + beatsDuration, bpmChanges),
                    obstacle.rotation,
                    obstacle.lineIndex,
                    obstacle.lineLayer,
                    beatsDuration,
                    obstacle.width,
                    obstacle.height
                )
            );
            time += beatsDuration;
        }

        return obstacleDatas;
    }
}