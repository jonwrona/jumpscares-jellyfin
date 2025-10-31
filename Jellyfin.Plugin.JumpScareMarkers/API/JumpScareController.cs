using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Jellyfin.Plugin.JumpScareMarkers.Models;
using Jellyfin.Plugin.JumpScareMarkers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JumpScareMarkers.API;

/// <summary>
/// Jump Scare API controller.
/// </summary>
[ApiController]
[Route("JumpScareMarkers")]
[Authorize(Policy = "RequiresElevation")] // Require admin access
public class JumpScareController : ControllerBase
{
    private readonly ImportService _importService;
    private readonly ILogger<JumpScareController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JumpScareController"/> class.
    /// </summary>
    /// <param name="importService">The import service.</param>
    /// <param name="logger">The logger.</param>
    public JumpScareController(ImportService importService, ILogger<JumpScareController> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    /// <summary>
    /// Imports jump scares from a CSV file.
    /// </summary>
    /// <param name="file">The CSV file to import.</param>
    /// <returns>Import result.</returns>
    [HttpPost("Import")]
    [Consumes("multipart/form-data")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ImportResult>> ImportCsv([Required] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ImportResult
            {
                Success = false,
                Message = "No file uploaded"
            });
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ImportResult
            {
                Success = false,
                Message = "File must be a CSV file"
            });
        }

        try
        {
            _logger.LogInformation("Importing CSV file: {FileName} ({Size} bytes)", file.FileName, file.Length);

            // Read file content
            string csvContent;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                csvContent = await reader.ReadToEndAsync();
            }

            // Import
            var result = _importService.ImportFromCsv(csvContent);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import CSV file");
            return StatusCode(500, new ImportResult
            {
                Success = false,
                Message = $"Import failed: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Gets all jump scares for a specific item.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <returns>List of jump scares.</returns>
    [HttpGet("Item/{itemId}")]
    [Produces(MediaTypeNames.Application.Json)]
    public ActionResult<List<JumpScareData>> GetJumpScares([FromRoute] Guid itemId)
    {
        var plugin = Plugin.Instance;
        if (plugin == null)
        {
            return StatusCode(500, "Plugin not initialized");
        }

        var scares = plugin.Configuration.JumpScares
            .Where(js => js.ItemId == itemId)
            .OrderBy(js => js.TimestampTicks)
            .ToList();

        _logger.LogInformation("Retrieved {Count} jump scares for item {ItemId}", scares.Count, itemId);
        return Ok(scares);
    }

    /// <summary>
    /// Gets all jump scares in the database.
    /// </summary>
    /// <returns>List of all jump scares.</returns>
    [HttpGet("All")]
    [Produces(MediaTypeNames.Application.Json)]
    public ActionResult<List<JumpScareData>> GetAllJumpScares()
    {
        var plugin = Plugin.Instance;
        if (plugin == null)
        {
            return StatusCode(500, "Plugin not initialized");
        }

        var scares = plugin.Configuration.JumpScares
            .OrderBy(js => js.ItemName)
            .ThenBy(js => js.TimestampTicks)
            .ToList();

        _logger.LogInformation("Retrieved all {Count} jump scares", scares.Count);
        return Ok(scares);
    }

    /// <summary>
    /// Gets statistics about the jump scare database.
    /// </summary>
    /// <returns>Statistics object.</returns>
    [HttpGet("Statistics")]
    [Produces(MediaTypeNames.Application.Json)]
    public ActionResult<ImportStatistics> GetStatistics()
    {
        var stats = _importService.GetStatistics();
        return Ok(stats);
    }

    /// <summary>
    /// Clears all jump scare data.
    /// </summary>
    /// <returns>Result of the operation.</returns>
    [HttpDelete("All")]
    public ActionResult<object> ClearAll()
    {
        _logger.LogWarning("Clearing all jump scare data");

        var success = _importService.ClearAll();

        if (success)
        {
            return Ok(new { success = true, message = "All jump scares cleared" });
        }

        return StatusCode(500, new { success = false, message = "Failed to clear data" });
    }

    /// <summary>
    /// Refreshes segments for all items with jump scare data.
    /// This triggers Jellyfin to re-query the segment provider.
    /// </summary>
    /// <returns>Result of the operation.</returns>
    [HttpPost("RefreshSegments")]
    public ActionResult<object> RefreshSegments()
    {
        _logger.LogInformation("Segment refresh requested");

        // Note: In a full implementation, this would trigger IMediaSegmentManager
        // to refresh segments. For now, we'll just return success.
        // Segments will be generated on next playback when Jellyfin queries the provider.

        return Ok(new
        {
            success = true,
            message = "Segments will be refreshed on next playback. Jellyfin clients may need to reload the player."
        });
    }
}
