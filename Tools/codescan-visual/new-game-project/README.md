# CodeScan Visualizer

A Godot project that provides a visual interface for scanning and displaying the contents of `CodeScanList.json` files. This tool helps developers understand and navigate through code scanning configurations in a user-friendly manner.

## Features

### Basic Visualizer (`CodeScanVisualizer.cs`)
- **Tree View**: Hierarchical display of all CodeScan data
- **Search Functionality**: Filter namespaces, types, methods, and fields
- **Expand/Collapse**: Control tree expansion state
- **Refresh**: Reload data from the JSON file
- **Three-column Layout**: Name, Type, and Details columns

### Enhanced Visualizer (`CodeScanVisualizerEnhanced.cs`)
- **Tabbed Interface**: Tree view and JSON viewer tabs
- **Statistics Display**: Real-time statistics showing counts of namespaces, types, methods, etc.
- **Export Functionality**: Generate detailed statistics reports
- **Enhanced UI**: Icons and better visual organization
- **JSON Viewer**: Raw JSON display with basic formatting

## Project Structure

```
new-game-project/
├── CodeScanList.json          # The JSON file to be scanned
├── CodeScanData.cs            # Data structures for JSON serialization
├── CodeScanParser.cs          # JSON parsing and utility functions
├── CodeScanVisualizer.cs      # Basic visualizer implementation
├── CodeScanVisualizerEnhanced.cs # Enhanced visualizer with additional features
├── Main.tscn                  # Basic scene file
├── MainEnhanced.tscn          # Enhanced scene file
├── project.godot              # Godot project configuration
└── README.md                  # This file
```

## Data Structure

The visualizer parses the following structure from `CodeScanList.json`:

- **AllowedVerifierErrors**: List of allowed verification errors
- **WhitelistedNamespaces**: List of whitelisted namespace names
- **WhitelistedAssembliesDEBUG**: List of whitelisted assembly names for debug builds
- **Types**: Dictionary of namespaces, each containing:
  - **Types**: Dictionary of type definitions, each containing:
    - **All**: Boolean indicating if all members are allowed
    - **Inherit**: Inheritance policy ("Allow", "Block", etc.)
    - **Methods**: List of allowed method signatures
    - **Fields**: List of allowed field names
    - **NestedTypes**: Dictionary of nested type definitions

## Usage

### Running the Project

1. Open the project in Godot 4.4+
2. Ensure `CodeScanList.json` is in the project root
3. Run the project (F5 or Play button)

### Using the Basic Visualizer

1. The tree will automatically load and display all data
2. Use the search box to filter items
3. Click the expand/collapse buttons to control tree state
4. Use the refresh button to reload data

### Using the Enhanced Visualizer

1. Switch between "Tree View" and "JSON Viewer" tabs
2. View real-time statistics in the top bar
3. Use the export button to generate a detailed report
4. All basic functionality is available plus enhanced features

### Search Tips

- Search is case-insensitive
- Searches across names, types, and details
- Empty search shows all items
- Parent items remain visible if children match

## Configuration

### Changing the JSON File Path

Edit the `JsonFilePath` property in either visualizer script:

```csharp
[Export] public string JsonFilePath = "res://CodeScanList.json";
```

### Customizing the Display

Modify the `PopulateTree()` method to change how data is displayed, or adjust the column configuration in `SetupUI()`.

## Export Functionality

The enhanced visualizer can export detailed statistics to a text file in the user data directory. The report includes:

- Summary statistics
- Detailed breakdown by namespace
- Type-level information
- Method and field counts
- Nested type information

## Technical Details

### Dependencies

- **Godot 4.4+**: Required for C# support and UI components
- **System.Text.Json**: Used for JSON parsing
- **.NET 6+**: Required for C# features

### Performance

- Large JSON files are loaded efficiently using streaming
- Tree view uses lazy loading for better performance
- Search filtering is optimized for real-time use

### Error Handling

- Graceful handling of missing or malformed JSON files
- User-friendly error messages
- Fallback to empty data structure on errors

## Development

### Adding New Features

1. Extend the `CodeScanData` classes for new data types
2. Update the parser in `CodeScanParser.cs`
3. Modify the visualizer to display new data
4. Add appropriate UI controls

### Customizing the UI

The UI is built programmatically in C#, making it easy to modify:
- Change colors and styling
- Add new controls
- Modify layout
- Add new tabs or panels

## Troubleshooting

### Common Issues

1. **JSON not loading**: Check file path and JSON syntax
2. **Performance issues**: Large files may take time to load
3. **UI not displaying**: Ensure Godot 4.4+ is being used
4. **C# errors**: Verify .NET 6+ is installed

### Debug Information

Enable debug output by checking the Godot console for error messages and loading status information.

## License

This project is provided as-is for educational and development purposes. 