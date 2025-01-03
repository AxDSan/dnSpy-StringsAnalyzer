using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;

// Adds a tool window and a command that will show it. The command is added to the View menu and a
// keyboard shortcut is added to the main window. Keyboard shortcut Ctrl+Alt+Z shows the tool window.

namespace Example2.Extension {
	// Adds the 'OpenToolWindow' command to the main window and sets keyboard shortcut to Ctrl+Alt+Z
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

	// Adds a menu item to the View menu to show the tool window
	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "Extension Tool Window", InputGestureText = "Ctrl+Alt+Z", Group = MenuConstants.GROUP_APP_MENU_VIEW_WINDOWS, Order = 2000)]
	sealed class ViewCommand1 : MenuItemCommand {
		ViewCommand1()
			: base(ToolWindowLoader.OpenToolWindow) {
		}
	}

	// Dummy dependency "needed" by MainToolWindowContentProvider
	[Export]
	sealed class DeppDep {
		public void Hello() {
		}
	}

	// Called by dnSpy to create the tool window
	[Export(typeof(IToolWindowContentProvider))]
	sealed class MainToolWindowContentProvider : IToolWindowContentProvider {
		// Caches the created tool window
		ToolWindowContentImpl ToolWindowContent => myToolWindowContent ??= new ToolWindowContentImpl();
		ToolWindowContentImpl? myToolWindowContent;

		// Add any deps to the constructor if needed, else remove the constructor
		[ImportingConstructor]
		MainToolWindowContentProvider(DeppDep deppDep) => deppDep.Hello();

		// Lets dnSpy know which tool windows it can create and their default locations
		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(ToolWindowContentImpl.THE_GUID, ToolWindowContentImpl.DEFAULT_LOCATION, 0, false); }
		}

		// Called by dnSpy. If it's your tool window guid, return the instance. Make sure it's
		// cached since it can be called multiple times.
		public ToolWindowContent? GetOrCreate(Guid guid) {
			if (guid == ToolWindowContentImpl.THE_GUID)
				return ToolWindowContent;
			return null;
		}
	}

	sealed class ToolWindowContentImpl : ToolWindowContent {
		//TODO: Use your own guid
		public static readonly Guid THE_GUID = new Guid("18785447-21A8-41DB-B8AD-0F166AEC0D08");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public override Guid Guid => THE_GUID;
		public override string Title => "Extension Example";

		// This is the object shown in the UI. Return a WPF object or a .NET object with a DataTemplate
		public override object? UIObject => toolWindowControl;

		// The element inside UIObject that gets the focus when the tool window should be focused.
		// If it's not as easy as calling FocusedElement.Focus() to focus it, you must implement
		// dnSpy.Contracts.Controls.IFocusable.
		public override IInputElement? FocusedElement => toolWindowControl.option1TextBox;

		// The element that gets scaled when the user zooms in or out. Return null if zooming isn't
		// possible
		public override FrameworkElement? ZoomElement => toolWindowControl;

		readonly ToolWindowControl toolWindowControl;
		readonly ToolWindowVM toolWindowVM;

		public ToolWindowContentImpl() {
			toolWindowControl = new ToolWindowControl();
			toolWindowVM = new ToolWindowVM();
			toolWindowControl.DataContext = toolWindowVM;
		}

		// Gets notified when the content gets hidden, visible, etc. Can be used to tell the view
		// model to stop doing stuff when it gets hidden in case it does a lot of work.
		public override void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				toolWindowVM.IsEnabled = true;
				break;

			case ToolWindowContentVisibilityEvent.Removed:
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

	sealed class ToolWindowVM : ViewModelBase {
		public string StringOption1 {
			get => stringOption1;
			set {
				if (stringOption1 != value) {
					stringOption1 = value;
					OnPropertyChanged(nameof(StringOption1));
				}
			}
		}
		string stringOption1 = string.Empty;

		public string StringOption2 {
			get => stringOption2;
			set {
				if (stringOption2 != value) {
					stringOption2 = value;
					OnPropertyChanged(nameof(StringOption2));
				}
			}
		}
		string stringOption2 = string.Empty;

		public string StringOption3 {
			get => stringOption3;
			set {
				if (stringOption3 != value) {
					stringOption3 = value;
					OnPropertyChanged(nameof(StringOption3));
				}
			}
		}
		string stringOption3 = string.Empty;

		public string StringOption4 {
			get => stringOption4;
			set {
				if (stringOption4 != value) {
					stringOption4 = value;
					OnPropertyChanged(nameof(StringOption4));
				}
			}
		}
		string stringOption4 = string.Empty;

		public bool IsEnabled { get; set; }
		public bool IsVisible { get; set; }
	}
}
