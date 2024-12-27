# Overview
This document describes the core organization of the dnSpyStringsAnalyzer project's code files and directories.

# Primary Directories & Files

## Root Directory (dnSpy-StringsAnalyzer)
• Contains the main project files (csproj, solution references, and build configurations).

## /Properties
• AssemblyInfo.cs: Standard assembly metadata.

## /Themes
• resourcedict.xaml: Stores WPF resources and stylistic definitions used by the extension.

# Key Source Files
• ToolWindowControl.xaml and ToolWindowControl.xaml.cs: Defines the UI for the "Strings Analyzer" tool window.
• TreeNodeDataProvider.cs: Injects custom tree nodes (e.g., "ModuleChildNode") into dnSpy's assembly explorer.
• OutputTextPane.cs: Adds a custom output pane in dnSpy's Output window, useful for logging or debugging messages from the extension.
• StringAnalyzer.cs: Main entry point exporting the extension (IExtension) implementing any necessary initialization code.
• Other *.cs files: Various helper classes, contexts, and data models for handling assembly references, string extraction, and UI interactions.

# Additional Files
• README.md: Basic usage notes or placeholders for further instructions.
• .csproj files: Build configurations for the entire extension.
