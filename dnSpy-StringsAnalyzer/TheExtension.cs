using System.Collections.Generic;
using dnSpy.Contracts.Extension;

// Each extension should export one class implementing IExtension

namespace Plugin.StringAnalyzer {
	[ExportExtension]
	sealed class TheExtension : IExtension {
		public IEnumerable<string> MergedResourceDictionaries {
			get {
				yield return "Themes/resourcedict.xaml";
			}
		}

		public ExtensionInfo ExtensionInfo => new ExtensionInfo {
			ShortDescription = "What this plugin does it that it reads the whole assembly and gives you a detailed list view with all the found strings inside of a given assembly.",
		};

		public void OnEvent(ExtensionEvent @event, object obj) {
			// We don't care about any events
		}
	}
}
