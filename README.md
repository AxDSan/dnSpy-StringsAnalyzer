# dnSpy-StringsAnalyzer
Plugin for DnSpy - Analyze assemblies and display strings.
---
![dnSpy Strings Analyzer - Plugin in Action](https://i.imgur.com/tP6JNXu.gif)
---

The StringsAnalyzer plugin is a powerful tool for analyzing string literals in .NET assemblies within dnSpy. It provides a comprehensive view of all string values along with their metadata and locations within the assembly.

## Features

- **String Extraction**: Finds all string literals in a .NET assembly
- **Detailed Metadata**: Shows IL offset, method information, and module details for each string
- **Sorting**: Sort strings by value, IL offset, method name, or other metadata
- **Search**: 
  - Basic text search
  - Regular expression support
  - Case-sensitive/insensitive options
- **Navigation**: Double-click to navigate to the string's location in the code
- **Copy Functionality**:
  - Copy string value
  - Copy full metadata information
- **Integration**: Works seamlessly with dnSpy's document system

## Installation

1. Build the plugin from source
2. Copy the compiled DLL to dnSpy's `Extensions` folder
3. Restart dnSpy

## Usage

1. Open an assembly in dnSpy
2. Navigate to the StringsAnalyzer tool window
3. Click "Analyze" to extract all strings from the assembly
4. Use the search box to filter results
5. Double-click any result to navigate to its location in the code

## Sorting

Click any column header to sort by that field. Click again to toggle between ascending and descending order.

## Search Options

- **Text Search**: Basic substring matching
- **Regex Search**: Enable regex checkbox for pattern matching
- **Case Sensitivity**: Toggle case sensitivity with the checkbox

## Navigation

- **Double-click**: Navigate to string location in current tab
- **Right-click > Go to Reference**: Same as double-click
- **Right-click > Go to Reference (New Tab)**: Open location in new tab

## Copy Options

- **Copy Value**: Copies just the string value
- **Copy Full Info**: Copies all metadata in a formatted text block

## Contributing

Contributions are welcome! Please follow these guidelines:
1. Fork the repository
2. Create a feature branch
3. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
