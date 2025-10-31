using System;

namespace Jellyfin.Plugin.JumpScareMarkers.Models;

/// <summary>
/// Helper methods for time conversions between ticks and seconds.
/// </summary>
public static class TimeHelpers
{
    /// <summary>
    /// Number of ticks per second (10,000,000 ticks = 1 second in .NET).
    /// </summary>
    public const long TicksPerSecond = 10_000_000;

    /// <summary>
    /// Converts seconds to ticks.
    /// </summary>
    /// <param name="seconds">The time in seconds.</param>
    /// <returns>The time in ticks.</returns>
    public static long SecondsToTicks(double seconds)
    {
        return (long)(seconds * TicksPerSecond);
    }

    /// <summary>
    /// Converts ticks to seconds.
    /// </summary>
    /// <param name="ticks">The time in ticks.</param>
    /// <returns>The time in seconds.</returns>
    public static double TicksToSeconds(long ticks)
    {
        return ticks / (double)TicksPerSecond;
    }

    /// <summary>
    /// Converts ticks to TimeSpan.
    /// </summary>
    /// <param name="ticks">The time in ticks.</param>
    /// <returns>The TimeSpan representation.</returns>
    public static TimeSpan TicksToTimeSpan(long ticks)
    {
        return TimeSpan.FromTicks(ticks);
    }

    /// <summary>
    /// Converts a TimeSpan to ticks.
    /// </summary>
    /// <param name="timeSpan">The TimeSpan to convert.</param>
    /// <returns>The time in ticks.</returns>
    public static long TimeSpanToTicks(TimeSpan timeSpan)
    {
        return timeSpan.Ticks;
    }
}
