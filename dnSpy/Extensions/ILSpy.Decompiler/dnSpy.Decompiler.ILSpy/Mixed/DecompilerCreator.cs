using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Decompiler;
using dnSpy.Decompiler.ILSpy.Core.Mixed;
using dnSpy.Decompiler.ILSpy.Core.Settings;

namespace dnSpy.Decompiler.ILSpy.Mixed {
	[Export(typeof(IDecompilerCreator))]
	sealed class MyDecompilerCreator : IDecompilerCreator {
		readonly DecompilerSettingsService decompilerSettingsService;

		[ImportingConstructor]
		MyDecompilerCreator(DecompilerSettingsService decompilerSettingsService) => this.decompilerSettingsService = decompilerSettingsService;

		public IEnumerable<IDecompiler> Create() => new DecompilerProvider(decompilerSettingsService).Create();
	}
}
