using System.Collections.Generic;
using dnSpy.Contracts.Extension;

// Each extension should export one class implementing IExtension

namespace dnSpy.StringsAnalyzer {
	[ExportExtension]
	sealed class StringAnalyzer : IExtension {
		public IEnumerable<string> MergedResourceDictionaries {
			get {
				yield return "Themes/resourcedict.xaml";
			}
		}

		public ExtensionInfo ExtensionInfo => new ExtensionInfo {
			ShortDescription =
                $"This plugin will read the an assembly and give you a detailed list view with all the available strings that were found."
        };

		public void OnEvent(ExtensionEvent @event, object obj) {
			// We don't care about any events
		}
	}
}
