# Build and Test Guide

This guide will walk you through building and testing the Jump Scare Markers plugin MVP.

## Prerequisites

1. **.NET 8.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **Jellyfin Server 10.10+** - Running locally or accessible
3. **Test Movie** - "Weapons (2025)" in your Jellyfin library (or any movie you can use for testing)

## Step 1: Build the Plugin

### Option A: Using Visual Studio
1. Open `Jellyfin.Plugin.JumpScareMarkers.sln` in Visual Studio 2022
2. Build → Build Solution (or press Ctrl+Shift+B)
3. Check the Output window for any errors

### Option B: Using Command Line
```bash
cd d:\Github\jumpscares-jellyfin
dotnet build Jellyfin.Plugin.JumpScareMarkers.sln -c Release
```

### Expected Output
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Build Artifacts Location
```
Jellyfin.Plugin.JumpScareMarkers\bin\Release\net8.0\Jellyfin.Plugin.JumpScareMarkers.dll
```

## Step 2: Deploy to Jellyfin

### Find Your Jellyfin Plugins Directory

**Windows:**
- Direct installation: `%UserProfile%\AppData\Local\jellyfin\plugins`
- Tray app: `%ProgramData%\Jellyfin\Server\plugins`

**Linux:**
- `/var/lib/jellyfin/plugins/`

**Docker:**
- Mapped volume, typically `/config/plugins/`

### Deploy the Plugin

1. **Create plugin directory:**
   ```bash
   # Windows (adjust path as needed)
   mkdir "%ProgramData%\Jellyfin\Server\plugins\JumpScareMarkers"

   # Linux
   sudo mkdir -p /var/lib/jellyfin/plugins/JumpScareMarkers
   ```

2. **Copy DLL:**
   ```bash
   # Windows
   copy Jellyfin.Plugin.JumpScareMarkers\bin\Release\net8.0\Jellyfin.Plugin.JumpScareMarkers.dll "%ProgramData%\Jellyfin\Server\plugins\JumpScareMarkers\"

   # Linux
   sudo cp Jellyfin.Plugin.JumpScareMarkers/bin/Release/net8.0/Jellyfin.Plugin.JumpScareMarkers.dll /var/lib/jellyfin/plugins/JumpScareMarkers/
   ```

## Step 3: Restart Jellyfin

Plugins only load on server startup.

**Windows (Service):**
```powershell
Restart-Service Jellyfin
```

**Linux (systemd):**
```bash
sudo systemctl restart jellyfin
```

**Docker:**
```bash
docker restart jellyfin
```

**Tray App:** Right-click tray icon → Exit → Restart

## Step 4: Verify Plugin Loaded

### Check Dashboard
1. Open Jellyfin web interface
2. Go to **Dashboard** (admin panel)
3. Navigate to **Plugins**
4. Look for "Jump Scare Markers" in the list
5. Click on it to open the configuration page

### Check Logs
Look for plugin initialization messages:

**Windows:**
```
%ProgramData%\Jellyfin\Server\log\log_YYYYMMDD.log
```

**Linux:**
```bash
sudo journalctl -u jellyfin -f | grep JumpScare
```

**Expected log entries:**
```
[INF] Loaded plugin: Jump Scare Markers
[INF] Registered service: JumpScareSegmentProvider
```

## Step 5: Configure Plugin

1. In plugin configuration page, verify default settings:
   - Start Delta: -2 seconds
   - End Delta: 2 seconds
   - notscare.me API URL: https://notscare.me/api

2. Click "Save Configuration"

## Step 6: Prepare Test Data

### Option A: Use Weapons (2025) Data
If you have "Weapons (2025)" in your library:
1. Navigate to `examples/weapons-2025.csv`
2. This file is ready to import

### Option B: Create Custom Test Data
If you don't have Weapons, create a minimal CSV for a movie you do have:

```csv
ItemName,IMDb,TMDb,Timestamp,Intensity,Description,Type
YourMovie (2024),tt1234567,999999,00:05:00,Major,Test scare,Visual
YourMovie (2024),tt1234567,999999,00:10:30,Minor,Another test,Audio
```

Replace "YourMovie (2024)" with the exact name as it appears in Jellyfin.

## Step 7: Import Jump Scare Data

### Using Admin UI
1. In plugin configuration page, scroll to "Import Jump Scare Data"
2. Click "Choose File" and select `examples/weapons-2025.csv`
3. Click "Import CSV"
4. Wait for success message

### Expected Result
```
Successfully imported 17 jump scares, skipped 0 duplicates
Total Rows: 17
Imported: 17
Skipped: 0
```

### Troubleshooting Import Failures

**"No match found for item":**
- Movie name doesn't match exactly
- Movie doesn't have IMDb/TMDb metadata
- Movie not in library

**Solution:** Check exact movie name in Jellyfin library and update CSV

## Step 8: Verify Data Import

### Check Statistics
In the configuration page, the "Database Statistics" section should show:
```
Total Jump Scares: 17
Items with Scares: 1
Major Scares: 3
Minor Scares: 14
```

### Check Jump Scare List
Scroll to "Current Jump Scares" - you should see a table with all 17 entries showing:
- Item name
- Timestamp
- Intensity
- Type
- Description

## Step 9: Test API Endpoints (Optional)

Get your API key from Jellyfin:
1. Dashboard → API Keys → Create new key

### Test Statistics Endpoint
```bash
curl "http://localhost:8096/JumpScareMarkers/Statistics" \
  -H "Authorization: MediaBrowser Token=YOUR_API_KEY"
```

### Test Get All Scares
```bash
curl "http://localhost:8096/JumpScareMarkers/All" \
  -H "Authorization: MediaBrowser Token=YOUR_API_KEY"
```

### Test Get Scares for Specific Item
First, find your item ID:
```bash
curl "http://localhost:8096/Items?SearchTerm=Weapons&IncludeItemTypes=Movie" \
  -H "Authorization: MediaBrowser Token=YOUR_API_KEY"
```

Then query jump scares:
```bash
curl "http://localhost:8096/JumpScareMarkers/Item/{ITEM_ID}" \
  -H "Authorization: MediaBrowser Token=YOUR_API_KEY"
```

## Step 10: Test Segment Display

This is the critical test - do markers actually appear on the timeline?

### Prerequisites
- Movie must be in library
- Jump scares must be imported for that movie
- Client must support media segments (web, Android TV, Roku do)

### Testing Steps

1. **Open Jellyfin web client**
2. **Navigate to the movie** (Weapons 2025 or your test movie)
3. **Start playback**
4. **Look at the progress bar/timeline**

### What to Look For

**Expected behavior:**
- Small markers/indicators on the timeline at jump scare timestamps
- Markers appear at timestamps ± configured deltas
- For Weapons (2025), first marker should appear around 11:51 (00:11:51)

**Marker appearance:**
- May be colored differently (depends on Jellyfin theme)
- May show as small vertical lines
- Hovering might show tooltip (client-dependent)

### Troubleshooting: No Markers Appearing

#### Check 1: Verify Segment Provider is Being Called
Add logging to `JumpScareSegmentProvider.cs`:

```csharp
public Task<IReadOnlyList<MediaSegmentDto>> GetMediaSegments(...)
{
    _logger.LogWarning("GetMediaSegments called for ItemId: {ItemId}", request.ItemId);
    // ... rest of method
}
```

Rebuild, redeploy, restart, and check logs during playback.

#### Check 2: Verify Item ID Matches
```bash
# Get the ItemId from your imported data
curl "http://localhost:8096/JumpScareMarkers/All" -H "Authorization: ..."

# Get the actual ItemId from Jellyfin
curl "http://localhost:8096/Items?SearchTerm=Weapons" -H "Authorization: ..."
```

If IDs don't match, your import didn't find the correct item.

#### Check 3: Check Jellyfin Logs for Segment Errors
```bash
sudo journalctl -u jellyfin -n 100 | grep -i segment
```

Look for errors related to segment providers.

#### Check 4: Verify Client Supports Segments
- Web client: YES (10.10+)
- Android TV: YES
- Roku: YES
- iOS/Android mobile: MAYBE (check version)
- Desktop apps: MAYBE

Try the web client first as it has the best support.

#### Check 5: Media Segments API Version
Ensure you're running Jellyfin 10.10.0 or higher:
```
Dashboard → System → About
```

## Expected Timeline Markers

For Weapons (2025) with default deltas (-2s, +2s):

| Scare Time | Segment Start | Segment End | Intensity |
|------------|---------------|-------------|-----------|
| 00:11:51   | 00:11:49      | 00:11:53    | Minor     |
| 00:27:38   | 00:27:36      | 00:27:40    | Major     |
| 00:41:10   | 00:41:08      | 00:41:12    | Major     |
| 01:51:49   | 01:51:47      | 01:51:51    | Major     |
| ... | ... | ... | ... |

## Common Issues and Solutions

### Issue: Build Errors - "Package not found"
**Solution:** Ensure you have internet connection. NuGet needs to download Jellyfin packages.
```bash
dotnet restore
dotnet build
```

### Issue: "Plugin not loading"
**Solutions:**
1. Check file is in correct plugins directory
2. Check file permissions (Linux: `sudo chown jellyfin:jellyfin ...`)
3. Check Jellyfin logs for specific error
4. Verify .NET 8.0 runtime is installed on server

### Issue: "Service not registered" runtime error
**Solution:** Verify `PluginServiceRegistrator.cs` exists and implements `IPluginServiceRegistrator`

### Issue: Import succeeds but no segments appear
**Solutions:**
1. ItemId mismatch - check import logs
2. Segment provider not being called - add logging
3. Client doesn't support segments - try web client
4. Jellyfin version < 10.10.0

### Issue: CSV import fails - "No match found"
**Solutions:**
1. Check exact movie name in Jellyfin (case-sensitive)
2. Ensure movie has IMDb/TMDb metadata
3. Update CSV with correct ItemName
4. Check `ItemMatchingService` logs for match attempts

## Success Criteria

✅ Plugin appears in Jellyfin dashboard
✅ Configuration page loads without errors
✅ CSV import succeeds with expected count
✅ Statistics show correct numbers
✅ Jump scare list displays in admin UI
✅ Markers appear on timeline during playback
✅ Markers appear at correct timestamps

## Next Steps After Successful Test

1. **Document any issues** encountered during testing
2. **Test with multiple movies** to verify matching logic
3. **Test different delta values** (e.g., -5s to +5s)
4. **Test JSON import** (Phase 2 feature)
5. **Test on different clients** (Android TV, Roku)
6. **Consider Phase 2 features:**
   - JSON import support
   - Manual add/edit/delete UI
   - notscare.me API integration
   - Scheduled sync tasks

## Getting Help

If you encounter issues:

1. **Check Jellyfin logs** first
2. **Enable debug logging** for the plugin
3. **Test API endpoints directly** to isolate issues
4. **Verify data with curl/Postman** before blaming the UI
5. **Check Jellyfin version compatibility**

## Debug Logging

To enable verbose logging, add to Jellyfin's `logging.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "Jellyfin.Plugin.JumpScareMarkers": "Debug"
      }
    }
  }
}
```

Restart Jellyfin to apply.

---

**Good luck with your testing!** 🎬👻
