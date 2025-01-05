using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;
using dnlib.DotNet;

namespace StringsAnalyzer.Extension {
	[ExportAutoLoaded]
	sealed class ToolWindowLoader : IAutoLoaded {
		public static readonly RoutedCommand OpenToolWindow = new RoutedCommand("OpenToolWindow", typeof(ToolWindowLoader));

		[ImportingConstructor]
		ToolWindowLoader(IWpfCommandService wpfCommandService, IDsToolWindowService toolWindowService) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);
			cmds.Add(OpenToolWindow, new RelayCommand(a => toolWindowService.Show(ToolWindowContentImpl.THE_GUID)));
			cmds.Add(OpenToolWindow, ModifierKeys.Control | ModifierKeys.Alt, Key.Z);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "Strings Analyzer", InputGestureText = "Ctrl+Alt+Z", Group = MenuConstants.GROUP_APP_MENU_VIEW_WINDOWS, Order = 2000)]
	sealed class ViewCommand1 : MenuItemCommand {
		ViewCommand1()
			: base(ToolWindowLoader.OpenToolWindow) {
		}
	}

	[Export]
	sealed class DeppDep {
		public void Hello() {
		}
	}

	[Export(typeof(IToolWindowContentProvider))]
	sealed class MainToolWindowContentProvider : IToolWindowContentProvider {
		readonly Lazy<IDocumentTabService> documentTabService;
		readonly Lazy<IDecompilerService> decompilerService;

		[ImportingConstructor]
		MainToolWindowContentProvider(Lazy<IDocumentTabService> documentTabService, Lazy<IDecompilerService> decompilerService, DeppDep deppDep) {
			this.documentTabService = documentTabService;
			this.decompilerService = decompilerService;
			deppDep.Hello();
		}

		ToolWindowContentImpl ToolWindowContent => myToolWindowContent ??= new ToolWindowContentImpl(documentTabService, decompilerService);
		ToolWindowContentImpl? myToolWindowContent;

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(ToolWindowContentImpl.THE_GUID, ToolWindowContentImpl.DEFAULT_LOCATION, 0, false); }
		}

		public ToolWindowContent? GetOrCreate(Guid guid) {
			if (guid == ToolWindowContentImpl.THE_GUID)
				return ToolWindowContent;
			return null;
		}
	}

	sealed class ToolWindowContentImpl : ToolWindowContent {
		public static readonly Guid THE_GUID = new Guid("740baa9a-f5ad-40c2-8cf5-90a10584600b");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public override Guid Guid => THE_GUID;
		public override string Title => "Strings Analyzer";
		public override object? UIObject => toolWindowControl;
		public override IInputElement? FocusedElement => toolWindowControl.option1TextBox;
		public override FrameworkElement? ZoomElement => toolWindowControl;

		readonly ToolWindowControl toolWindowControl;
		readonly ToolWindowVM toolWindowVM;

		public ToolWindowContentImpl(Lazy<IDocumentTabService> documentTabService, Lazy<IDecompilerService> decompilerService) {
			toolWindowControl = new ToolWindowControl(documentTabService, decompilerService.Value);
			toolWindowVM = new ToolWindowVM();
			toolWindowControl.DataContext = toolWindowVM;
		}

		public override void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				toolWindowVM.IsEnabled = true;
				break;

			case ToolWindowContentVisibilityEvent.Removed:
				toolWindowControl.OnClose();
				toolWindowVM.IsEnabled = false;
				break;

			case ToolWindowContentVisibilityEvent.Visible:
				toolWindowVM.IsVisible = true;
				break;

			case ToolWindowContentVisibilityEvent.Hidden:
				toolWindowVM.IsVisible = false;
				break;
			}
		}
	}

	public class ToolWindowVM : ViewModelBase {
		public class StringAnalyzerData {
			public string? StringValue { get; set; }
			public string? IlOffset { get; set; }
			public string? MdToken { get; set; }
			public string? MdName { get; set; }
			public string? FullmdName { get; set; }
			public string? ModuleID { get; set; }
			public MethodDef? Method { get; set; }
			public uint? Offset { get; set; }

			public override string ToString() =>
				$"{StringValue} at {IlOffset} in {MdName} ({ModuleID})";
		}

		public bool IsEnabled { get; set; }
		public bool IsVisible { get; set; }
	}
}
