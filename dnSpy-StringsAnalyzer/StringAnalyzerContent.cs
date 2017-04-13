using dnSpy.Contracts.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dnSpy.StringsAnalyzer
{
    interface IStringsAnalyzerContent : IUIObjectProvider
    {
        void OnShow();
        void OnClose();
        void OnVisible();
        void OnHidden();
        void Focus();
        ToolWindowVm.StringAnalyzerData StringsAnalyzerVM { get; }
    }
}
