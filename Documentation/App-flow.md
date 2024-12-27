# Overview
This document outlines the general user flow for the dnSpyStringsAnalyzer extension within dnSpy. Although this extension does not manage login or authentication, the following steps summarize how a user (or an automated script) typically interacts with dnSpyStringsAnalyzer.

# Flow of the Application

## User Install/Startup
• The user installs or enables the dnSpyStringsAnalyzer extension in dnSpy.
• On startup, dnSpy loads all active extensions, including dnSpyStringsAnalyzer.

## Loading an Assembly
• The user opens an assembly (DLL or EXE) in dnSpy.
• dnSpyStringsAnalyzer parses that assembly to collect string references or relevant data.

## String Analysis Tool Window
• The user opens the "Strings Analyzer" tool window in dnSpy, where all extracted strings are displayed.
• The extension lets the user filter, sort, or inspect these strings.

## Navigating to IL Offsets
• The user selects a particular string and navigates directly to the corresponding IL offset or method.
• This helps with reverse engineering or debugging tasks without manually browsing through the IL code.

## Closing/Cleanup
• Upon dnSpy's closure or disabling of the extension, dnSpyStringsAnalyzer unloads cleanly.
• User or developer can continue standard dnSpy usage without leftover processes from dnSpyStringsAnalyzer.
