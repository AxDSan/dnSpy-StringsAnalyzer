using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.ToolWindows.App;

namespace dnSpy.StringsAnalyzer
{
    [ExportAutoLoaded]
    sealed class StringsAnalyzerCommandLoader : IAutoLoaded
    {
        [ImportingConstructor]
        StringsAnalyzerCommandLoader(IWpfCommandService wpfCommandService, IDsToolWindowService toolWindowService)
        {
            //var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);
            //cmds.Add(StringsAnalyzerRoutedCommand.DeleteAllBreakpoints, (s, e) => breakpointService.Value.ClearAskUser(), (s, e) => e.CanExecute = breakpointService.Value.CanClear, ModifierKeys.Control | ModifierKeys.Shift, Key.F9);
            //cmds.Add(StringsAnalyzerRoutedCommand.ToggleBreakpoint, (s, e) => breakpointService.Value.ToggleBreakpoint(), (s, e) => e.CanExecute = breakpointService.Value.CanToggleBreakpoint, ModifierKeys.None, Key.F9);
            //cmds.Add(StringsAnalyzerRoutedCommand.DisableBreakpoint, (s, e) => breakpointService.Value.DisableBreakpoint(), (s, e) => e.CanExecute = breakpointService.Value.CanDisableBreakpoint, ModifierKeys.Control, Key.F9);
            //cmds.Add(StringsAnalyzerRoutedCommand.DisableAllBreakpoints, (s, e) => breakpointService.Value.DisableAllBreakpoints(), (s, e) => e.CanExecute = breakpointService.Value.CanDisableAllBreakpoints);
            //cmds.Add(StringsAnalyzerRoutedCommand.EnableAllBreakpoints, (s, e) => breakpointService.Value.EnableAllBreakpoints(), (s, e) => e.CanExecute = breakpointService.Value.CanEnableAllBreakpoints);

            //cmds.Add(StringsAnalyzerRoutedCommand.ShowBreakpoints, new RelayCommand(a => toolWindowService.Show(BreakpointsToolWindowContent.THE_GUID)));
            //cmds.Add(StringsAnalyzerRoutedCommand.ShowBreakpoints, ModifierKeys.Control | ModifierKeys.Alt, Key.B);
            //cmds.Add(StringsAnalyzerRoutedCommand.ShowBreakpoints, ModifierKeys.Alt, Key.F9);
        }

        sealed class StringsAnalyzerCtxMenuContext
        {
            public ToolWindowVm.StringAnalyzerData VM { get; }
            //public BreakpointVM[] SelectedItems { get; }

            public StringsAnalyzerCtxMenuContext(ToolWindowVm.StringAnalyzerData vm)
            {
                VM = vm;
                //SelectedItems = selItems;
            }
        }

        
        [Export, ExportMenuItem(Header = "Go To String", Icon = DsImagesAttribute.GoToSourceCode, InputGestureText = "res:ShortCutKeyEnter", Group = MenuConstants.GROUP_CTX_DBG_BPS_CODE, Order = 0)]
        sealed class GoToStringCtxMenuCommand : BreakpointCtxMenuCommand
        {
            //readonly Lazy<IModuleLoader> moduleLoader;
            readonly IDocumentTabService documentTabService;
            readonly IModuleIdProvider moduleIdProvider;

            [ImportingConstructor]
            GoToStringCtxMenuCommand(Lazy<IStringsAnalyzerContent> stringAnalyzerContent, IDocumentTabService documentTabService, IModuleIdProvider moduleIdProvider)
                : base(stringAnalyzerContent)
            {
                //this.moduleLoader = moduleLoader;
                this.documentTabService = documentTabService;
                this.moduleIdProvider = moduleIdProvider;
            }

            public override void Execute(StringsAnalyzerCtxMenuContext context)
            {
                ToolWindowVm.StringAnalyzerData stringAnalyzerData = new ToolWindowVm.StringAnalyzerData();
          
                GoTo(moduleIdProvider, documentTabService, stringAnalyzerData, false);
            }

            // This is supposed to take us to the whatever string we are inspecting into the respective assembly.
            internal static void GoTo(IModuleIdProvider moduleIdProvider, IDocumentTabService documentTabService, ToolWindowVm.StringAnalyzerData items, bool newTab)
            {
                if (items == null)
                    return;
                var ilbp = items as ToolWindowVm.StringAnalyzerData;
                if (ilbp == null)
                    return;
                ContextUtils.GoToIL(moduleIdProvider, documentTabService, ilbp.ModuleID, uint.Parse(ilbp.MdToken), uint.Parse(ilbp.IlOffset), newTab);
            }

            //public override bool IsEnabled(StringsAnalyzerCtxMenuContext context) => context.SelectedItems.Length == 1 && context.SelectedItems[0].Breakpoint is ILCodeBreakpoint;
        }

        abstract class BreakpointCtxMenuCommand : MenuItemBase<StringsAnalyzerCtxMenuContext>
        {
            protected sealed override object CachedContextKey => ContextKey;
            static readonly object ContextKey = new object();

            protected readonly Lazy<IStringsAnalyzerContent> breakpointsContent;

            protected BreakpointCtxMenuCommand(Lazy<IStringsAnalyzerContent> breakpointsContent)
            {
                this.breakpointsContent = breakpointsContent;
            }

            protected sealed override StringsAnalyzerCtxMenuContext CreateContext(IMenuItemContext context)
            {
                if (!(context.CreatorObject.Object is ListView))
                    return null;
                //if (context.CreatorObject.Object != breakpointsContent.Value.ListView)
                    //return null;
                return Create();
            }

            internal StringsAnalyzerCtxMenuContext Create()
            {
                var listView = ToolWindowControl.stringAnalyzer;
                var vm = breakpointsContent.Value.StringsAnalyzerVM;

                var dict = new Dictionary<object, int>(listView.Items.Count);
                for (int i = 0; i < listView.Items.Count; i++)
                    dict[listView.Items[i]] = i;
                var elems = listView.SelectedItems.OfType<ToolWindowVm.StringAnalyzerData>().ToArray();
                Array.Sort(elems, (a, b) => dict[a].CompareTo(dict[b]));

                return new StringsAnalyzerCtxMenuContext(vm);
            }
        }
    }
}
