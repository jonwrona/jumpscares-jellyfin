# Jellyfin Jump Scare Markers Plugin

A Jellyfin plugin that displays visual timeline markers for jump scares in horror movies and TV shows, helping viewers who are sensitive to sudden scares.

## Features

- **Visual Timeline Markers**: Display markers on video progress bars showing when jump scares occur
- **notscare.me Integration**: Import jump scare data from the notscare.me community database (9,500+ movies)
- **Configurable Segments**: Adjust how timeline segments are created around jump scare timestamps
- **Intensity Levels**: Support for Major and Minor jump scare classifications
- **Manual Entry**: Add and manage jump scare data through the admin interface
- **Import/Export**: Support for JSON and CSV data formats

## Requirements

- **Jellyfin**: Version 10.10.0 or higher (required for Media Segments API)
- **.NET**: 8.0 SDK for building

## Installation

### Option 1: From Release (Coming Soon)
1. Download the latest release from the [Releases](https://github.com/jumpscares-jellyfin/jumpscares-jellyfin/releases) page
2. Extract the DLL to your Jellyfin plugins directory
3. Restart Jellyfin

### Option 2: Build from Source

```bash
# Clone the repository
git clone https://github.com/jumpscares-jellyfin/jumpscares-jellyfin.git
cd jumpscares-jellyfin

# Build the plugin
dotnet build Jellyfin.Plugin.JumpScareMarkers.sln -c Release

# Copy to Jellyfin plugins directory
# Linux:
sudo cp Jellyfin.Plugin.JumpScareMarkers/bin/Release/net8.0/Jellyfin.Plugin.JumpScareMarkers.dll /var/lib/jellyfin/plugins/JumpScareMarkers/

# Windows (adjust path as needed):
copy Jellyfin.Plugin.JumpScareMarkers\bin\Release\net8.0\Jellyfin.Plugin.JumpScareMarkers.dll %AppData%\Jellyfin\plugins\JumpScareMarkers\

# Restart Jellyfin
sudo systemctl restart jellyfin  # Linux
```

## Configuration

Access the plugin configuration through the Jellyfin admin dashboard:

1. Navigate to **Dashboard** → **Plugins** → **Jump Scare Markers**
2. Configure the following settings:
   - **Start Delta**: Seconds before the timestamp to start the segment (e.g., -2)
   - **End Delta**: Seconds after the timestamp to end the segment (e.g., 2)
   - **notscare.me API URL**: API endpoint for data sync
   - **Enable notscare.me Sync**: Toggle automatic synchronization

## Data Model

Jump scares are stored with the following information:

- **Timestamp**: Single point in time when the scare occurs (in ticks)
- **Intensity**: Major or Minor classification (aligns with notscare.me)
- **Type**: Visual, Audio, Combined, or Other
- **Description**: Text description of the scare
- **Source**: Origin of the data (notscare.me, manual, import)

### Example JSON Format

```json
{
  "version": "1.0",
  "exported": "2025-10-31T12:00:00Z",
  "jumpScares": [
    {
      "itemId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "itemName": "The Conjuring (2013)",
      "scares": [
        {
          "timestamp": "00:23:45",
          "intensity": "Major",
          "description": "Ghost appears suddenly",
          "type": "Combined"
        }
      ]
    }
  ]
}
```

## How It Works

1. **Timestamp-Based Segments**: Jump scares are stored as single timestamps
2. **Delta Calculation**: Timeline segments are created using configurable start/end deltas
3. **Media Segments API**: Leverages Jellyfin's native Media Segments system
4. **Client Display**: Markers automatically appear on supported Jellyfin clients

## Data Sources

### Primary: notscare.me

- **Database Size**: 9,500+ movies, 100+ TV series
- **Accuracy**: Community-verified timestamps (accurate to the second)
- **API**: Public API for programmatic access
- **Website**: [notscare.me](https://notscare.me)

## Development

### Project Structure

```
Jellyfin.Plugin.JumpScareMarkers/
├── Configuration/
│   ├── PluginConfiguration.cs
│   └── configPage.html
├── Models/
│   ├── JumpScareData.cs
│   ├── JumpScareIntensity.cs
│   ├── JumpScareType.cs
│   └── TimeHelpers.cs
├── Providers/          # (Future: Segment providers)
├── ScheduledTasks/     # (Future: Sync tasks)
├── API/                # (Future: REST endpoints)
└── Plugin.cs
```

### Building

```bash
dotnet build
```

### Testing

```bash
dotnet test
```

## Roadmap

### Phase 1 (MVP) - Current
- ✅ Basic data models
- ✅ Plugin scaffolding
- ✅ Configuration page
- ⏳ Segment provider implementation
- ⏳ Manual data entry UI

### Phase 2
- Import from notscare.me JSON exports
- CSV import/export
- Scheduled library scan task
- Enhanced admin UI with search/filter

### Phase 3
- notscare.me API integration
- Automatic sync and matching
- IMDb/TMDb-based data population
- Warning notifications

### Phase 4
- Community contributions
- Advanced filtering
- Multi-language support

## Contributing

Contributions are welcome! Please feel free to submit:

- Bug reports
- Feature requests
- Pull requests
- Jump scare data submissions

## License

This project is licensed under the GPL-3.0 License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Jellyfin](https://jellyfin.org) - The free software media system
- [notscare.me](https://notscare.me) - Community jump scare database
- All contributors and data submitters

## Support

- **Issues**: [GitHub Issues](https://github.com/jumpscares-jellyfin/jumpscares-jellyfin/issues)
- **Documentation**: See [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md) for detailed technical information

---

**Note**: This plugin is in active development. Features and APIs may change.
