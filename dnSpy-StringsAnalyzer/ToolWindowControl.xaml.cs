using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

using static dnSpy.StringsAnalyzer.ToolWindowVm;
using System.IO;
using System;

using dnSpy.Contracts.Documents;
using System.ComponentModel.Composition;

namespace dnSpy.StringsAnalyzer
{
    public partial class ToolWindowControl : UserControl
    {
        private readonly IDsDocumentService documentService;

        [ImportingConstructor]
        public ToolWindowControl(IDsDocumentService documentService)
        {
            this.documentService = documentService;
            InitializeComponent();
        }

        public ToolWindowControl()
        {
        }

        public static List<MethodDef> Methods = new List<MethodDef>();
        public static List<StringAnalyzerData> Items = new List<StringAnalyzerData>();
        public static ListView stringAnalyzer = new ListView();
        public static int I;
        public static ContextMenu ContextMenu1;
        internal IInputElement option1TextBox;

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Items.Clear();
            try
            {
                var documents = documentService.GetDocuments();
                if (documents.Length == 0)
                {
                    MessageBox.Show("No documents loaded.");
                    return;
                }

                // Assuming the first document is the active one, or you might need to implement logic to determine the active document
                var document = documents[0];
                if (document?.ModuleDef == null)
                {
                    MessageBox.Show("No module loaded in the document.");
                    return;
                }

                var md = document.ModuleDef;
                foreach (var types in md.Types)
                {
                    foreach (var mdInfo in types.Methods)
                    {
                        I++;
                        if (!mdInfo.HasBody) continue;
                        var instructions = mdInfo.Body.Instructions;

                        foreach (var instr in instructions)
                        {
                            var token = mdInfo.MDToken;

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
                stringAnalyzer.ItemsSource = Items;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Items.Clear();
        }
    }
}