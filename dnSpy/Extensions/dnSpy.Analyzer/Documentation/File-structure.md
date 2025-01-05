# dnSpy Analyzer Extension - File Structure

## Directory Layout

```
Extensions/
└── dnSpy.Analyzer/
    ├── Properties/
    ├── TreeNodes/
    ├── AnalyzerService.cs
    ├── AnalyzerSettings.cs
    ├── AnalyzerToolWindowContent.cs
    ├── AnalyzerTreeNodeDataContext.cs
    ├── Commands.cs
    ├── ContentTypeDefinitions.cs
    └── TheExtension.cs
```

## Key Files

- **AnalyzerService.cs:** Main service implementation
- **TreeNodes/:** Contains analysis node classes
- **AnalyzerSettings.cs:** Manages user preferences
- **TheExtension.cs:** Extension entry point

## Organization Principles

- Separation of concerns
- Modular design
- Clear naming conventions
- Logical grouping of related files
