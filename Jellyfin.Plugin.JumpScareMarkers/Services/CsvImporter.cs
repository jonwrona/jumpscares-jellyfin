using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.JumpScareMarkers.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JumpScareMarkers.Services;

/// <summary>
/// Service for importing jump scare data from CSV files.
/// </summary>
public class CsvImporter
{
    private readonly ItemMatchingService _itemMatchingService;
    private readonly ILogger<CsvImporter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvImporter"/> class.
    /// </summary>
    /// <param name="itemMatchingService">The item matching service.</param>
    /// <param name="logger">The logger.</param>
    public CsvImporter(ItemMatchingService itemMatchingService, ILogger<CsvImporter> logger)
    {
        _itemMatchingService = itemMatchingService;
        _logger = logger;
    }

    /// <summary>
    /// Imports jump scare data from a CSV string.
    /// </summary>
    /// <param name="csvContent">The CSV content.</param>
    /// <returns>A list of imported JumpScareData objects.</returns>
    public List<JumpScareData> ImportFromCsv(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            throw new ArgumentException("CSV content is empty", nameof(csvContent));
        }

        var jumpScares = new List<JumpScareData>();
        var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            throw new ArgumentException("CSV must contain at least a header row and one data row");
        }

        // Expected header: ItemName,IMDb,TMDb,Timestamp,Intensity,Description,Type
        var header = lines[0].Split(',');
        _logger.LogInformation("CSV header: {Header}", string.Join(", ", header));

        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                var jumpScare = ParseCsvLine(lines[i]);
                if (jumpScare != null)
                {
                    jumpScares.Add(jumpScare);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse CSV line {LineNumber}: {Line}", i + 1, lines[i]);
            }
        }

        _logger.LogInformation("Successfully imported {Count} jump scares from CSV", jumpScares.Count);
        return jumpScares;
    }

    /// <summary>
    /// Parses a single CSV line into a JumpScareData object.
    /// </summary>
    /// <param name="line">The CSV line.</param>
    /// <returns>A JumpScareData object, or null if parsing failed.</returns>
    private JumpScareData? ParseCsvLine(string line)
    {
        // Simple CSV parsing (doesn't handle quoted commas, but our data doesn't have them)
        var fields = line.Split(',');

        if (fields.Length < 7)
        {
            _logger.LogWarning("CSV line has insufficient fields: {Line}", line);
            return null;
        }

        var itemName = fields[0].Trim();
        var imdbId = fields[1].Trim();
        var tmdbId = fields[2].Trim();
        var timestamp = fields[3].Trim();
        var intensityStr = fields[4].Trim();
        var description = fields[5].Trim();
        var typeStr = fields[6].Trim();

        // Match item to Jellyfin library
        Guid? itemId = null;

        // Try external IDs first (more reliable)
        if (!string.IsNullOrWhiteSpace(imdbId) || !string.IsNullOrWhiteSpace(tmdbId))
        {
            itemId = _itemMatchingService.FindItemByExternalId(imdbId, tmdbId);
        }

        // Fall back to name matching
        if (itemId == null && !string.IsNullOrWhiteSpace(itemName))
        {
            itemId = _itemMatchingService.FindItemByName(itemName);
        }

        if (itemId == null)
        {
            _logger.LogWarning("Could not match item '{ItemName}' (IMDb: {ImdbId}, TMDb: {TmdbId}) to library", itemName, imdbId, tmdbId);
            return null;
        }

        // Parse timestamp (HH:MM:SS or MM:SS)
        var timestampTicks = ParseTimestamp(timestamp);
        if (timestampTicks == null)
        {
            _logger.LogWarning("Invalid timestamp format: {Timestamp}", timestamp);
            return null;
        }

        // Parse intensity
        if (!Enum.TryParse<JumpScareIntensity>(intensityStr, true, out var intensity))
        {
            _logger.LogWarning("Invalid intensity value: {Intensity}, defaulting to Minor", intensityStr);
            intensity = JumpScareIntensity.Minor;
        }

        // Parse type
        if (!Enum.TryParse<JumpScareType>(typeStr, true, out var type))
        {
            _logger.LogWarning("Invalid type value: {Type}, defaulting to Other", typeStr);
            type = JumpScareType.Other;
        }

        return new JumpScareData
        {
            Id = Guid.NewGuid(),
            ItemId = itemId.Value,
            ItemName = itemName,
            TimestampTicks = timestampTicks.Value,
            Description = description,
            Intensity = intensity,
            Type = type,
            Source = "csv_import",
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Parses a timestamp string (HH:MM:SS or MM:SS) into ticks.
    /// </summary>
    /// <param name="timestamp">The timestamp string.</param>
    /// <returns>The timestamp in ticks, or null if parsing failed.</returns>
    private long? ParseTimestamp(string timestamp)
    {
        try
        {
            // Try parsing as HH:MM:SS first
            if (TimeSpan.TryParseExact(timestamp, @"h\:mm\:ss", CultureInfo.InvariantCulture, out var timeSpan))
            {
                return timeSpan.Ticks;
            }

            // Try parsing as MM:SS
            if (TimeSpan.TryParseExact(timestamp, @"mm\:ss", CultureInfo.InvariantCulture, out timeSpan))
            {
                return timeSpan.Ticks;
            }

            // Try generic TimeSpan.Parse as last resort
            if (TimeSpan.TryParse(timestamp, out timeSpan))
            {
                return timeSpan.Ticks;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse timestamp: {Timestamp}", timestamp);
            return null;
        }
    }
}
