namespace Jellyfin.Plugin.JumpScareMarkers.Models;

/// <summary>
/// Represents the type of jump scare.
/// </summary>
public enum JumpScareType
{
    /// <summary>
    /// Sudden visual scare.
    /// </summary>
    Visual,

    /// <summary>
    /// Loud noise or sound.
    /// </summary>
    Audio,

    /// <summary>
    /// Both visual and audio elements.
    /// </summary>
    Combined,

    /// <summary>
    /// Other type of scare.
    /// </summary>
    Other
}
