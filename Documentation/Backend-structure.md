# Overview
dnSpyStringsAnalyzer mostly runs inside the dnSpy environment, but it has some backend-like operations, particularly around data handling for the extracted strings.

# Architecture

## dnSpy Environment
• dnSpy provides APIs for debugging, disassembly, and presenting data in tool windows.
• dnSpyStringsAnalyzer hooks into these APIs to inspect assemblies for strings.

## Data Collection Layer
• The extension works with dnSpy's internal structures (e.g., documents, IL instructions) to extract string values.
• This data is temporarily stored in memory as objects containing metadata (e.g., MD tokens, IL offsets, method names).

## Optional Persistence
• By default, dnSpyStringsAnalyzer does not maintain any database or long-term data store.
• All strings are kept in memory for the user session, unless customized to log or export data.
