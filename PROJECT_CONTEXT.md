# Jellyfin Jump Scare Marker Plugin - Project Context

## Project Overview

### Goal

Create a Jellyfin plugin that adds visual timeline markers to indicate when jump scares occur during horror movie/TV show playback, helping viewers who are sensitive to sudden scares.

### Core Functionality

- Display markers on the video timeline/progress bar showing jump scare locations
- Store timestamp data for jump scares with metadata (intensity, type, description)
- Provide administrative interface for managing jump scare data
- Support import/export of timestamp databases
- Enable hover tooltips with jump scare details
- Optional: Warning notifications before jump scares occur

---

## Technical Foundation

### Target Platform

- **Jellyfin Version:** 10.10+ (required for Media Segments API)
- **Plugin Framework:** .NET 8.0
- **Language:** C#
- **Architecture:** Server-side plugin with optional web UI components

### Key Jellyfin Systems

#### Media Segments API

Jellyfin 10.10+ includes a Media Segments system that allows plugins to store segment metadata (intros, outros, commercials, etc.) that clients automatically display on video timelines.

**Key Characteristics:**

- Segments have type information (Intro, Outro, Commercial, etc.)
- Clients automatically render segments without custom modifications
- Supports multiple segment providers working together
- Segments stored in Jellyfin's database with timestamps
- Each segment has: ItemId, Type, StartTicks, EndTicks

**Segment Display:**

- Rendered as visual markers on video timeline
- Position calculated dynamically based on video duration
- Styled with CSS classes for customization
- Support hover states and tooltips

#### Plugin Interfaces

Based on Jellyfin's plugin architecture, key interfaces we'll use:

```csharp
// Core plugin base
public class JumpScarePlugin : BasePlugin<PluginConfiguration>
{
    public override Guid Id => new("your-guid-here");
    public override string Name => "Jump Scare Markers";
}

// For custom pages in admin UI
public interface IHasWebPages
{
    IEnumerable<PluginPageInfo> GetPages();
}

// For scheduled tasks
public interface IScheduledTask
{
    string Name { get; }
    string Description { get; }
    string Category { get; }
    Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken);
    // Other methods
}
```

---

## Implementation Architecture

### Component Structure

```
Jellyfin.Plugin.JumpScareMarkers/
├── Configuration/
│   ├── PluginConfiguration.cs       # Plugin settings
│   ├── configPage.html              # Admin UI page
│   └── config.js                    # Admin UI logic
├── Models/
│   ├── JumpScareData.cs            # Data structure for jump scares
│   ├── JumpScareType.cs            # Enum for scare types
│   └── JumpScareIntensity.cs       # Enum for intensity levels
├── Providers/
│   ├── JumpScareSegmentProvider.cs # Implements segment provider
│   └── JumpScareManager.cs         # Business logic
├── ScheduledTasks/
│   └── ScanJumpScaresTask.cs       # Task to populate segments
├── API/
│   └── JumpScareController.cs      # REST endpoints (optional)
└── JumpScarePlugin.cs              # Main plugin class
```

### Data Model

```csharp
public class JumpScareData
{
    // Required fields
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }        // Jellyfin item ID
    public long TimestampTicks { get; set; } // Single timestamp point (converted to segment using deltas)

    // Optional metadata (aligned with notscare.me data structure)
    public string? Description { get; set; }
    public JumpScareType? Type { get; set; }
    public JumpScareIntensity? Intensity { get; set; }

    // Optional: Display and tracking
    public string? ItemName { get; set; }    // For display
    public string? Source { get; set; }      // "notscare.me", "manual", "import"
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Computed properties for segment creation
    public long StartTicks => TimestampTicks + (Configuration.StartDeltaSeconds * TimeHelpers.TicksPerSecond);
    public long EndTicks => TimestampTicks + (Configuration.EndDeltaSeconds * TimeHelpers.TicksPerSecond);
}

public enum JumpScareType
{
    Visual,      // Sudden visual scare
    Audio,       // Loud noise/sound
    Combined,    // Both visual and audio
    Other
}

public enum JumpScareIntensity
{
    Minor,       // Minor startle (maps to notscare.me "Minor")
    Major        // Major jump scare (maps to notscare.me "Major")
}

public class PluginConfiguration : BasePluginConfiguration
{
    // Global segment delta configuration
    public int StartDeltaSeconds { get; set; } = -2;  // 2 seconds before timestamp
    public int EndDeltaSeconds { get; set; } = 2;     // 2 seconds after timestamp

    // Jump scare data storage
    public List<JumpScareData> JumpScares { get; set; } = new();

    // notscare.me API configuration
    public string NotScareApiUrl { get; set; } = "https://notscare.me/api";
    public bool EnableNotScareSync { get; set; } = false;
}
```

### Media Segment Integration

The plugin will create media segments with a custom type. Based on Jellyfin's segment system:

```csharp
public class JumpScareSegmentProvider
{
    private readonly IMediaSegmentManager _segmentManager;
    private readonly ILogger<JumpScareSegmentProvider> _logger;

    public async Task CreateJumpScareSegments(Guid itemId, CancellationToken cancellationToken)
    {
        // Get jump scare data for this item
        var jumpScares = GetJumpScaresForItem(itemId);

        foreach (var scare in jumpScares)
        {
            var segment = new MediaSegmentDto
            {
                ItemId = itemId,
                Type = MediaSegmentType.Unknown, // Or custom type if supported
                StartTicks = scare.StartTicks,
                EndTicks = scare.EndTicks,
                // Additional metadata
            };

            await _segmentManager.CreateSegmentAsync(
                segment,
                "JumpScareMarkers", // Provider ID
                cancellationToken
            );
        }
    }
}
```

---

## Key Technical Details

### Jellyfin Time Format

- **Ticks:** Jellyfin uses .NET ticks for time measurements
- **Conversion:** 1 second = 10,000,000 ticks
- **Example:** 2 minutes 30 seconds = 1,500,000,000 ticks

```csharp
// Helper conversions
public static class TimeHelpers
{
    private const long TicksPerSecond = 10_000_000;

    public static long SecondsToTicks(double seconds)
        => (long)(seconds * TicksPerSecond);

    public static double TicksToSeconds(long ticks)
        => ticks / (double)TicksPerSecond;

    public static TimeSpan TicksToTimeSpan(long ticks)
        => TimeSpan.FromTicks(ticks);
}
```

### Plugin File Locations

- **Plugins Directory:**
  - Linux: `/var/lib/jellyfin/plugins/`
  - Windows (Direct): `%UserProfile%\AppData\Local\jellyfin\plugins`
  - Windows (Tray): `%ProgramData%\Jellyfin\Server\plugins`
- **Configuration:** `plugins/configurations/`
- **Plugin must restart Jellyfin to load**

### Configuration Storage

- Stored as XML in `plugins/configurations/[PluginName].xml`
- Automatically serialized/deserialized by Jellyfin
- Changes via web UI saved automatically

---

## Development Setup

### Prerequisites

```bash
# Required
- .NET 8.0 SDK
- Git
- IDE: Visual Studio 2022, VS Code, or JetBrains Rider

# Optional but recommended
- Jellyfin server running locally (for testing)
- Docker (for containerized Jellyfin testing)
```

### Initial Setup Steps

1. **Clone the Jellyfin plugin template:**

```bash
git clone https://github.com/jellyfin/jellyfin-plugin-template.git jellyfin-plugin-jumpscare
cd jellyfin-plugin-jumpscare
```

2. **Project structure modifications:**

```bash
# Rename namespace and project files
# Update .csproj file with plugin details
# Modify plugin GUID and metadata
```

3. **Build the plugin:**

```bash
dotnet build
```

4. **Deploy for testing:**

```bash
# Copy DLL to Jellyfin plugins directory
cp bin/Debug/net8.0/Jellyfin.Plugin.JumpScareMarkers.dll \
   /var/lib/jellyfin/plugins/JumpScareMarkers/

# Restart Jellyfin
sudo systemctl restart jellyfin
```

### Debugging Setup

**Visual Studio Code:**

- Attach to running Jellyfin process
- Set breakpoints in plugin code
- Use debug logging for troubleshooting

**Logging:**

```csharp
_logger.LogDebug("Jump scare segment created for item {ItemId}", itemId);
_logger.LogInformation("Scanned {Count} items for jump scares", count);
_logger.LogError(ex, "Failed to create jump scare segment");
```

Enable debug logging in Jellyfin:

```json
// logging.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Jellyfin.Plugin.JumpScareMarkers": "Debug"
      }
    }
  }
}
```

---

## Reference Plugins

### 1. Chapter Segments Provider

**Repository:** https://github.com/jellyfin/jellyfin-plugin-chapter-segments

**Key Learnings:**

- Converts chapters into media segments
- Uses regex patterns for classification
- Implements segment provider interface
- Configuration stored in plugin settings

**Relevant Code Patterns:**

```csharp
// Pattern for identifying segment types
public class PluginConfiguration
{
    public Dictionary<string, string> PatternMappings { get; set; }
    // Maps regex patterns to segment types
}
```

### 2. Intro Skipper

**Repository:** https://github.com/intro-skipper/intro-skipper

**Key Learnings:**

- Audio fingerprinting for automatic detection
- Media segment creation and management
- Complex analysis workflows
- Integration with Jellyfin's segment system

**Architecture Notes:**

- Uses scheduled tasks for analysis
- Stores detection results as segments
- Supports multiple detection methods

### 3. Webhook Plugin

**Repository:** https://github.com/jellyfin/jellyfin-plugin-webhook

**Key Learnings:**

- Event consumer implementation patterns
- REST API endpoint creation
- Configuration page design
- Data serialization/deserialization

---

## API Endpoints (Planned)

### Admin Endpoints

```csharp
[ApiController]
[Route("JumpScareMarkers")]
public class JumpScareController : ControllerBase
{
    // GET: /JumpScareMarkers/Item/{itemId}
    [HttpGet("Item/{itemId}")]
    public async Task<ActionResult<List<JumpScareData>>> GetJumpScares(Guid itemId)

    // POST: /JumpScareMarkers/Item/{itemId}
    [HttpPost("Item/{itemId}")]
    public async Task<ActionResult> AddJumpScare(Guid itemId, JumpScareData data)

    // PUT: /JumpScareMarkers/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateJumpScare(Guid id, JumpScareData data)

    // DELETE: /JumpScareMarkers/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteJumpScare(Guid id)

    // POST: /JumpScareMarkers/Import
    [HttpPost("Import")]
    public async Task<ActionResult> ImportJumpScares(ImportRequest request)

    // GET: /JumpScareMarkers/Export
    [HttpGet("Export")]
    public async Task<ActionResult> ExportJumpScares(ExportFormat format)
}
```

### Client Integration Points

Clients automatically receive segment data through Jellyfin's standard API:

- `GET /Items/{itemId}/Segments` - Returns all segments including jump scares
- Segments include type, start/end times, and metadata
- No custom client code needed for basic display

---

## Data Import/Export Formats

### JSON Format (Primary)

```json
{
  "version": "1.0",
  "exported": "2025-10-31T12:00:00Z",
  "jumpScares": [
    {
      "itemId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "itemName": "The Conjuring (2013)",
      "imdbId": "tt1457767",
      "tmdbId": "138843",
      "scares": [
        {
          "timestamp": "00:23:45",
          "intensity": "Major",
          "description": "Ghost appears suddenly with loud noise",
          "type": "Combined"
        },
        {
          "timestamp": "01:12:30",
          "intensity": "Minor",
          "description": "Mirror reflection scare",
          "type": "Visual"
        }
      ]
    }
  ]
}
```

**Note:** Timestamps are single points in time. The plugin will use configured `StartDeltaSeconds` and `EndDeltaSeconds` to create timeline segments around each timestamp.

### CSV Format (Simple)

```csv
ItemName,IMDb,TMDb,Timestamp,Intensity,Description,Type
The Conjuring (2013),tt1457767,138843,00:23:45,Major,Ghost appears suddenly,Combined
The Conjuring (2013),tt1457767,138843,01:12:30,Minor,Mirror reflection scare,Visual
```

---

## UI/UX Considerations

### Admin Configuration Page

**Requirements:**

- List all movies/shows with jump scare data
- Search and filter functionality
- Add/edit/delete jump scare entries
- Configure global delta settings (start/end seconds)
- Bulk import from file (notscare.me format, JSON, CSV)
- Export current database
- Preview timeline with markers
- notscare.me API sync configuration

**Design Pattern:**
Follow Jellyfin's existing plugin configuration pages:

- Use Jellyfin's web component library
- Maintain consistent styling
- Responsive design for mobile access
- Real-time validation

**Configuration Options:**

```javascript
// Plugin configuration settings
{
  "startDeltaSeconds": -2,      // Seconds before timestamp to start segment
  "endDeltaSeconds": 2,         // Seconds after timestamp to end segment
  "notScareApiUrl": "https://notscare.me/api",
  "enableNotScareSync": false   // Enable automatic sync with notscare.me
}
```

### Timeline Marker Display

**Visual Design:**

- Distinct color (e.g., red/orange for warnings)
- Icon indicator (⚠️ or 👻)
- Size proportional to intensity
- Hover tooltip with details

**Example CSS:**

```css
.sliderMarker.jumpScareMarker {
  background-color: #ff4444 !important;
  border: 2px solid #cc0000;
  width: 4px;
  height: 100%;
}

.sliderMarker.jumpScareMarker.intensity-severe {
  background-color: #ff0000 !important;
  width: 6px;
}

.sliderMarker.jumpScareMarker:hover::after {
  content: attr(data-description);
  position: absolute;
  background: rgba(0, 0, 0, 0.9);
  color: white;
  padding: 8px;
  border-radius: 4px;
  bottom: 100%;
  white-space: nowrap;
}
```

---

## External Data Sources

### Primary Source: notscare.me

**Website:** https://notscare.me
**API Documentation:** https://notscare.me/docs/api-reference
**Database Size:** 9,500+ movies, 100+ TV series

#### Features
- Community-verified timestamps (accurate to the second)
- Two-tier intensity classification: "Major" and "Minor"
- Detailed descriptions for each jump scare
- Support for both movies and TV series (episode-level)
- Public API for programmatic access

#### Data Structure
```typescript
// notscare.me API response structure
interface JumpScare {
  id: string;
  timestamp: number;           // In seconds
  description: string;
  type: "Major" | "Minor";     // Maps to our JumpScareIntensity
  submittedBy: string;
  userName: string;
  createdAt: string;
  updatedAt: string;
  approved: boolean;
  triggers: string[];
  movieId: string;
}
```

#### Integration Plan
1. **Phase 1:** Manual import via JSON export from notscare.me
2. **Phase 2:** Scheduled task to sync with notscare.me API
3. **Phase 3:** Automatic matching for new media items in Jellyfin library

#### Intensity Mapping
```csharp
public static JumpScareIntensity MapNotScareIntensity(string notScareType)
{
    return notScareType switch
    {
        "Major" => JumpScareIntensity.Major,
        "Minor" => JumpScareIntensity.Minor,
        _ => JumpScareIntensity.Minor
    };
}
```

### Alternative Sources

- **Where's The Jump?** (wheresthejump.com) - Popular jump scare database (potential future integration)
- **Does The Dog Die?** (doesthedogdie.com) - Content warnings database
- **Community contributions** - User-submitted data through plugin interface

### Data Matching Strategy

```csharp
public class MediaMatcher
{
    // Match using multiple identifiers
    public async Task<List<JumpScareData>> MatchMedia(BaseItem item)
    {
        // Priority order for matching with notscare.me:
        // 1. IMDb ID (most reliable)
        // 2. TMDb ID
        // 3. Title + Year

        // Query notscare.me API with matched identifier
        // Convert response to our JumpScareData format
        // Apply timestamp conversion and delta configuration
    }
}
```

---

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void TimeConversion_SecondsToTicks_CalculatesCorrectly()
{
    var seconds = 150.5;
    var ticks = TimeHelpers.SecondsToTicks(seconds);
    Assert.Equal(1_505_000_000, ticks);
}

[Fact]
public void JumpScareData_Validation_RejectsInvalidTimes()
{
    var scare = new JumpScareData
    {
        StartTicks = 1000,
        EndTicks = 500 // Invalid: end before start
    };
    Assert.False(scare.IsValid());
}
```

### Integration Tests

- Test segment creation in Jellyfin
- Verify segment retrieval through API
- Test configuration persistence
- Validate scheduled task execution

### Manual Testing Checklist

- [ ] Plugin loads in Jellyfin dashboard
- [ ] Configuration page accessible
- [ ] Add jump scare data manually
- [ ] Segments appear on timeline
- [ ] Hover tooltips display correctly
- [ ] Import/export functionality works
- [ ] Scheduled task executes successfully
- [ ] Multiple concurrent scares handled
- [ ] Performance with large datasets

---

## Performance Considerations

### Database Queries

- Index ItemId for fast lookups
- Cache segment data per item
- Batch operations for imports
- Limit memory footprint for large libraries

### Segment Generation

- Only regenerate when data changes
- Use background tasks for bulk operations
- Implement progress reporting
- Support cancellation

### Client Rendering

- Segments cached by clients
- Minimal data transfer
- Efficient marker positioning calculations

---

## Security Considerations

1. **Authentication:** All API endpoints require authentication
2. **Authorization:** Only admins can modify jump scare data
3. **Input Validation:** Sanitize all user inputs
4. **XSS Prevention:** Escape descriptions in UI
5. **Rate Limiting:** Prevent abuse of import endpoints

---

## Future Enhancements

### Phase 1 (MVP)

- Basic segment creation with timestamp + delta system
- Manual data entry
- Simple timeline markers
- Data model aligned with notscare.me
- Import/export JSON with single timestamp format

### Phase 2

- Enhanced UI with search/filter
- notscare.me manual import support
- CSV import support
- Scheduled scan task for library
- Global delta configuration in admin UI

### Phase 3

- notscare.me API integration
- Automatic sync with notscare.me database
- IMDb/TMDb matching for automatic data population
- Advanced warning notifications
- Statistics and reporting

### Phase 4

- Crowdsourced community contributions
- Mobile app integration
- Multi-language support (using notscare.me's localization)
- Advanced filtering by intensity/type

---

## Known Limitations

1. **Client Support:** Not all Jellyfin clients may render segments perfectly
2. **Manual Data Entry:** Initial data population requires manual effort or import
3. **Media Matching:** Requires accurate metadata (IMDb/TMDb IDs) for matching
4. **Version Compatibility:** Requires Jellyfin 10.10+
5. **Timeline Precision:** Limited by client rendering capabilities

---

## Resources and Documentation

### Official Jellyfin Documentation

- **Plugin Development:** https://jellyfin.org/docs/general/server/plugins/
- **API Documentation:** https://jellyfin.org/docs/plugin-api/
- **Media Segments:** https://jellyfin.org/docs/general/server/metadata/media-segments/
- **Contributing Guide:** https://jellyfin.org/docs/general/contributing/

### GitHub Repositories

- **Plugin Template:** https://github.com/jellyfin/jellyfin-plugin-template
- **Jellyfin Server:** https://github.com/jellyfin/jellyfin
- **Chapter Segments:** https://github.com/jellyfin/jellyfin-plugin-chapter-segments

### Community

- **Forum:** https://forum.jellyfin.org/
- **Matrix Chat:** https://matrix.to/#/#jellyfin:matrix.org
- **Reddit:** r/jellyfin

---

## Quick Start Commands

```bash
# Clone template
git clone https://github.com/jellyfin/jellyfin-plugin-template.git jellyfin-plugin-jumpscare

# Build plugin
cd jellyfin-plugin-jumpscare
dotnet build

# Run tests
dotnet test

# Create release package
dotnet publish -c Release

# Deploy to Jellyfin
sudo cp bin/Release/net8.0/publish/*.dll /var/lib/jellyfin/plugins/JumpScareMarkers/
sudo systemctl restart jellyfin

# View logs
sudo journalctl -u jellyfin -f | grep JumpScare
```

---

## Project Status Tracking

### Milestones

- [ ] Project setup and scaffolding
- [ ] Core data models implemented
- [ ] Segment provider functional
- [ ] Basic UI created
- [ ] Import/export working
- [ ] Testing completed
- [ ] Documentation finalized
- [ ] First release published

### Current Priorities

1. Set up project structure from template
2. Implement core data models
3. Create segment provider
4. Build admin configuration page
5. Test segment display in clients

---

## Contact and Contribution

**Project Goals:**

- Open source and community-driven
- User privacy and data control
- Accessibility for scare-sensitive viewers
- Easy contribution and data sharing

**Contribution Welcome:**

- Code improvements
- Jump scare data submissions
- UI/UX enhancements
- Documentation updates
- Bug reports and testing

---

_Last Updated: 2025-10-31_
_Document Version: 1.0_
_Target Jellyfin Version: 10.10+_
