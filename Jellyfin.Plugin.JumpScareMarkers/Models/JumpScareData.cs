using System;

namespace Jellyfin.Plugin.JumpScareMarkers.Models;

/// <summary>
/// Represents jump scare data for a media item.
/// Aligned with notscare.me data structure.
/// </summary>
public class JumpScareData
{
    /// <summary>
    /// Gets or sets the unique identifier for this jump scare entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin item ID this jump scare belongs to.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the jump scare in ticks.
    /// Single point in time - segments are created using deltas.
    /// </summary>
    public long TimestampTicks { get; set; }

    /// <summary>
    /// Gets or sets the description of the jump scare.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the type of jump scare (Visual, Audio, Combined, Other).
    /// </summary>
    public JumpScareType? Type { get; set; }

    /// <summary>
    /// Gets or sets the intensity level (Minor or Major).
    /// Maps to notscare.me's classification.
    /// </summary>
    public JumpScareIntensity? Intensity { get; set; }

    /// <summary>
    /// Gets or sets the media item name for display purposes.
    /// </summary>
    public string? ItemName { get; set; }

    /// <summary>
    /// Gets or sets the source of this data (e.g., "notscare.me", "manual", "import").
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
