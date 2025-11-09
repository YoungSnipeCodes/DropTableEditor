# ROSE Online Drop Table Editor

A standalone WPF application for editing ROSE Online drop tables (ITEM_DROP.STB). This tool provides a user-friendly interface to view, search, and modify monster drop tables.
AruaMapEditor Was used as a base for somo code linking, and this was later removed for publishing as a independant app.

## Features

- **Browse and Load**: Select your ROSE Online 3DDATA folder to load drop tables
- **Search Functionality**: Search by table number, item name, or NPC name
- **Edit Drop Tables**: Modify drop rates, items, and quantities for any monster
- **Add/Remove Tables**: Create new drop tables or remove existing ones
- **Auto-Save**: Changes are automatically saved to ITEM_DROP.STB
- **Path Memory**: Remembers your last used 3DDATA folder for quick access

## Requirements

- Windows OS
- .NET Framework 3.5 or higher
- ROSE Online 3DDATA folder with STB files

## Installation

1. Download `DropTableEditor.exe` and `DropTableEditor.exe.config`
2. Place both files in the same folder
3. Run `DropTableEditor.exe`

## Usage

### First Launch
1. Click **Browse** or type your ROSE Online 3DDATA folder path
2. Press **Enter** or click **Browse** to load the files
3. Click **Open Drop Table Editor** to start editing

### Subsequent Launches
- The app remembers your last folder path
- Files are automatically loaded on startup
- Just click **Open Drop Table Editor** to continue

### Editing Drop Tables
1. Use the search box to find specific tables, items, or NPCs
2. Select a drop table from the list
3. Edit drop rates, items, and quantities in the data grid
4. Click **Save** to write changes to ITEM_DROP.STB
5. Use **Add Table** or **Remove Table** to manage drop tables

## File Structure

```
DropTableEditor.exe              # Main application
DropTableEditor.exe.config       # .NET configuration (required)
DropTableEditor.config           # User settings (auto-created)
```

### Configuration File

`DropTableEditor.config` is auto-created on first launch and stores your settings in JSON format:

```json
{
  "LastDataPath": "C:\\path\\to\\3Ddata"
}
```

You can manually edit this file to change the default folder path.

## Supported Files

The editor loads the following files from your 3DDATA/STB folder:

- **ITEM_DROP.STB** - Main drop table data
- **LIST_NPC.STB** / **LIST_NPC_S.STL** - NPC names
- **Item Category Files** (14 categories):
  - ARMS, BACK, BODY, CAP, FACE, FACEITEM, FOOT
  - JEMITEM, JEWEL, PAT, SUBWPN, USEITEM, WEAPON, ZONE

## Building from Source

### Prerequisites
- Visual Studio 2015 or higher
- .NET Framework 3.5 SDK
- MSBuild

### Build Steps
```powershell
msbuild DropTableEditor.sln /p:Configuration=Release /t:Build
```

Output will be in `bin/Release/`

### Debug Build
```powershell
msbuild DropTableEditor.sln /p:Configuration=Debug /t:Build
```

Or press **F5** in VS Code with the included launch configuration.

## Project Structure

```
DropTableEditor/
├── Engine/
│   ├── FileManager.cs          # File loading management
│   └── Data/
│       ├── STB.cs              # STB file format handler
│       ├── STL.cs              # STL file format handler
│       └── FileHandler.cs      # Binary file I/O
├── Windows/
│   ├── MainWindow.xaml         # Launcher UI
│   ├── MainWindow.xaml.cs      # Launcher logic
│   ├── DropTableViewer.xaml    # Editor UI
│   └── DropTableViewer.xaml.cs # Editor logic
├── Misc/
│   └── Output.cs               # Debug output helper
├── AppConfig.cs                # Configuration manager
└── App.xaml                    # Application entry point
```

## Technical Details

- **Platform**: Windows (.NET Framework 3.5)
- **Architecture**: x86/AnyCPU
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Build System**: MSBuild
- **File Format**: Binary STB/STL files with custom encoding

## Troubleshooting

### "Configuration system failed to initialize"
- Ensure `DropTableEditor.exe.config` is in the same folder as the .exe

### "Error: STB folder not found"
- Verify you selected the correct 3DDATA folder (not the STB subfolder)
- The path should contain a `STB` subfolder with `ITEM_DROP.STB` inside

### Files don't load on startup
- Check that `DropTableEditor.config` contains a valid path
- Manually delete the config file to reset settings

### Path keeps adding backslashes
- This was fixed in the latest version
- Delete your `DropTableEditor.config` and let it regenerate

## Contributing

Contributions are welcome! Please ensure:
- Code follows existing style and patterns
- Build succeeds with 0 errors
- Test with various ROSE Online versions (iRose, eRose,jRose!, etc.)

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Credits

Created as a standalone extraction from A very heavily modified version of AruaMapEditor project.

## Support

For issues, questions, or feature requests, please open an issue on the GitHub repository.
