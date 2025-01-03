/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using dnlib.DotNet;

namespace dnSpy.BamlDecompiler.Baml {
	internal partial class KnownThings {
		readonly ModuleDef module;
		readonly IAssemblyResolver resolver;
		readonly IResolver typeResolver;

		readonly Dictionary<int, AssemblyDef> assemblies;
		readonly Dictionary<KnownMembers, KnownMember> members;
		readonly Dictionary<KnownTypes, TypeDef> types;
		readonly Dictionary<int, string> strings;
		readonly Dictionary<int, Tuple<string, string, string>> resources;

		public KnownThings(ModuleDef module) {
			this.module = module;
			resolver = module.Context.AssemblyResolver;
			typeResolver = module.Context.Resolver;

			assemblies = new Dictionary<int, AssemblyDef>();
			types = new Dictionary<KnownTypes, TypeDef>();
			members = new Dictionary<KnownMembers, KnownMember>();
			strings = new Dictionary<int, string>();
			resources = new Dictionary<int, Tuple<string, string, string>>();

			InitAssemblies();
			InitTypes();
			InitMembers();
			InitStrings();
			InitResources();
		}

		public Func<KnownTypes, TypeDef> Types => id => types[id];
		public Func<KnownMembers, KnownMember> Members => id => members[id];
		public Func<short, string> Strings => id => strings[id];
		public Func<short, Tuple<string, string, string>> Resources => id => resources[id];
		public AssemblyDef FrameworkAssembly => assemblies[0];
		TypeDef InitType(IAssembly assembly, string ns, string name) => typeResolver.ResolveThrow(new TypeRefUser(module, ns, name, assembly.ToAssemblyRef()));
		KnownMember InitMember(KnownTypes parent, string name, TypeDef type) => new KnownMember(parent, types[parent], name, type);
		AssemblyDef ResolveThrow(string asmFullName) {
			var asm = resolver.Resolve(asmFullName, module);
			if (asm is not null)
				return asm;
			var newName = asmFullName switch {
				"WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" => "WindowsBase, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
				"PresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" => "PresentationCore, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
				"PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" => "PresentationFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
				_ => null,
			};
			asm = newName is null ? null : resolver.Resolve(newName, module);
			return asm ?? resolver.ResolveThrow(asmFullName, module)/*Will throw*/;
		}
	}

	internal class KnownMember {
		public KnownMember(KnownTypes parent, TypeDef declType, string name, TypeDef type) {
			Parent = parent;
			Property = declType.FindProperty(name);
			DeclaringType = declType;
			Name = name;
			Type = type;
		}

		public KnownTypes Parent { get; }
		public TypeDef DeclaringType { get; }
		public PropertyDef Property { get; }
		public string Name { get; }
		public TypeDef Type { get; }
	}
}
