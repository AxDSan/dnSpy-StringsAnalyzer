/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Threading.Tasks;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.AsmEditor.Compiler {
	sealed class EditAssemblyVM : EditCodeVM {
		sealed class EditAssemblyDecompileCodeState : DecompileCodeState {
			public StringBuilderDecompilerOutput MainOutput { get; } = new StringBuilderDecompilerOutput();
		}

		public EditAssemblyVM(EditCodeVMOptions options) : base(options, null) => StartDecompile();

		protected override DecompileCodeState CreateDecompileCodeState() =>
			new EditAssemblyDecompileCodeState();

		protected override Task<DecompileAsyncResult> DecompileAsync(DecompileCodeState decompileCodeState) {
			var state = (EditAssemblyDecompileCodeState)decompileCodeState;
			state.CancellationToken.ThrowIfCancellationRequested();

			var options = new DecompileAssemblyInfo(state.MainOutput, state.DecompilationContext, sourceModule);
			options.KeepAllAttributes = true;
			decompiler.Decompile(DecompilationType.AssemblyInfo, options);

			state.CancellationToken.ThrowIfCancellationRequested();

			var result = new DecompileAsyncResult();
			result.AddDocument(MainCodeName, state.MainOutput.ToString(), null);
			return Task.FromResult(result);
		}

		protected override void Import(ModuleImporter importer, CompilationResult result) =>
			importer.Import(result.RawFile!, result.DebugFile, ModuleImporterOptions.ReplaceModuleAssemblyAttributes | ModuleImporterOptions.ReplaceAssemblyDeclSecurities | ModuleImporterOptions.ReplaceExportedTypes);
	}
}
