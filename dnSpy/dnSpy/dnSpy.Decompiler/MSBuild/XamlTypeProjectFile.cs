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
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Decompiler.MSBuild {
	sealed class XamlTypeProjectFile : TypeProjectFile {
		public XamlTypeProjectFile(TypeDef type, string filename, DecompilationContext decompilationContext, IDecompiler decompiler, Func<TextWriter, IDecompilerOutput> createDecompilerOutput)
			: base(type, filename, decompilationContext, decompiler, createDecompilerOutput) {
		}

		protected override void Decompile(DecompileContext ctx, IDecompilerOutput output) {
			var opts = new DecompilePartialType(output, decompilationContext, Type);
			foreach (var d in GetDefsToRemove())
				opts.Definitions.Add(d);
			opts.InterfacesToRemove.Add(new TypeRefUser(Type.Module, "System.Windows.Markup", "IComponentConnector", new AssemblyNameInfo("WindowsBase, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35").ToAssemblyRef()));
			opts.InterfacesToRemove.Add(new TypeRefUser(Type.Module, "System.Windows.Markup", "IComponentConnector", new AssemblyNameInfo("System.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089").ToAssemblyRef()));
			decompiler.Decompile(DecompilationType.PartialType, opts);
		}

		IEnumerable<IMemberDef> GetDefsToRemove() {
			var ep = Type.Module.EntryPoint;
			if (ep is not null && ep.DeclaringType == Type)
				yield return ep;

			var d = FindInitializeComponent();
			if (d is not null) {
				yield return d;
				foreach (var f in DotNetUtils.GetFields(d)) {
					if (f.FieldType.RemovePinnedAndModifiers().GetElementType() == ElementType.Boolean)
						yield return f;
				}
			}

			var connMeth = FindConnectMethod();
			if (connMeth is not null) {
				yield return connMeth;
				foreach (var f in DotNetUtils.GetFields(connMeth))
					yield return f;
			}

			var delMeth = FindCreateDelegateMethod();
			if (delMeth is not null)
				yield return delMeth;
		}

		MethodDef? FindInitializeComponent() {
			foreach (var md in Type.FindMethods("InitializeComponent")) {
				if (md.IsStatic || md.Parameters.Count != 1)
					continue;
				if (md.ReturnType.RemovePinnedAndModifiers().GetElementType() != ElementType.Void)
					continue;

				return md;
			}
			return null;
		}

		MethodDef? FindConnectMethod() {
			foreach (var md in Type.Methods) {
				if (IsConnect(md))
					return md;
			}
			return null;
		}

		static bool IsConnect(MethodDef md) {
			if (md is null || md.IsStatic || md.Parameters.Count != 3)
				return false;
			if (md.ReturnType.RemovePinnedAndModifiers().GetElementType() != ElementType.Void)
				return false;

			var sig = md.MethodSig;
			if (sig is null || sig.Params.Count != 2)
				return false;
			if (sig.Params[0].RemovePinnedAndModifiers().GetElementType() != ElementType.I4)
				return false;
			if (sig.Params[1].RemovePinnedAndModifiers().GetElementType() != ElementType.Object)
				return false;

			foreach (var o in md.Overrides) {
				if (o.MethodDeclaration is null || o.MethodDeclaration.DeclaringType is null)
					continue;
				if (o.MethodDeclaration.DeclaringType.FullName != "System.Windows.Markup.IComponentConnector")
					continue;
				if (o.MethodDeclaration.Name != "Connect")
					continue;
				return true;
			}

			return md.Name == "Connect";
		}

		// Finds 'internal Delegate _CreateDelegate(Type delegateType, string handler)' which is
		// called by XamlGeneratedNamespace.GeneratedInternalTypeHelper.CreateDelegate()
		MethodDef? FindCreateDelegateMethod() {
			foreach (var m in Type.Methods) {
				if (m.Name != "_CreateDelegate")
					continue;
				if (m.IsStatic || !m.IsAssembly)
					continue;
				var sig = m.MethodSig;
				if (sig.GetParamCount() != 2)
					continue;
				if (sig.RetType.RemovePinnedAndModifiers() is null || sig.RetType.RemovePinnedAndModifiers().ToString() != "System.Delegate")
					continue;
				if (sig.Params[0].RemovePinnedAndModifiers() is null || sig.Params[0].RemovePinnedAndModifiers().ToString() != "System.Type")
					continue;
				if (sig.Params[1].RemovePinnedAndModifiers() is null || sig.Params[1].RemovePinnedAndModifiers().GetElementType() != ElementType.String)
					continue;

				return m;
			}
			return null;
		}
	}
}
