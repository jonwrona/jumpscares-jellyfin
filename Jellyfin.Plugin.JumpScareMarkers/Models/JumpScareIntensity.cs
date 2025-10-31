namespace Jellyfin.Plugin.JumpScareMarkers.Models;

/// <summary>
/// Represents the intensity level of a jump scare.
/// Maps to notscare.me's "Major" and "Minor" classification.
/// </summary>
public enum JumpScareIntensity
{
    /// <summary>
    /// Minor startle (maps to notscare.me "Minor").
    /// </summary>
    Minor,

    /// <summary>
    /// Major jump scare (maps to notscare.me "Major").
    /// </summary>
    Major
}
