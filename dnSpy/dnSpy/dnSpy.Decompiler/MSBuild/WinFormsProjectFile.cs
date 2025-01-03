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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Decompiler.Properties;

namespace dnSpy.Decompiler.MSBuild {
	sealed class WinFormsProjectFile : TypeProjectFile {
		public override string Description => dnSpy_Decompiler_Resources.MSBuild_CreateWinFormsFile;
		public IDecompiler Decompiler => decompiler;
		public DecompilationContext DecompilationContext => decompilationContext;

		public WinFormsProjectFile(TypeDef type, string filename, DecompilationContext decompilationContext, IDecompiler decompiler, Func<TextWriter, IDecompilerOutput> createDecompilerOutput)
			: base(type, filename, decompilationContext, decompiler, createDecompilerOutput) => SubType = "Form";

		protected override void Decompile(DecompileContext ctx, IDecompilerOutput output) {
			if (!decompiler.CanDecompile(DecompilationType.PartialType))
				base.Decompile(ctx, output);
			else {
				var opts = new DecompilePartialType(output, decompilationContext, Type);
				foreach (var d in GetDefsToRemove())
					opts.Definitions.Add(d);
				decompiler.Decompile(DecompilationType.PartialType, opts);
			}
		}

		public IMemberDef[] GetDefsToRemove() {
			if (defsToRemove is not null)
				return defsToRemove;
			lock (defsToRemoveLock) {
				if (defsToRemove is null)
					defsToRemove = CalculateDefsToRemove().Distinct().ToArray();
			}
			return defsToRemove;
		}
		readonly object defsToRemoveLock = new object();
		IMemberDef[]? defsToRemove;

		IEnumerable<IMemberDef> CalculateDefsToRemove() {
			var m = GetInitializeComponent();
			if (m is not null) {
				yield return m;
				foreach (var f in DotNetUtils.GetFields(m))
					yield return f;
			}

			m = GetDispose();
			if (m is not null) {
				yield return m;
				foreach (var f in DotNetUtils.GetFields(m))
					yield return f;
			}
		}

		MethodDef? GetInitializeComponent() {
			foreach (var m in Type.Methods) {
				if (m.Access != MethodAttributes.Private)
					continue;
				if (m.IsStatic || m.Parameters.Count != 1)
					continue;
				if (m.ReturnType.RemovePinnedAndModifiers().GetElementType() != ElementType.Void)
					continue;
				if (m.Name != "InitializeComponent")
					continue;
				if (m.Body is null)
					continue;
				return m;
			}
			return null;
		}

		MethodDef? GetDispose() {
			foreach (var m in Type.Methods) {
				if (m.Access != MethodAttributes.Family)
					continue;
				if (m.IsStatic || m.Parameters.Count != 2 || m.Parameters[1].Type.RemovePinnedAndModifiers().GetElementType() != ElementType.Boolean)
					continue;
				if (m.ReturnType.RemovePinnedAndModifiers().GetElementType() != ElementType.Void)
					continue;
				if (m.Name != "Dispose")
					continue;
				if (m.Body is null)
					continue;
				return m;
			}
			return null;
		}
	}

	sealed class WinFormsDesignerProjectFile : ProjectFile {
		public override string Description => dnSpy_Decompiler_Resources.MSBuild_CreateWinFormsDesignerFile;
		public override BuildAction BuildAction => BuildAction.Compile;
		public override string Filename => filename;
		readonly string filename;

		readonly WinFormsProjectFile winFormsFile;
		readonly Func<TextWriter, IDecompilerOutput> createDecompilerOutput;

		public WinFormsDesignerProjectFile(WinFormsProjectFile winFormsFile, string filename, Func<TextWriter, IDecompilerOutput> createDecompilerOutput) {
			this.winFormsFile = winFormsFile;
			this.filename = filename;
			this.createDecompilerOutput = createDecompilerOutput;
		}

		public override void Create(DecompileContext ctx) {
			using (var writer = new StreamWriter(Filename, false, Encoding.UTF8)) {
				if (winFormsFile.Decompiler.CanDecompile(DecompilationType.PartialType)) {
					var output = createDecompilerOutput(writer);
					var opts = new DecompilePartialType(output, winFormsFile.DecompilationContext, winFormsFile.Type);
					foreach (var d in winFormsFile.GetDefsToRemove())
						opts.Definitions.Add(d);
					opts.ShowDefinitions = true;
					opts.UseUsingDeclarations = false;
					winFormsFile.Decompiler.Decompile(DecompilationType.PartialType, opts);
				}
			}
		}
	}
}
