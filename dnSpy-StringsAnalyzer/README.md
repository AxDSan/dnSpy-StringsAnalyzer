
This extension shows how to do more advanced stuff. It:

- Adds a tool window (ToolWindowContent.cs)
- Adds new tree nodes (TreeNodeDataProvider.cs)
- Adds custom tab content for the new AssemblyChildNode tree node (AssemblyChildNodeTabContent.cs). ModuleChildNode implements IDecompileSelf to decompile itself.
- Shows tooltips when hovering over custom references added to the text editor (DocumentViewerToolTipProvider.cs)
- Adds a new IDsDocument instance and IDsDocumentNode node (NewDsDocument.cs). It opens .txt files and shows the output in the text editor.
- Colorizes text in text editors (Colorizer.cs)
- Adds a new Output window text pane (OutputTextPane.cs)
