using System;
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

namespace Plugin.StringAnalyzer {
    // Adds the 'OpenToolWindow' command to the main window and sets keyboard shortcut to Ctrl+Alt+Z
    [ExportAutoLoaded]
    internal sealed class ToolWindowLoader : IAutoLoaded
    {
        public static readonly RoutedCommand OpenToolWindow =
            new RoutedCommand("OpenToolWindow", typeof(ToolWindowLoader));

        [ImportingConstructor]
        private ToolWindowLoader(IWpfCommandService wpfCommandService, IDsToolWindowService toolWindowService)
        {
            var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);
            cmds.Add(OpenToolWindow, new RelayCommand(a => toolWindowService.Show(ToolWindowContent.TheGuid)));
            cmds.Add(OpenToolWindow, ModifierKeys.Control | ModifierKeys.Alt, Key.Z);
        }
    }

    // Adds a menu item to the View menu to show the tool window
    [ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "Strings Analyzer",
        InputGestureText = "Ctrl+Alt+Z", Icon = dnSpy.Contracts.Images.DsImagesAttribute.String,
        Group = MenuConstants.GROUP_APP_MENU_VIEW_WINDOWS, Order = 2000)]
    internal sealed class ViewCommand1 : MenuItemCommand
    {
        private ViewCommand1()
            : base(ToolWindowLoader.OpenToolWindow)
        {
        }
    }

    // Dummy dependency "needed" by MainToolWindowContentProvider
    [Export]
    internal sealed class DeppDep
    {
        public void Hello()
        {
        }
    }

    // Called by dnSpy to create the tool window

    internal sealed class ToolWindowContent : IToolWindowContent
    {
        //TODO: Use your own guid
        public static readonly Guid TheGuid = new Guid("25B02491-85FB-4F63-8DEE-73A9304DFEFB");

        public const AppToolWindowLocation DefaultLocation = AppToolWindowLocation.DefaultHorizontal;

        public Guid Guid => TheGuid;
        public string Title => "Strings Analyzer";
        public object ToolTip => null;

        // This is the object shown in the UI. Return a WPF object or a .NET object with a DataTemplate
        public object UIObject => ToolWindowControl;

        // The element inside UIObject that gets the focus when the tool window should be focused.
        // If it's not as easy as calling FocusedElement.Focus() to focus it, you must implement
        // dnSpy.Contracts.Controls.IFocusable.
        public IInputElement FocusedElement => ToolWindowControl.lvStringsAnalyzer;

        // The element that gets scaled when the user zooms in or out. Return null if zooming isn't
        // possible
        public FrameworkElement ZoomElement => ToolWindowControl;

        public readonly ToolWindowControl ToolWindowControl;
        public readonly ToolWindowVm ToolWindowVm;


        public ToolWindowContent()
        {
            ToolWindowControl = new ToolWindowControl();
            ToolWindowVm = new ToolWindowVm();
            ToolWindowControl.DataContext = ToolWindowVm;
        }

        // Gets notified when the content gets hidden, visible, etc. Can be used to tell the view
        // model to stop doing stuff when it gets hidden in case it does a lot of work.
        public void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent)
        {
            switch (visEvent)
            {
                case ToolWindowContentVisibilityEvent.Added:
                    ToolWindowVm.IsEnabled = true;
                    break;

                case ToolWindowContentVisibilityEvent.Removed:
                    ToolWindowVm.IsEnabled = false;
                    break;

                case ToolWindowContentVisibilityEvent.Visible:
                    ToolWindowVm.IsVisible = true;
                    break;

                case ToolWindowContentVisibilityEvent.Hidden:
                    ToolWindowVm.IsVisible = false;
                    break;
                case ToolWindowContentVisibilityEvent.GotKeyboardFocus:
                    break;
                case ToolWindowContentVisibilityEvent.LostKeyboardFocus:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(visEvent), visEvent, null);
            }
        }
    }

    public class ToolWindowVm : ViewModelBase
    {
        public class StringAnalyzerData
        {
            public string StringValue { get; set; }
            public string IlOffset { get; set; }
            public string MdToken { get; set; }
            public string MdName { get; set; }
            public string FullmdName { get; set; }
        }

        public bool IsEnabled { get; set; }
        public bool IsVisible { get; set; }
    }
}
