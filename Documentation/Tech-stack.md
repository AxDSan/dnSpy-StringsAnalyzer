# Overview
This document lists the main technologies and frameworks used by dnSpyStringsAnalyzer.

# Technologies

## Language & Framework
• C# (Targeting .NET Framework 4.6)
• WPF (Windows Presentation Foundation) for user interfaces

## dnSpy Integration Points
• dnSpy APIs for tool window creation, document analysis, and assembly exploration.
• dnlib: A library for reading and manipulating .NET assemblies.

## Dependencies & Libraries
• dnSpy.Contracts.*: Contains extension points to integrate seamlessly with dnSpy's tool windows and UI.
• Microsoft .NET references: System.ComponentModel.Composition (MEF), System.Xaml, etc.

## Development & Build
• Visual Studio (2019 or 2022 recommended) or an equivalent MSBuild environment.
• Git for version control (if shared in a repository).

## Future Considerations
• Potential for extra functionalities like string searching or pattern matching.
• Additional UI enhancements, without adding external dependencies that complicate the build process.
