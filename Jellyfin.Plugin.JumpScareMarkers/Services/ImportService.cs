using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.JumpScareMarkers.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JumpScareMarkers.Services;

/// <summary>
/// Service for importing jump scare data.
/// </summary>
public class ImportService
{
    private readonly CsvImporter _csvImporter;
    private readonly ILogger<ImportService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportService"/> class.
    /// </summary>
    /// <param name="csvImporter">The CSV importer.</param>
    /// <param name="logger">The logger.</param>
    public ImportService(CsvImporter csvImporter, ILogger<ImportService> logger)
    {
        _csvImporter = csvImporter;
        _logger = logger;
    }

    /// <summary>
    /// Imports jump scares from CSV content and updates the plugin configuration.
    /// </summary>
    /// <param name="csvContent">The CSV content to import.</param>
    /// <returns>An import result with statistics.</returns>
    public ImportResult ImportFromCsv(string csvContent)
    {
        var result = new ImportResult();

        try
        {
            _logger.LogInformation("Starting CSV import");

            // Parse CSV
            var importedScares = _csvImporter.ImportFromCsv(csvContent);
            result.TotalRows = importedScares.Count;

            if (importedScares.Count == 0)
            {
                result.Message = "No valid jump scares found in CSV";
                _logger.LogWarning(result.Message);
                return result;
            }

            // Get plugin instance
            var plugin = Plugin.Instance;
            if (plugin == null)
            {
                result.Success = false;
                result.Message = "Plugin instance not available";
                _logger.LogError(result.Message);
                return result;
            }

            var config = plugin.Configuration;

            // Filter out duplicates (same ItemId and TimestampTicks)
            var existingKeys = config.JumpScares
                .Select(js => (js.ItemId, js.TimestampTicks))
                .ToHashSet();

            var newScares = new List<JumpScareData>();
            var skippedDuplicates = 0;

            foreach (var scare in importedScares)
            {
                var key = (scare.ItemId, scare.TimestampTicks);
                if (existingKeys.Contains(key))
                {
                    skippedDuplicates++;
                    _logger.LogDebug("Skipping duplicate: ItemId={ItemId}, Timestamp={Timestamp}",
                        scare.ItemId, TimeHelpers.TicksToSeconds(scare.TimestampTicks));
                }
                else
                {
                    newScares.Add(scare);
                    existingKeys.Add(key);
                }
            }

            // Add new scares to configuration
            config.JumpScares.AddRange(newScares);

            // Save configuration
            plugin.SaveConfiguration(config);

            result.Success = true;
            result.ImportedCount = newScares.Count;
            result.SkippedCount = skippedDuplicates;
            result.Message = $"Successfully imported {newScares.Count} jump scares, skipped {skippedDuplicates} duplicates";

            _logger.LogInformation(result.Message);
            _logger.LogInformation("Total jump scares in database: {Total}", config.JumpScares.Count);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Import failed: {ex.Message}";
            _logger.LogError(ex, "CSV import failed");
        }

        return result;
    }

    /// <summary>
    /// Clears all jump scare data from the configuration.
    /// </summary>
    /// <returns>True if successful.</returns>
    public bool ClearAll()
    {
        try
        {
            var plugin = Plugin.Instance;
            if (plugin == null)
            {
                _logger.LogError("Plugin instance not available");
                return false;
            }

            var config = plugin.Configuration;
            var count = config.JumpScares.Count;

            config.JumpScares.Clear();
            plugin.SaveConfiguration(config);

            _logger.LogInformation("Cleared {Count} jump scares from configuration", count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear jump scares");
            return false;
        }
    }

    /// <summary>
    /// Gets statistics about the current jump scare database.
    /// </summary>
    /// <returns>Statistics object.</returns>
    public ImportStatistics GetStatistics()
    {
        var plugin = Plugin.Instance;
        if (plugin == null)
        {
            return new ImportStatistics();
        }

        var config = plugin.Configuration;
        var scares = config.JumpScares;

        return new ImportStatistics
        {
            TotalScares = scares.Count,
            TotalItems = scares.Select(js => js.ItemId).Distinct().Count(),
            MajorScares = scares.Count(js => js.Intensity == JumpScareIntensity.Major),
            MinorScares = scares.Count(js => js.Intensity == JumpScareIntensity.Minor)
        };
    }
}

/// <summary>
/// Result of an import operation.
/// </summary>
public class ImportResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the import was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of rows in the import file.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Gets or sets the number of jump scares successfully imported.
    /// </summary>
    public int ImportedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of rows skipped (duplicates, errors, etc.).
    /// </summary>
    public int SkippedCount { get; set; }
}

/// <summary>
/// Statistics about the jump scare database.
/// </summary>
public class ImportStatistics
{
    /// <summary>
    /// Gets or sets the total number of jump scares.
    /// </summary>
    public int TotalScares { get; set; }

    /// <summary>
    /// Gets or sets the total number of media items with jump scares.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets or sets the number of major intensity scares.
    /// </summary>
    public int MajorScares { get; set; }

    /// <summary>
    /// Gets or sets the number of minor intensity scares.
    /// </summary>
    public int MinorScares { get; set; }
}
