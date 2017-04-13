using System.Windows.Input;

namespace dnSpy.StringsAnalyzer
{
    static class StringsAnalyzerRoutedCommand
    {
        public static readonly RoutedCommand DebugCurrentAssembly = new RoutedCommand("DebugCurrentAssembly", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand DebugAssembly = new RoutedCommand("DebugAssembly", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand DebugCoreCLRAssembly = new RoutedCommand("DebugCoreCLRAssembly", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand Attach = new RoutedCommand("Attach", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand StartWithoutDebugging = new RoutedCommand("StartWithoutDebugging", typeof(StringsAnalyzerRoutedCommand));

        public static readonly RoutedCommand Break = new RoutedCommand("Break", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand Restart = new RoutedCommand("Restart", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand Stop = new RoutedCommand("Stop", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand Detach = new RoutedCommand("Detach", typeof(StringsAnalyzerRoutedCommand));

        public static readonly RoutedCommand Continue = new RoutedCommand("Continue", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand StepInto = new RoutedCommand("StepInto", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand StepOver = new RoutedCommand("StepOver", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand StepOut = new RoutedCommand("StepOut", typeof(StringsAnalyzerRoutedCommand));

        public static readonly RoutedCommand DeleteAllBreakpoints = new RoutedCommand("DeleteAllBreakpoints", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand ToggleBreakpoint = new RoutedCommand("ToggleBreakpoint", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand DisableBreakpoint = new RoutedCommand("DisableBreakpoint", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand DisableAllBreakpoints = new RoutedCommand("DisableAllBreakpoints", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand EnableAllBreakpoints = new RoutedCommand("EnableAllBreakpoints", typeof(StringsAnalyzerRoutedCommand));

        public static readonly RoutedCommand ShowNextStatement = new RoutedCommand("ShowNextStatement", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand SetNextStatement = new RoutedCommand("SetNextStatement", typeof(StringsAnalyzerRoutedCommand));

        public static readonly RoutedCommand ShowCallStack = new RoutedCommand("ShowCallStack", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand ShowBreakpoints = new RoutedCommand("ShowBreakpoints", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand ShowThreads = new RoutedCommand("ShowThreads", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand ShowModules = new RoutedCommand("ShowModules", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand ShowLocals = new RoutedCommand("ShowLocals", typeof(StringsAnalyzerRoutedCommand));
        public static readonly RoutedCommand ShowExceptions = new RoutedCommand("ShowExceptions", typeof(StringsAnalyzerRoutedCommand));

        static StringsAnalyzerRoutedCommand()
        {
            //ShowMemoryCommands = new RoutedCommand[Memory.MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS];
            //for (int i = 0; i < ShowMemoryCommands.Length; i++)
            //    ShowMemoryCommands[i] = new RoutedCommand(string.Format("ShowMemory{0}", i + 1), typeof(StringsAnalyzerRoutedCommand));
        }
        public static readonly RoutedCommand[] ShowMemoryCommands;
    }
}
