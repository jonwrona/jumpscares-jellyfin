using System;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JumpScareMarkers.Services;

/// <summary>
/// Service for matching media item names to Jellyfin library items.
/// </summary>
public class ItemMatchingService
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<ItemMatchingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemMatchingService"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="logger">The logger.</param>
    public ItemMatchingService(ILibraryManager libraryManager, ILogger<ItemMatchingService> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to find a Jellyfin library item by name.
    /// </summary>
    /// <param name="itemName">The name of the item to find (e.g., "Weapons (2025)").</param>
    /// <returns>The matching item's GUID, or null if not found.</returns>
    public Guid? FindItemByName(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            _logger.LogWarning("Item name is null or empty");
            return null;
        }

        _logger.LogDebug("Searching for item with name: {ItemName}", itemName);

        // Get all video items from the library
        var query = new InternalItemsQuery
        {
            IncludeItemTypes = new[] { Jellyfin.Data.Enums.BaseItemKind.Movie, Jellyfin.Data.Enums.BaseItemKind.Episode },
            Recursive = true
        };

        var items = _libraryManager.GetItemList(query);

        // Try exact match first
        var exactMatch = items.FirstOrDefault(i =>
            string.Equals(i.Name, itemName, StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null)
        {
            _logger.LogInformation("Found exact match for '{ItemName}': {ItemId} - {MatchedName}",
                itemName, exactMatch.Id, exactMatch.Name);
            return exactMatch.Id;
        }

        // Try contains match
        var containsMatch = items.FirstOrDefault(i =>
            i.Name.Contains(itemName, StringComparison.OrdinalIgnoreCase));

        if (containsMatch != null)
        {
            _logger.LogInformation("Found partial match for '{ItemName}': {ItemId} - {MatchedName}",
                itemName, containsMatch.Id, containsMatch.Name);
            return containsMatch.Id;
        }

        // Try extracting year and title separately for better matching
        // Example: "Weapons (2025)" -> title: "Weapons", year: 2025
        var match = System.Text.RegularExpressions.Regex.Match(itemName, @"^(.+?)\s*\((\d{4})\)$");
        if (match.Success)
        {
            var title = match.Groups[1].Value.Trim();
            if (int.TryParse(match.Groups[2].Value, out var year))
            {
                _logger.LogDebug("Extracted title '{Title}' and year {Year}", title, year);

                var yearMatch = items.FirstOrDefault(i =>
                    string.Equals(i.Name, title, StringComparison.OrdinalIgnoreCase) &&
                    i.ProductionYear == year);

                if (yearMatch != null)
                {
                    _logger.LogInformation("Found match by title and year for '{ItemName}': {ItemId} - {MatchedName}",
                        itemName, yearMatch.Id, yearMatch.Name);
                    return yearMatch.Id;
                }

                // Try just title match if year doesn't match
                var titleMatch = items.FirstOrDefault(i =>
                    string.Equals(i.Name, title, StringComparison.OrdinalIgnoreCase));

                if (titleMatch != null)
                {
                    _logger.LogWarning("Found title match but year mismatch for '{ItemName}': {ItemId} - {MatchedName} (Year: {Year})",
                        itemName, titleMatch.Id, titleMatch.Name, titleMatch.ProductionYear);
                    return titleMatch.Id;
                }
            }
        }

        _logger.LogWarning("No match found for item '{ItemName}'", itemName);
        return null;
    }

    /// <summary>
    /// Attempts to find a Jellyfin library item by IMDb or TMDb ID.
    /// </summary>
    /// <param name="imdbId">The IMDb ID (e.g., "tt26581740").</param>
    /// <param name="tmdbId">The TMDb ID (e.g., "1078605").</param>
    /// <returns>The matching item's GUID, or null if not found.</returns>
    public Guid? FindItemByExternalId(string? imdbId, string? tmdbId)
    {
        if (string.IsNullOrWhiteSpace(imdbId) && string.IsNullOrWhiteSpace(tmdbId))
        {
            _logger.LogWarning("Both IMDb and TMDb IDs are null or empty");
            return null;
        }

        _logger.LogDebug("Searching for item with IMDb: {ImdbId}, TMDb: {TmdbId}", imdbId, tmdbId);

        var query = new InternalItemsQuery
        {
            IncludeItemTypes = new[] { Jellyfin.Data.Enums.BaseItemKind.Movie, Jellyfin.Data.Enums.BaseItemKind.Episode },
            Recursive = true
        };

        var items = _libraryManager.GetItemList(query);

        // Try IMDb ID first
        if (!string.IsNullOrWhiteSpace(imdbId))
        {
            var imdbMatch = items.FirstOrDefault(i =>
                i.GetProviderId(MediaBrowser.Model.Entities.MetadataProvider.Imdb) == imdbId);

            if (imdbMatch != null)
            {
                _logger.LogInformation("Found match by IMDb ID '{ImdbId}': {ItemId} - {ItemName}",
                    imdbId, imdbMatch.Id, imdbMatch.Name);
                return imdbMatch.Id;
            }
        }

        // Try TMDb ID
        if (!string.IsNullOrWhiteSpace(tmdbId))
        {
            var tmdbMatch = items.FirstOrDefault(i =>
                i.GetProviderId(MediaBrowser.Model.Entities.MetadataProvider.Tmdb) == tmdbId);

            if (tmdbMatch != null)
            {
                _logger.LogInformation("Found match by TMDb ID '{TmdbId}': {ItemId} - {ItemName}",
                    tmdbId, tmdbMatch.Id, tmdbMatch.Name);
                return tmdbMatch.Id;
            }
        }

        _logger.LogWarning("No match found for IMDb: {ImdbId}, TMDb: {TmdbId}", imdbId, tmdbId);
        return null;
    }
}
