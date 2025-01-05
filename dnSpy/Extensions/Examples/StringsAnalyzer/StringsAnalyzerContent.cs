using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnSpy.Contracts.Controls;
using StringsAnalyzer.Extension;

namespace StringsAnalyzer {
	interface IStringsAnalyzerContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		ToolWindowVM.StringAnalyzerData StringsAnalyzerVM { get; }
	}
}
