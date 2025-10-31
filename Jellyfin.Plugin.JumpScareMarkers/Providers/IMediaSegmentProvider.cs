using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.MediaSegments;

namespace Jellyfin.Plugin.JumpScareMarkers.Providers;

/// <summary>
/// Interface for media segment providers.
/// This interface matches Jellyfin's IMediaSegmentProvider from MediaBrowser.Controller.
/// </summary>
public interface IMediaSegmentProvider
{
    /// <summary>
    /// Gets the name of the provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Determines whether this provider supports the given item.
    /// </summary>
    /// <param name="item">The media item to check.</param>
    /// <returns>True if the provider supports this item, false otherwise.</returns>
    ValueTask<bool> Supports(BaseItem item);

    /// <summary>
    /// Gets the media segments for the specified item.
    /// </summary>
    /// <param name="request">The media segment generation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of media segments.</returns>
    Task<IReadOnlyList<MediaSegmentDto>> GetMediaSegments(
        MediaSegmentGenerationRequest request,
        CancellationToken cancellationToken);
}
