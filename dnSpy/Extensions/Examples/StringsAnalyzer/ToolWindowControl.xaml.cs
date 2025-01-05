using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using dnlib.DotNet;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.MVVM;
using static StringsAnalyzer.Extension.ToolWindowVM;
using System.Linq;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Documents.TreeView;

namespace StringsAnalyzer.Extension {
	public partial class ToolWindowControl : UserControl {
		private List<StringAnalyzerData> AllItems = new List<StringAnalyzerData>();
		private List<StringAnalyzerData> FilteredItems = new List<StringAnalyzerData>();
		public static ListView stringAnalyzer = new ListView();
		internal IInputElement option1TextBox;

		private string currentSortProperty = "StringValue";
		private bool isAscending = true;

		private GridViewColumnHeader? lastHeaderClicked;
		private ListSortDirection lastDirection = ListSortDirection.Ascending;

		private void SortListView(string propertyName, bool changeDirection = true) {
			if (string.IsNullOrEmpty(propertyName))
				return;

			if (changeDirection) {
				// Toggle direction if same property
				if (propertyName == currentSortProperty)
					isAscending = !isAscending;
				else {
					currentSortProperty = propertyName;
					isAscending = true;
				}
			}

			// Sort the filtered items
			FilteredItems.Sort((a, b) => {
				var comparison = CompareByProperty(a, b, propertyName);
				return isAscending ? comparison : -comparison;
			});

			// Update ListView
			stringAnalyzer = lvStringsAnalyzer;
			stringAnalyzer.ItemsSource = null;
			stringAnalyzer.ItemsSource = FilteredItems;

			// Add visual feedback
			var view = (GridView)lvStringsAnalyzer.View;
			foreach (var column in view.Columns.Cast<GridViewColumn>()) {
				var header = column.Header.ToString() switch {
					"String Value" => "StringValue",
					"IL Offset" => "IlOffset",
					"MD Token" => "MdToken",
					"MD Name" => "MdName",
					"Full MD Name" => "FullmdName",
					"Module" => "ModuleID",
					_ => null
				};

				if (header == propertyName) {
					column.Header = isAscending ? $"{column.Header} ▲" : $"{column.Header} ▼";
				}
				else {
					// Remove arrows from other columns
					column.Header = column.Header.ToString()?.Replace(" ▲", "").Replace(" ▼", "");
				}
			}
		}

		private int CompareByProperty(StringAnalyzerData a, StringAnalyzerData b, string propertyName) {
			return propertyName switch {
				"StringValue" => string.Compare(a.StringValue, b.StringValue, StringComparison.OrdinalIgnoreCase),
				"IlOffset" => string.Compare(a.IlOffset, b.IlOffset, StringComparison.OrdinalIgnoreCase),
				"MdToken" => string.Compare(a.MdToken, b.MdToken, StringComparison.OrdinalIgnoreCase),
				"MdName" => string.Compare(a.MdName, b.MdName, StringComparison.OrdinalIgnoreCase),
				"FullmdName" => string.Compare(a.FullmdName, b.FullmdName, StringComparison.OrdinalIgnoreCase),
				"ModuleID" => string.Compare(a.ModuleID, b.ModuleID, StringComparison.OrdinalIgnoreCase),
				_ => 0
			};
		}

		private readonly Lazy<IDocumentTabService> documentTabService;
		private readonly IDecompilerService decompilerService;
		private System.Text.RegularExpressions.Regex? currentRegex;
		private string currentSearchText = "";
		private bool isRegexSearch;
		private bool isCaseSensitive;

		[ImportingConstructor]
		public ToolWindowControl(Lazy<IDocumentTabService> documentTabService, IDecompilerService decompilerService) {
			this.documentTabService = documentTabService;
			this.decompilerService = decompilerService;
			InitializeComponent();
			
			// Add event handlers
			lvStringsAnalyzer.MouseDoubleClick += LvStringsAnalyzer_MouseDoubleClick;
			lvStringsAnalyzer.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(GridViewColumnHeader_Click));

			// Subscribe to document changes
			documentTabService.Value.DocumentTreeView.DocumentService.CollectionChanged += DocumentService_CollectionChanged;
		}

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) {
			currentSearchText = searchTextBox.Text;
			UpdateSearch();
		}

		private void RegexCheckBox_CheckedChanged(object sender, RoutedEventArgs e) {
			isRegexSearch = regexCheckBox.IsChecked == true;
			UpdateSearch();
		}

		private void CaseSensitiveCheckBox_CheckedChanged(object sender, RoutedEventArgs e) {
			isCaseSensitive = caseSensitiveCheckBox.IsChecked == true;
			UpdateSearch();
		}

		private void UpdateSearch() {
			try {
				if (string.IsNullOrWhiteSpace(currentSearchText)) {
					FilteredItems = new List<StringAnalyzerData>(AllItems);
					searchStatusText.Text = $"Showing all {AllItems.Count} items";
				}
				else {
					if (isRegexSearch) {
						try {
							var options = isCaseSensitive ? 
								System.Text.RegularExpressions.RegexOptions.None : 
								System.Text.RegularExpressions.RegexOptions.IgnoreCase;
							
							currentRegex = new System.Text.RegularExpressions.Regex(currentSearchText, options);
							FilteredItems = AllItems.Where(item => 
								currentRegex.IsMatch(item.StringValue ?? "") ||
								currentRegex.IsMatch(item.MdName ?? "") ||
								currentRegex.IsMatch(item.FullmdName ?? "")
							).ToList();
							
							searchStatusText.Text = $"Found {FilteredItems.Count} matches (Regex)";
						}
						catch (ArgumentException) {
							searchStatusText.Text = "Invalid regular expression";
							FilteredItems = new List<StringAnalyzerData>();
						}
					}
					else {
						var comparison = isCaseSensitive ? 
							StringComparison.Ordinal : 
							StringComparison.OrdinalIgnoreCase;
						
						FilteredItems = AllItems.Where(item =>
							(item.StringValue?.IndexOf(currentSearchText, comparison) >= 0) ||
							(item.MdName?.IndexOf(currentSearchText, comparison) >= 0) ||
							(item.FullmdName?.IndexOf(currentSearchText, comparison) >= 0)
						).ToList();
						
						searchStatusText.Text = $"Found {FilteredItems.Count} matches";
					}
				}

				stringAnalyzer = lvStringsAnalyzer;
				stringAnalyzer.ItemsSource = FilteredItems;
			}
			catch (Exception ex) {
				searchStatusText.Text = $"Search error: {ex.Message}";
			}
		}

		private void AnalyzeButton_Click(object sender, RoutedEventArgs e) {
			AllItems.Clear();
			FilteredItems.Clear();

			// Get the selected node from the document tree view
			var selectedNodes = documentTabService.Value.DocumentTreeView.TreeView.SelectedItems;
			if (selectedNodes == null || selectedNodes.Length == 0) {
				MessageBox.Show("Please select an assembly in the Assembly Explorer");
				return;
			}

			// Get the module from the selected node
			var module = GetModuleFromNode(selectedNodes[0]) as ModuleDefMD;

			if (module == null) {
				MessageBox.Show("No assembly is currently loaded");
				return;
			}

			try {
				var analyzer = new StringAnalyzer(module);
				var results = analyzer.Analyze();

				if (results.Count == 0) {
					MessageBox.Show("No strings found in the current assembly");
					return;
				}

				AllItems.AddRange(results);
				UpdateSearch(); // Apply any existing search filter
			}
			catch (Exception ex) {
				MessageBox.Show($"Error analyzing strings: {ex.Message}");
			}
		}

		private void ClearButton_Click(object sender, RoutedEventArgs e) {
			AllItems.Clear();
			FilteredItems.Clear();
			stringAnalyzer.ItemsSource = null;
			searchTextBox.Clear();
			searchStatusText.Text = "Type to search...";
		}

		private void LvStringsAnalyzer_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			var item = lvStringsAnalyzer.SelectedItem as StringAnalyzerData;
			if (item != null) {
				NavigateToReference(item, false);
			}
		}

		private void MenuItem_GoToReference(object sender, RoutedEventArgs e) {
			var item = lvStringsAnalyzer.SelectedItem as StringAnalyzerData;
			if (item != null) {
				NavigateToReference(item, false);
			}
		}

		private void MenuItem_GoToReferenceNewTab(object sender, RoutedEventArgs e) {
			var item = lvStringsAnalyzer.SelectedItem as StringAnalyzerData;
			if (item != null) {
				NavigateToReference(item, true);
			}
		}

		private void MenuItem_CopyValue(object sender, RoutedEventArgs e) {
			var item = lvStringsAnalyzer.SelectedItem as StringAnalyzerData;
			if (item?.StringValue != null) {
				try {
					Clipboard.SetText(item.StringValue);
				}
				catch (Exception) { }
			}
		}

		private void MenuItem_CopyFullInfo(object sender, RoutedEventArgs e) {
			var item = lvStringsAnalyzer.SelectedItem as StringAnalyzerData;
			if (item != null) {
				try {
					var info = $"String: {item.StringValue}\n" +
						$"IL Offset: {item.IlOffset}\n" +
						$"Method: {item.FullmdName}\n" +
						$"Token: {item.MdToken}\n" +
						$"Module: {item.ModuleID}";
					Clipboard.SetText(info);
				}
				catch (Exception) { }
			}
		}

		private void NavigateToReference(StringAnalyzerData item, bool newTab) {
			if (item.Method != null && item.Offset.HasValue) {
				documentTabService.Value.FollowReference(item.Method, newTab);
			}
		}

		private ModuleDef? GetModuleFromNode(TreeNodeData node) {
			// Handle different node types that might contain a module
			if (node is ModuleDocumentNode moduleNode)
				return moduleNode.Document.ModuleDef;
			if (node is AssemblyDocumentNode assemblyNode)
				return assemblyNode.Document.ModuleDef;
			if (node is DsDocumentNode documentNode)
				return documentNode.Document.ModuleDef;
			
			return null;
		}

		private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e) {
			if (e.OriginalSource is GridViewColumnHeader headerClicked && headerClicked.Column is GridViewColumn column) {
				string propertyName = column.Header.ToString()?.Replace(" ▲", "").Replace(" ▼", "") switch {
					"String Value" => "StringValue",
					"IL Offset" => "IlOffset",
					"MD Token" => "MdToken",
					"MD Name" => "MdName",
					"Full MD Name" => "FullmdName",
					"Module" => "ModuleID",
					_ => null
				};

				if (propertyName != null) {
					SortListView(propertyName);
				}
			}
		}

		private void DocumentService_CollectionChanged(object? sender, EventArgs e) {
			// Clear results when documents change
			AllItems.Clear();
			FilteredItems.Clear();
			stringAnalyzer.ItemsSource = null;
			searchTextBox?.Clear();
			if (searchStatusText != null)
				searchStatusText.Text = "Type to search...";

			// Update button state
			if (analyzeButton != null)
				analyzeButton.IsEnabled = documentTabService.Value.DocumentTreeView.DocumentService.GetDocuments().Any();
		}

		public void OnClose() {
			// Unsubscribe from events
			if (documentTabService.Value != null) {
				documentTabService.Value.DocumentTreeView.DocumentService.CollectionChanged -= DocumentService_CollectionChanged;
			}
		}
	}
}
