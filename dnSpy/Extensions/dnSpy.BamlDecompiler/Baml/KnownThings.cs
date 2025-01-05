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
using System.Threading;
using dnlib.DotNet;

namespace dnSpy.BamlDecompiler.Baml {
	sealed partial class KnownThings {
		readonly ModuleDef module;

		readonly Dictionary<int, KnownAssembly> assemblies;
		readonly Dictionary<KnownMembers, KnownMember> members;
		readonly Dictionary<KnownTypes, KnownType> types;
		readonly Dictionary<int, string> strings;
		readonly Dictionary<int, (string TypeName, string KeyName, string PropertyName)> resources;

		public KnownThings(ModuleDef module) {
			this.module = module;

			assemblies = new Dictionary<int, KnownAssembly>();
			types = new Dictionary<KnownTypes, KnownType>();
			members = new Dictionary<KnownMembers, KnownMember>();
			strings = new Dictionary<int, string>();
			resources = new Dictionary<int, (string, string, string)>();

			InitAssemblies();
			InitTypes();
			InitMembers();
			InitStrings();
			InitResources();
		}

		public Func<KnownTypes, KnownType> Types => id => types[id];
		public Func<KnownMembers, KnownMember> Members => id => members[id];
		public Func<short, string> Strings => id => strings[id];
		public Func<short, (string TypeName, string KeyName, string PropertyName)> Resources => id => resources[id];
		public KnownAssembly PresentationFrameworkAssembly => assemblies[4];
		KnownType InitType(KnownAssembly assembly, string ns, string name) => new KnownType(assembly, ns, name, module);
		KnownMember InitMember(KnownTypes parent, string name, KnownType type) => new KnownMember(parent, types[parent], name, type);
		KnownAssembly InitAssembly(string asmFullName) => new KnownAssembly(asmFullName, module);
	}

	sealed class KnownAssembly {
		AssemblyDef resolved;
		readonly ModuleDef sourceModule;

		public string FullName { get; }

		public AssemblyDef AssemblyDef {
			get {
				if (resolved is null)
					Interlocked.CompareExchange(ref resolved, ResolveAssembly(), null);
				return resolved;
			}
		}

		public KnownAssembly(string fullName, ModuleDef sourceModule) {
			FullName = fullName;
			this.sourceModule = sourceModule;
		}

		AssemblyDef ResolveAssembly() {
			var asm = sourceModule.Context.AssemblyResolver.Resolve(FullName, sourceModule);
			if (asm is not null)
				return asm;
			var newName = FullName switch {
				"WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" => "WindowsBase, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
				"PresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" => "PresentationCore, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
				"PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" => "PresentationFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
				_ => null
			};
			asm = newName is null ? null : sourceModule.Context.AssemblyResolver.Resolve(newName, sourceModule);
			return asm ?? sourceModule.Context.AssemblyResolver.ResolveThrow(FullName, sourceModule)/*Will throw*/;
		}

		public static implicit operator AssemblyDef(KnownAssembly knownAssembly) => knownAssembly.AssemblyDef;
	}

	sealed class KnownType {
		TypeDef resolved;
		readonly ModuleDef sourceModule;

		public KnownAssembly Assembly { get; }

		public string Namespace { get; }

		public string Name { get; }

		public TypeDef TypeDef {
			get {
				if (resolved is null)
					Interlocked.CompareExchange(ref resolved, ResolveType(), null);
				return resolved;
			}
		}

		public KnownType(KnownAssembly assembly, string ns, string name, ModuleDef sourceModule) {
			Assembly = assembly;
			Namespace = ns;
			Name = name;
			this.sourceModule = sourceModule;
		}

		TypeDef ResolveType() => sourceModule.Context.Resolver.ResolveThrow(new TypeRefUser(sourceModule, Namespace, Name, Assembly.AssemblyDef.ToAssemblyRef()));

		public static implicit operator TypeDef(KnownType knownType) => knownType.TypeDef;
	}

	sealed class KnownMember {
		PropertyDef resolvedProperty;

		public KnownMember(KnownTypes parent, KnownType declType, string name, KnownType type) {
			Parent = parent;
			DeclaringType = declType;
			Name = name;
			Type = type;
		}

		public KnownTypes Parent { get; }
		public KnownType DeclaringType { get; }

		public PropertyDef Property {
			get {
				if (resolvedProperty is null)
					Interlocked.CompareExchange(ref resolvedProperty, DeclaringType.TypeDef.FindProperty(Name), null);
				return resolvedProperty;
			}
		}

		public string Name { get; }
		public KnownType Type { get; }
	}
}
