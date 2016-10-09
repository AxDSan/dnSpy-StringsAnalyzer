﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;

// This file adds custom document tab content when the user clicks on our new AssemblyChildNode tree node.
// This node is created by TreeNodeDataProvider.cs.

namespace Plugin.StringAnalyzer {
	[ExportDocumentTabContentFactory]
	sealed class AssemblyChildNodeTabContentFactory : IDocumentTabContentFactory {
		// Called to create a new IFileTabContent. If it's our new tree node, create a new IFileTabContent for it
		public IDocumentTabContent Create(IDocumentTabContentFactoryContext context) {
			if (context.Nodes.Length == 1 && context.Nodes[0] is AssemblyChildNode)
				return new AssemblyChildNodeTabContent((AssemblyChildNode)context.Nodes[0]);
			return null;
		}

		//TODO: Use your own guid
		static readonly Guid GUID_SerializedContent = new Guid("FC6D2EC8-6FF8-4071-928E-EB07735A6402");

		public IDocumentTabContent Deserialize(Guid guid, ISettingsSection section, IDocumentTabContentFactoryContext context) {
			if (guid == GUID_SerializedContent) {
				// Serialize() doesn't add anything extra to 'section', but if it did, you'd have to
				// get that info here and return null if the serialized data wasn't found.
				var node = context.Nodes.Length == 1 ? context.Nodes[0] as AssemblyChildNode : null;
				if (node != null)
					return new AssemblyChildNodeTabContent(node);
			}
			return null;
		}

		public Guid? Serialize(IDocumentTabContent content, ISettingsSection section) {
			if (content is AssemblyChildNodeTabContent) {
				// There's nothing else we need to serialize it, but if there were, use 'section'
				// to write the info needed by Deserialize() above.
				return GUID_SerializedContent;
			}
			return null;
		}
	}

	sealed class AssemblyChildNodeTabContent : IDocumentTabContent {
		// Initialized by the owner
		public IDocumentTab DocumentTab { get; set; }

		// Returns all nodes used to generate the content
		public IEnumerable<IDocumentTreeNodeData> Nodes {
			get { yield return node; }
		}

		public string Title => node.ToString();
		public object ToolTip => node.ToString();

		readonly AssemblyChildNode node;

		public AssemblyChildNodeTabContent(AssemblyChildNode node) {
			this.node = node;
		}

		// Called when the user opens a new tab
		public IDocumentTabContent Clone() => new AssemblyChildNodeTabContent(node);

		// Gets called to create the UI context. It can be shared by any IFileTabContent in this tab.
		// Eg. there's only one text editor per tab, shared by all IFileTabContents that need a text
		// editor.
		public IDocumentTabUIContext CreateUIContext(IDocumentTabUIContextLocator locator) {
			// This custom view object is shared by all nodes of the same type. If we didn't want it
			// to be shared, we could use 'node' or 'this' as the key.
			var key = node.GetType();
			// var key = node;	// uncomment to not share it

			// If the UI object has already been created, use it, else create it. The object is
			// stored in a weak reference unless you use the other method override.
			return locator.Get(key, () => new AssemblyChildNodeUIContext());
		}

		public void OnShow(IShowContext ctx) {
			// Get the real type, created by CreateUIContext() above.
			var uiCtx = (AssemblyChildNodeUIContext)ctx.UIContext;

			// You could initialize some stuff, eg. update its DataContext or whatever
			uiCtx.Initialize("some input"); // pretend we need to initialize something
		}

		public void OnHide() { }
		public void OnSelected() { }
		public void OnUnselected() { }
	}

	sealed class AssemblyChildNodeUIContext : IDocumentTabUIContext {
		// Initialized by the owner
		public IDocumentTab DocumentTab { get; set; }

		// The element inside UIObject that gets the focus when the tool window should be focused.
		// If it's not as easy as calling FocusedElement.Focus() to focus it, you must implement
		// dnSpy.Contracts.Controls.IFocusable.
		public IInputElement FocusedElement => content;

		// The element in UIObject that gets the scale transform. null can be returned to disable scaling.
		public FrameworkElement ZoomElement => content;

		// The UI object shown in the tab. Should be a WPF control (eg. UserControl) or a .NET object
		// with a DataTemplate.
		public object UIObject => content;

		public void OnHide() { }
		public void OnShow() { }

		readonly ContentPresenter content;
		readonly AssemblyChildNodeVM vm;

		public AssemblyChildNodeUIContext() {
			this.vm = new AssemblyChildNodeVM();
			// A ContentPresenter + DataTemplate is used to show the VM, but you could of course use
			// a UserControl.
			this.content = new ContentPresenter {
				Focusable = true,
				Content = this.vm,
			};
		}

		sealed class MySerializedData {
			public string Value1;
			public bool Value2;

			public MySerializedData(string value1, bool value2) {
				this.Value1 = value1;
				this.Value2 = value2;
			}
		}

		// Called to create a value that can be used by Deserialize(). It's read from the settings file.
		public object CreateSerialized(ISettingsSection section) {
			var value1 = section.Attribute<string>(nameof(MySerializedData.Value1));
			var value2 = section.Attribute<bool?>(nameof(MySerializedData.Value2));
			if (value1 == null || value2 == null)
				return null;

			return new MySerializedData(value1, value2.Value);
		}

		// Saves the value returned by Serialize(). It's saved in the settings file.
		public void SaveSerialized(ISettingsSection section, object obj) {
			var d = obj as MySerializedData;
			if (d == null)
				return;

			section.Attribute(nameof(d.Value1), d.Value1);
			section.Attribute(nameof(d.Value2), d.Value2);
		}

		// Serializes the UI state
		// This is where you'd normally serialize the UI state, eg. position etc, but we'll just save random data
		public object Serialize() => new MySerializedData("Some string", true);

		// Deserializes the UI state created by Serialize()
		public void Deserialize(object obj) {
			var d = obj as MySerializedData;
			if (d == null)
				return;

			// Here's where you'd restore the UI state, eg position etc.
		}

		// Called by AssemblyChildNodeTabContent above to initialize it before it's shown again
		public void Initialize(string s) {
			// here we could initialize something before it's shown again, eg. initialize the DataContext
		}
	}

	sealed class AssemblyChildNodeVM : ViewModelBase {
		public string SomeMessage {
			get { return someMessage; }
			set {
				if (someMessage != value) {
					someMessage = value;
					OnPropertyChanged(nameof(SomeMessage));
				}
			}
		}
		string someMessage = "Hello World";
	}
}
