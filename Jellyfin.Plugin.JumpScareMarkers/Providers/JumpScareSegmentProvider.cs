using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.JumpScareMarkers.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Model;
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
        var buildTime = GetBuildTimestamp();
        _logger.LogInformation("JumpScareSegmentProvider initialized - Build: {BuildTime}", buildTime);
    }

    /// <inheritdoc />
    public string Name => "Jump Scare Markers";

    /// <inheritdoc />
    public ValueTask<bool> Supports(BaseItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // Support all video items (Movies, Episodes, etc.)
        var isVideo = item is Video;
        _logger.LogDebug("Supports check for item {ItemId} ({ItemName}): {IsVideo}", item.Id, item.Name, isVideo);
        return ValueTask.FromResult(isVideo);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MediaSegmentDto>> GetMediaSegments(
        MediaSegmentGenerationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

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

        _logger.LogDebug("Found {Count} jump scares for item {ItemId}", jumpScares.Count, request.ItemId);

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

        _logger.LogDebug("Created {Count} segments for item {ItemId}", segments.Count, request.ItemId);

        // Log each segment for debugging
        foreach (var segment in segments)
        {
            _logger.LogDebug(
                "Segment details: Id={Id}, Type={Type}, Start={Start}ms, End={End}ms, ItemId={ItemId}",
                segment.Id,
                segment.Type,
                segment.StartTicks / (TimeHelpers.TicksPerSecond / 1000),
                segment.EndTicks / (TimeHelpers.TicksPerSecond / 1000),
                segment.ItemId);
        }

        _logger.LogDebug("Returning {Count} segments to MediaSegmentManager", segments.Count);
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
            Id = Guid.NewGuid(),
            ItemId = jumpScare.ItemId,
            Type = MediaSegmentType.Commercial,
            StartTicks = startTicks,
            EndTicks = endTicks
        };
    }

    /// <summary>
    /// Gets the build timestamp from the assembly file.
    /// </summary>
    /// <returns>The build timestamp in UTC.</returns>
    private static string GetBuildTimestamp()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileInfo = new FileInfo(assembly.Location);
            return fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";
        }
        catch
        {
            return "Unknown";
        }
    }
}
