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
	sealed class SettingsTypeProjectFile : TypeProjectFile {
		public override string Description => dnSpy_Decompiler_Resources.MSBuild_CreateSettingsTypeFile;
		public IDecompiler Decompiler => decompiler;
		public DecompilationContext DecompilationContext => decompilationContext;
		public override BuildAction BuildAction => isEmpty ? BuildAction.DontIncludeInProjectFile : base.BuildAction;
		bool isEmpty;

		public SettingsTypeProjectFile(TypeDef type, string filename, DecompilationContext decompilationContext, IDecompiler decompiler, Func<TextWriter, IDecompilerOutput> createDecompilerOutput)
			: base(type, filename, decompilationContext, decompiler, createDecompilerOutput) {
		}

		public override void Create(DecompileContext ctx) {
			InitializeIsEmpty();
			if (!isEmpty)
				base.Create(ctx);
		}

		protected override void Decompile(DecompileContext ctx, IDecompilerOutput output) {
			var opts = new DecompilePartialType(output, decompilationContext, Type);
			foreach (var d in GetDefsToRemove())
				opts.Definitions.Add(d);
			decompiler.Decompile(DecompilationType.PartialType, opts);
		}

		void InitializeIsEmpty() {
			var allDefs = new HashSet<object>();
			foreach (var m in Type.Methods) allDefs.Add(m);
			foreach (var f in Type.Fields) allDefs.Add(f);
			foreach (var p in Type.Properties) allDefs.Add(p);
			foreach (var e in Type.Events) allDefs.Add(e);
			foreach (var t in Type.NestedTypes) allDefs.Add(t);
			foreach (var d in GetDefsToRemove()) {
				allDefs.Remove(d);
				if (d is PropertyDef) {
					foreach (var def in DotNetUtils.GetMethodsAndSelf((PropertyDef)d))
						allDefs.Remove(def);
				}
			}
			allDefs.Remove(Type.FindStaticConstructor());
			allDefs.Remove(Type.FindDefaultConstructor());
			isEmpty = allDefs.Count == 0;
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
			var defaultProp = FindDefaultProperty();
			if (defaultProp is not null) {
				foreach (var d in DotNetUtils.GetMethodsAndSelf(defaultProp))
					yield return d;
				foreach (var d in DotNetUtils.GetDefs(defaultProp))
					yield return d;
			}
			foreach (var p in Type.Properties) {
				if (p.CustomAttributes.IsDefined("System.Configuration.DefaultSettingValueAttribute")) {
					foreach (var d in DotNetUtils.GetMethodsAndSelf(p))
						yield return d;
				}
			}
		}

		PropertyDef? FindDefaultProperty() {
			foreach (var p in Type.Properties) {
				if (p.Name != "Default")
					continue;
				var g = p.GetMethod;
				if (g is null || !g.IsStatic || p.SetMethod is not null || p.OtherMethods.Count != 0)
					continue;
				if (g.MethodSig.GetParamCount() != 0)
					continue;
				if (g.ReturnType.RemovePinnedAndModifiers().TryGetTypeDef() != Type)
					continue;
				if (g.Body is null)
					continue;

				return p;
			}

			return null;
		}
	}

	sealed class SettingsDesignerTypeProjectFile : ProjectFile {
		public override string Description => dnSpy_Decompiler_Resources.MSBuild_CreateSettingsDesignerTypeFile;
		public override BuildAction BuildAction => BuildAction.Compile;
		public override string Filename => filename;
		readonly string filename;

		readonly SettingsTypeProjectFile typeFile;
		readonly Func<TextWriter, IDecompilerOutput> createDecompilerOutput;

		public SettingsDesignerTypeProjectFile(SettingsTypeProjectFile typeFile, string filename, Func<TextWriter, IDecompilerOutput> createDecompilerOutput) {
			this.typeFile = typeFile;
			this.filename = filename;
			this.createDecompilerOutput = createDecompilerOutput;
		}

		public override void Create(DecompileContext ctx) {
			using (var writer = new StreamWriter(Filename, false, Encoding.UTF8)) {
				if (typeFile.Decompiler.CanDecompile(DecompilationType.PartialType)) {
					var output = createDecompilerOutput(writer);
					var opts = new DecompilePartialType(output, typeFile.DecompilationContext, typeFile.Type);
					foreach (var d in typeFile.GetDefsToRemove())
						opts.Definitions.Add(d);
					opts.ShowDefinitions = true;
					typeFile.Decompiler.Decompile(DecompilationType.PartialType, opts);
				}
			}
		}
	}
}
