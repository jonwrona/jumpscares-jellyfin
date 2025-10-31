# Example Data - Weapons (2025)

This directory contains sample jump scare data extracted from notscare.me for testing and demonstrating the Jump Scare Markers plugin.

## Files

### weapons-2025.csv
CSV format with all 17 jump scares from the movie "Weapons" (2025).

**Columns:**
- `ItemName` - Movie title and year
- `IMDb` - IMDb ID (tt26581740)
- `TMDb` - TMDb ID (1078605)
- `Timestamp` - Time when the scare occurs (HH:MM:SS or MM:SS)
- `Intensity` - Major or Minor
- `Description` - Description of the jump scare
- `Type` - Visual, Audio, Combined, or Other

### weapons-2025.json
JSON format following the plugin's documented import structure.

**Structure:**
```json
{
  "version": "1.0",
  "exported": "timestamp",
  "jumpScares": [
    {
      "itemName": "Movie Title",
      "imdbId": "tt12345",
      "tmdbId": "67890",
      "scares": [
        {
          "timestamp": "HH:MM:SS",
          "intensity": "Major|Minor",
          "description": "Description text",
          "type": "Visual|Audio|Combined|Other"
        }
      ]
    }
  ]
}
```

## Movie Information

- **Title:** Weapons
- **Year:** 2025
- **Runtime:** 129 minutes
- **IMDb ID:** tt26581740
- **TMDb ID:** 1078605
- **Total Jump Scares:** 17
  - Major: 3
  - Minor: 14

## Data Source

This data was extracted from [notscare.me](https://notscare.me/movies/jump-scares-in-weapons-2025) on 2025-10-31.

## Type Classification

I've classified the scares based on the descriptions:

- **Audio:** Scares primarily involving sudden sounds (knocks, bangs, audio stingers)
- **Visual:** Scares primarily visual (sudden appearances, face changes)
- **Combined:** Scares with both visual and audio elements
- **Other:** Miscellaneous scares not fitting the above categories

## Using This Data

### For Testing CSV Import
1. Use `weapons-2025.csv` to test CSV parsing logic
2. The plugin should:
   - Parse each row
   - Convert timestamps to ticks
   - Map IMDb/TMDb IDs to Jellyfin ItemIds
   - Create JumpScareData entries

### For Testing JSON Import
1. Use `weapons-2025.json` to test JSON import
2. The plugin should:
   - Validate JSON structure
   - Match movies by IMDb/TMDb ID
   - Convert timestamps to ticks
   - Apply delta configuration to create segments

### Expected Behavior

With default delta settings (-2 seconds / +2 seconds):
- A scare at `00:11:51` (711 seconds) should create a segment from:
  - Start: `00:11:49` (709 seconds = 7,090,000,000 ticks)
  - End: `00:11:53` (713 seconds = 7,130,000,000 ticks)

### Timestamp to Ticks Conversion

```csharp
// Example: 01:04:00 = 1 hour, 4 minutes, 0 seconds
// = (1 * 3600) + (4 * 60) + 0 = 3840 seconds
// = 3840 * 10,000,000 = 38,400,000,000 ticks
```

## Notes

- The Type field is my interpretation based on descriptions - notscare.me doesn't provide this categorization
- Timestamps are exact as provided by notscare.me
- Some descriptions mention "well-telegraphed" which could affect intensity perception
- The plugin should handle both MM:SS and HH:MM:SS formats
