using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

using static dnSpy.StringsAnalyzer.ToolWindowVm;
using System.IO;

namespace dnSpy.StringsAnalyzer
{
    public partial class ToolWindowControl : UserControl
    {
        public ToolWindowControl()
        {
            InitializeComponent();
        }
        public static List<MethodDef> Method = new List<MethodDef>();
        public static List<StringAnalyzerData> Items = new List<StringAnalyzerData>();
        public static string Assembly = @"DemoAssembly.exe"; // Demo purposes only!
        public static ListView stringAnalyzer = new ListView();
        // as I'm going to later use the currently inspected assembly from the navigation and not 
        // loading it individually.
        public static int I;
        public static ContextMenu ContextMenu1;
        internal IInputElement option1TextBox;

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Items.Clear();
            try
            {

            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
            var md = ModuleDefMD.Load(Assembly);
            foreach (var types in md.Types)
            {
                foreach (var mdInfo in types.Methods)
                {
                    I++;
                    if (!mdInfo.HasBody) continue;
                    //var dict = new Dictionary<keytype, valuetype>();
                    var instructions = mdInfo.Body.Instructions;

                    foreach (var instr in instructions)
                    {
                        var token = mdInfo.MDToken;

                        /*
                         * Many thanks to Mr.Exodia for helping me out on this part below \(^o^)/~
                         */

                        if (!instr.OpCode.Equals(OpCodes.Ldstr)) continue;
                        var formattedOffset = $"IL_{instr.GetOffset():X4}";

                        Items.Add(new StringAnalyzerData()
                        {
                            StringValue = instr.GetOperand().ToString(),
                            IlOffset = formattedOffset,
                            MdToken = $"0x{token.ToString().Remove(0, 1):x}",
                            MdName = mdInfo.Name,
                            FullmdName = mdInfo.FullName,
                        });
                    }
                }
            }
            stringAnalyzer = lvStringsAnalyzer;
            stringAnalyzer.ItemsSource = Items;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Items.Clear();
        }
    }
}
