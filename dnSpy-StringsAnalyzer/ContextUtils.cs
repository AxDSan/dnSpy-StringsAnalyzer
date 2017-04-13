using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace dnSpy.StringsAnalyzer
{
    static class ContextUtils
    {
        public static void GoToIL(IModuleIdProvider moduleIdProvider, IDocumentTabService documentTabService, ModuleId moduleId, uint token, uint ilOffset, bool newTab)
        {
            //GoToIL(moduleIdProvider, documentTabService, file, token, ilOffset, newTab);
        }

        public static bool GoToIL(IModuleIdProvider moduleIdProvider, IDocumentTabService documentTabService, IDsDocument document, uint token, uint ilOffset, bool newTab)
        {
            if (document == null)
                return false;

            var method = document.ModuleDef.ResolveToken(token) as MethodDef;
            if (method == null)
                return false;

            var modId = moduleIdProvider.Create(method.Module);
            var key = new ModuleTokenId(modId, method.MDToken);

            bool found = documentTabService.DocumentTreeView.FindNode(method.Module) != null;
            if (found)
            {
                documentTabService.FollowReference(method, newTab, true, e =>
                {
                    Debug.Assert(e.Tab.UIContext is IDocumentViewer);
                    if (e.Success && !e.HasMovedCaret)
                    {
                        MoveCaretTo(e.Tab.UIContext as IDocumentViewer, key, ilOffset);
                        e.HasMovedCaret = true;
                    }
                });
                return true;
            }

            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                documentTabService.FollowReference(method, newTab, true, e =>
                {
                    Debug.Assert(e.Tab.UIContext is IDocumentViewer);
                    if (e.Success && !e.HasMovedCaret)
                    {
                        MoveCaretTo(e.Tab.UIContext as IDocumentViewer, key, ilOffset);
                        e.HasMovedCaret = true;
                    }
                });
            }));
            return true;
        }

        public static bool MoveCaretTo(IDocumentViewer documentViewer, ModuleTokenId key, uint ilOffset)
        {
            if (documentViewer == null)
                return false;

            IMethodDebugService methodDebugService;
            if (!VerifyAndGetCurrentDebuggedMethod(documentViewer, key, out methodDebugService))
                return false;

            var sourceStatement = methodDebugService.TryGetMethodDebugInfo(key).GetSourceStatementByCodeOffset(ilOffset);
            if (sourceStatement == null)
                return false;

            documentViewer.MoveCaretToPosition(sourceStatement.Value.TextSpan.Start);
            return true;
        }

        public static bool VerifyAndGetCurrentDebuggedMethod(IDocumentViewer documentViewer, ModuleTokenId serToken, out IMethodDebugService methodDebugService)
        {
            methodDebugService = documentViewer.GetMethodDebugService();
            return methodDebugService.TryGetMethodDebugInfo(serToken) != null;
        }
    }
}
