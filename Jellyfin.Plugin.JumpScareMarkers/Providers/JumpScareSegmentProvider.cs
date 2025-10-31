using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JumpScareMarkers.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.MediaSegments;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JumpScareMarkers.Providers;

/// <summary>
/// Provides jump scare segments for media items.
/// </summary>
public class JumpScareSegmentProvider : IMediaSegmentProvider
{
    private readonly ILogger<JumpScareSegmentProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JumpScareSegmentProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public JumpScareSegmentProvider(ILogger<JumpScareSegmentProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Jump Scare Markers";

    /// <inheritdoc />
    public ValueTask<bool> Supports(BaseItem item)
    {
        // Support all video items (Movies, Episodes, etc.)
        return ValueTask.FromResult(item is Video);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MediaSegmentDto>> GetMediaSegments(
        MediaSegmentGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var plugin = Plugin.Instance;
        if (plugin == null)
        {
            _logger.LogWarning("Plugin instance is null, cannot retrieve jump scares");
            return Task.FromResult<IReadOnlyList<MediaSegmentDto>>(Array.Empty<MediaSegmentDto>());
        }

        var config = plugin.Configuration;
        if (config.JumpScares == null || config.JumpScares.Count == 0)
        {
            _logger.LogDebug("No jump scares configured");
            return Task.FromResult<IReadOnlyList<MediaSegmentDto>>(Array.Empty<MediaSegmentDto>());
        }

        // Find jump scares for this specific item
        var jumpScares = config.JumpScares
            .Where(js => js.ItemId == request.ItemId)
            .ToList();

        if (jumpScares.Count == 0)
        {
            _logger.LogDebug("No jump scares found for item {ItemId}", request.ItemId);
            return Task.FromResult<IReadOnlyList<MediaSegmentDto>>(Array.Empty<MediaSegmentDto>());
        }

        _logger.LogInformation("Found {Count} jump scares for item {ItemId}", jumpScares.Count, request.ItemId);

        // Convert jump scares to media segments
        var segments = new List<MediaSegmentDto>();
        foreach (var jumpScare in jumpScares)
        {
            try
            {
                var segment = CreateSegment(jumpScare, config);
                segments.Add(segment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create segment for jump scare {Id}", jumpScare.Id);
            }
        }

        _logger.LogInformation("Created {Count} segments for item {ItemId}", segments.Count, request.ItemId);
        return Task.FromResult<IReadOnlyList<MediaSegmentDto>>(segments);
    }

    /// <summary>
    /// Creates a MediaSegmentDto from a JumpScareData object.
    /// </summary>
    /// <param name="jumpScare">The jump scare data.</param>
    /// <param name="config">The plugin configuration for delta values.</param>
    /// <returns>A MediaSegmentDto representing the jump scare segment.</returns>
    private MediaSegmentDto CreateSegment(JumpScareData jumpScare, Configuration.PluginConfiguration config)
    {
        // Calculate start and end ticks using deltas
        var startTicks = jumpScare.TimestampTicks + (config.StartDeltaSeconds * TimeHelpers.TicksPerSecond);
        var endTicks = jumpScare.TimestampTicks + (config.EndDeltaSeconds * TimeHelpers.TicksPerSecond);

        // Ensure start is before end
        if (startTicks >= endTicks)
        {
            _logger.LogWarning(
                "Invalid segment boundaries for jump scare {Id}: start={Start}, end={End}. Using 1-second duration.",
                jumpScare.Id,
                startTicks,
                endTicks);

            startTicks = jumpScare.TimestampTicks;
            endTicks = jumpScare.TimestampTicks + TimeHelpers.TicksPerSecond;
        }

        // Ensure non-negative ticks
        if (startTicks < 0)
        {
            _logger.LogWarning("Start ticks < 0 for jump scare {Id}, clamping to 0", jumpScare.Id);
            startTicks = 0;
        }

        return new MediaSegmentDto
        {
            Id = jumpScare.Id,
            ItemId = jumpScare.ItemId,
            Type = MediaSegmentType.Unknown, // Use Unknown for custom segment types
            StartTicks = startTicks,
            EndTicks = endTicks
        };
    }
}
