using System.Collections.Generic;
using Jellyfin.Plugin.JumpScareMarkers.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.JumpScareMarkers.Configuration;

/// <summary>
/// Plugin configuration for Jump Scare Markers.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        // Set default values
        StartDeltaSeconds = -2;
        EndDeltaSeconds = 2;
        JumpScares = new List<JumpScareData>();
        NotScareApiUrl = "https://notscare.me/api";
        EnableNotScareSync = false;
    }

    /// <summary>
    /// Gets or sets the number of seconds before the timestamp to start the segment.
    /// Default is -2 seconds (segment starts 2 seconds before the scare).
    /// </summary>
    public int StartDeltaSeconds { get; set; }

    /// <summary>
    /// Gets or sets the number of seconds after the timestamp to end the segment.
    /// Default is 2 seconds (segment ends 2 seconds after the scare).
    /// </summary>
    public int EndDeltaSeconds { get; set; }

    /// <summary>
    /// Gets or sets the list of jump scare data entries.
    /// </summary>
#pragma warning disable CA2227, CA1002 // Configuration needs mutable collection for serialization
    public List<JumpScareData> JumpScares { get; set; }
#pragma warning restore CA2227, CA1002

    /// <summary>
    /// Gets or sets the notscare.me API URL.
    /// </summary>
    public string NotScareApiUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether automatic sync with notscare.me is enabled.
    /// </summary>
    public bool EnableNotScareSync { get; set; }
}
