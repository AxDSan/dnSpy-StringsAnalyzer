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
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Resources;
using dnSpy.Decompiler.Properties;

namespace dnSpy.Decompiler.MSBuild {
	sealed class ResXProjectFile : ProjectFile {
		public override string Description => dnSpy_Decompiler_Resources.MSBuild_CreateResXFile;
		public override BuildAction BuildAction => BuildAction.EmbeddedResource;
		public override string Filename => filename;
		readonly string filename;

		public string TypeFullName { get; }
		public bool IsSatelliteFile { get; set; }

		readonly ModuleDef module;
		readonly ResourceElementSet resourceElementSet;
		readonly Dictionary<IAssembly, IAssembly> newToOldAsm;

		public ResXProjectFile(ModuleDef module, string filename, string typeFullName, ResourceElementSet resourceElementSet) {
			this.module = module;
			this.filename = filename;
			TypeFullName = typeFullName;
			this.resourceElementSet = resourceElementSet;

			newToOldAsm = new Dictionary<IAssembly, IAssembly>(new AssemblyNameComparer(AssemblyNameComparerFlags.All & ~AssemblyNameComparerFlags.Version));
			foreach (var asmRef in module.GetAssemblyRefs())
				newToOldAsm[asmRef] = asmRef;
		}

		public override void Create(DecompileContext ctx) {
			using (var writer = new ResXResourceFileWriter(Filename, TypeNameConverter, HasTypeConverterForByteArray)) {
				foreach (var resourceElement in resourceElementSet.ResourceElements) {
					ctx.CancellationToken.ThrowIfCancellationRequested();
					writer.AddResourceData(resourceElement);
				}
			}
		}

		string TypeNameConverter(Type type) {
			var newAsm = new AssemblyNameInfo(type.Assembly.GetName());
			if (!newToOldAsm.TryGetValue(newAsm, out var oldAsm))
				return type.AssemblyQualifiedName ?? throw new ArgumentException();
			if (type.IsGenericType)
				return type.AssemblyQualifiedName ?? throw new ArgumentException();
			if (AssemblyNameComparer.CompareAll.Equals(oldAsm, newAsm))
				return type.AssemblyQualifiedName ?? throw new ArgumentException();
			return $"{type.FullName}, {oldAsm.FullName}";
		}

		bool HasTypeConverterForByteArray(string typeFullName) {
			var typeDef = TypeNameParser.ParseReflection(module, typeFullName, null)?.ResolveTypeDef();

			while (typeDef is not null) {
				var attribute = typeDef.CustomAttributes.Find("System.ComponentModel.TypeConverterAttribute");
				if (attribute is not null && attribute.ConstructorArguments.Count == 1 && attribute.ConstructorArguments[0].Value is ClassOrValueTypeSig typeSig) {
					var converter = typeSig.TypeDefOrRef.ResolveTypeDef();
					var (canConvertToMethod, canConvertFromMethod) = FindCanConvertMethods(converter);

					if (canConvertToMethod is not null && canConvertFromMethod is not null) {
						bool canConvertToHasByteArray = ContainsByteArrayReference(canConvertToMethod);
						bool canConvertFromHasByteArray = ContainsByteArrayReference(canConvertFromMethod);
						if (canConvertFromHasByteArray && canConvertToHasByteArray)
							return true;
					}

					return false;
				}

				typeDef = typeDef.BaseType.ResolveTypeDef();
			}

			return false;
		}

		static (MethodDef? canConvertToMethod, MethodDef? canConvertFromMethod) FindCanConvertMethods(TypeDef type) {
			MethodDef? canConvertToMethod = null;
			MethodDef? canConvertFromMethod = null;
			foreach (var method in type.Methods) {
				if (!method.MethodSig.HasThis)
					continue;
				if (method.MethodSig.Params.Count != 2)
					continue;
				if (method.ReturnType.GetElementType() != ElementType.Boolean)
					continue;

				if (method.Name == "CanConvertTo")
					canConvertToMethod ??= method;
				else if (method.Name == "CanConvertFrom")
					canConvertFromMethod ??= method;
				else {
					foreach (var methodOverride in method.Overrides) {
						var overridenMethod = methodOverride.MethodDeclaration.ResolveMethodDef();
						if (overridenMethod.Name == "CanConvertTo")
							canConvertToMethod ??= method;
						else if (overridenMethod.Name == "CanConvertFrom")
							canConvertFromMethod ??= method;
					}
				}
			}

			return (canConvertToMethod, canConvertFromMethod);
		}

		static bool ContainsByteArrayReference(MethodDef method) {
			if (!method.HasBody)
				return false;
			for (var i = 0; i < method.Body.Instructions.Count; i++) {
				var instr = method.Body.Instructions[i];
				if (instr.OpCode.Code == Code.Ldtoken && instr.Operand is TypeSpec typeSpec &&
				    typeSpec.TypeSig is SZArraySig arraySig &&
				    arraySig.Next.GetElementType() == ElementType.U1) {
					return true;
				}
			}
			return false;
		}
	}
}
