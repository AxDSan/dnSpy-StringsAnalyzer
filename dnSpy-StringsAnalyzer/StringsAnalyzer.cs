using System.Collections.Generic;
using dnSpy.Contracts.Extension;

// Each extension should export one class implementing IExtension

namespace StringsAnalyzer.Extension {
	[ExportExtension]
	sealed class StringsAnalyzer : IExtension {
		public IEnumerable<string> MergedResourceDictionaries {
			get {
				yield return "Themes/resourcedict.xaml";
			}
		}

		public ExtensionInfo ExtensionInfo => new ExtensionInfo {
			ShortDescription = "Strings Analyzer plugin",
		};

		public void OnEvent(ExtensionEvent @event, object? obj) {
			// We don't care about any events
		}
	}
}
