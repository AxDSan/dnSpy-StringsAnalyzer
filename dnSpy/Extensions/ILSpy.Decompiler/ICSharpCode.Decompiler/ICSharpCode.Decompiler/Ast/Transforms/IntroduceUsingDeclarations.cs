// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.Decompiler.Ast.Transforms {
	/// <summary>
	/// Introduces using declarations.
	/// </summary>
	public class IntroduceUsingDeclarations : IAstTransformPoolObject
	{
		DecompilerContext context;
		readonly StringBuilder stringBuilder;
		readonly Dictionary<string, List<TypeDef>> typesWithNamespace_currentModule = new Dictionary<string, List<TypeDef>>(StringComparer.Ordinal);
		readonly List<Dictionary<string, List<TypeDef>>> typesWithNamespace_allAsms_list = new List<Dictionary<string, List<TypeDef>>>();
		readonly List<NamespaceRef> namespaceRefList = new List<NamespaceRef>();
		ModuleDef lastCheckedModule;
		readonly Dictionary<AssemblyDef, bool> friendAssemblyCache = new Dictionary<AssemblyDef, bool>();

		public IntroduceUsingDeclarations(DecompilerContext context)
		{
			this.stringBuilder = new StringBuilder();
			Reset(context);
		}

		public void Reset(DecompilerContext context)
		{
			this.context = context;
			namespaceRefList.Clear();
			ambiguousTypeNames.Clear();
			availableTypeNames.Clear();
			importedNamespaces.Clear();
			importedOrDeclaredNamespaces.Clear();
			importedOrDeclaredNamespaces.Add(string.Empty);
			// Don't clear lastCheckedModule, typesWithNamespace_currentModule, typesWithNamespace_allAsms_list since they're cached between resets
		}

		public void Run(AstNode compilationUnit)
		{
			// First determine all the namespaces that need to be imported:
			compilationUnit.AcceptVisitor(new FindRequiredImports(this), null);

			importedNamespaces.Add(new NamespaceRef(context.CurrentModule.CorLibTypes.AssemblyRef, "System")); // always import System, even when not necessary
			importedOrDeclaredNamespaces.Add("System");
			if (context.CalculateILSpans) {
				Debug.Assert(context.UsingNamespaces.Count == 0);
				foreach (var nsRef in importedNamespaces)
					context.UsingNamespaces.Add(nsRef.Namespace);
			}

			if (context.Settings.UsingDeclarations) {
				// Now add using declarations for those namespaces:
				foreach (var ns in GetNamespacesInReverseOrder()) {
					// we go backwards (OrderByDescending) through the list of namespaces because we insert them backwards
					// (always inserting at the start of the list)
					string[] parts = ns.Namespace.Split(namespaceSep);
					stringBuilder.Clear();
					stringBuilder.Append(parts[0]);
					var nsAsm = ns.Assembly;
					SimpleType simpleType;
					AstType nsType = simpleType = new SimpleType(parts[0]).WithAnnotation(BoxedTextColor.Namespace);
					simpleType.IdentifierToken.WithAnnotation(BoxedTextColor.Namespace).WithAnnotation(new NamespaceReference(nsAsm, parts[0]));
					for (int i = 1; i < parts.Length; i++) {
						stringBuilder.Append('.');
						stringBuilder.Append(parts[i]);
						var nsPart = stringBuilder.ToString();
						nsType = new MemberType { Target = nsType, MemberNameToken = Identifier.Create(parts[i]).WithAnnotation(BoxedTextColor.Namespace).WithAnnotation(new NamespaceReference(nsAsm, nsPart)) }.WithAnnotation(BoxedTextColor.Namespace);
					}
					compilationUnit.InsertChildAfter(null, new UsingDeclaration { Import = nsType }, SyntaxTree.MemberRole);
				}
			}

			if (!context.Settings.FullyQualifyAmbiguousTypeNames && !context.Settings.FullyQualifyAllTypes)
				return;

			if (context.CurrentModule != null) {
				if (lastCheckedModule != context.CurrentModule) {
					typesWithNamespace_currentModule.Clear();
					BuildAmbiguousTypeNamesTable(typesWithNamespace_currentModule, context.CurrentModule.Types, static _ => true);
				}
				FindAmbiguousTypeNames(typesWithNamespace_currentModule, static _ => true);
				if (lastCheckedModule != context.CurrentModule) {
					friendAssemblyCache.Clear();
					var asmDict = new Dictionary<AssemblyDef, List<AssemblyDef>>(AssemblyEqualityComparer.Instance);
					lastCheckedModule = context.CurrentModule;
					foreach (var r in context.CurrentModule.GetAssemblyRefs()) {
						AssemblyDef d = context.CurrentModule.Context.AssemblyResolver.Resolve(r, context.CurrentModule);
						if (d == null)
							continue;
						if (!asmDict.TryGetValue(d, out var list))
							asmDict.Add(d, list = new List<AssemblyDef>());
						list.Add(d);
					}
					typesWithNamespace_allAsms_list.Clear();
					foreach (var list in asmDict.Values) {
						var dict = new Dictionary<string, List<TypeDef>>(StringComparer.Ordinal);
						BuildAmbiguousTypeNamesTable(dict, GetTypes(list), IsFriendAssemblyOf);
						typesWithNamespace_allAsms_list.Add(dict);
					}
				}
				foreach (var dict in typesWithNamespace_allAsms_list) {
					FindAmbiguousTypeNames(dict, IsFriendAssemblyOf);
				}
			}

			// verify that the SimpleTypes refer to the correct type (no ambiguities)
			compilationUnit.AcceptVisitor(new FullyQualifyAmbiguousTypeNamesVisitor(this), null);
		}
		static readonly char[] namespaceSep = new char[] { '.' };

		bool IsFriendAssemblyOf(AssemblyDef assemblyDef) {
			if (context.CurrentModule.Assembly is null)
				return false;
			if (friendAssemblyCache.TryGetValue(assemblyDef, out var cachedValue))
				return cachedValue;

			return friendAssemblyCache[assemblyDef] = context.CurrentModule.Assembly.IsFriendAssemblyOf(assemblyDef);
		}

		readonly struct NamespaceRef : IEquatable<NamespaceRef> {
			public IAssembly Assembly { get; }
			public string Namespace { get; }
			public NamespaceRef(IAssembly asm, string ns) {
				Assembly = asm;
				Namespace = ns;
			}
			public bool Equals(NamespaceRef other) => StringComparer.Ordinal.Equals(other.Namespace, Namespace);
			public override bool Equals(object obj) => obj is NamespaceRef namespaceRef && Equals(namespaceRef);
			public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Namespace);
		}

		List<NamespaceRef> GetNamespacesInReverseOrder()
		{
			namespaceRefList.Clear();
			foreach (var s in importedNamespaces)
				namespaceRefList.Add(s);

			if (context.Settings.SortSystemUsingStatementsFirst)
				namespaceRefList.Sort(ReverseSortSystemUsingStatementsFirstComparer.Instance);
			else
				namespaceRefList.Sort(ReverseSortNamespacesAlphabeticallyComparer.Instance);

			return namespaceRefList;
		}

		sealed class ReverseSortSystemUsingStatementsFirstComparer : IComparer<NamespaceRef> {
			public static readonly ReverseSortSystemUsingStatementsFirstComparer Instance = new ReverseSortSystemUsingStatementsFirstComparer();
			public int Compare(NamespaceRef x, NamespaceRef y) {
				bool sx = x.Namespace == "System" || x.Namespace.StartsWith("System.", StringComparison.Ordinal);
				bool sy = y.Namespace == "System" || y.Namespace.StartsWith("System.", StringComparison.Ordinal);
				if (sx && sy)
					return StringComparer.OrdinalIgnoreCase.Compare(y.Namespace, x.Namespace);
				if (sx)
					return 1;
				if (sy)
					return -1;
				return StringComparer.OrdinalIgnoreCase.Compare(y.Namespace, x.Namespace);
			}
		}

		sealed class ReverseSortNamespacesAlphabeticallyComparer : IComparer<NamespaceRef> {
			public static readonly ReverseSortNamespacesAlphabeticallyComparer Instance = new ReverseSortNamespacesAlphabeticallyComparer();
			public int Compare(NamespaceRef x, NamespaceRef y) => StringComparer.OrdinalIgnoreCase.Compare(y.Namespace, x.Namespace);
		}

		static IEnumerable<TypeDef> GetTypes(List<AssemblyDef> asms)
		{
			if (asms.Count == 0)
				return Array.Empty<TypeDef>();
			if (asms.Count == 1) {
				if (asms[0].Modules.Count == 1)
					return asms[0].ManifestModule.Types;
				return asms[0].Modules.SelectMany(m => m.Types);
			}

			var types = new HashSet<TypeDef>(new TypeEqualityComparer(SigComparerOptions.DontCompareTypeScope));
			foreach (var asm in asms) {
				foreach (var mod in asm.Modules) {
					foreach (var type in mod.Types) {
						if (types.Add(type))
							continue;
						if (!type.IsPublic)
							continue;
						types.Remove(type);
						bool b = types.Add(type);
						Debug.Assert(b);
					}
				}
			}
			return types;
		}

		sealed class AssemblyEqualityComparer : IEqualityComparer<AssemblyDef>
		{
			public static readonly AssemblyEqualityComparer Instance = new AssemblyEqualityComparer();

			public bool Equals(AssemblyDef x, AssemblyDef y)
			{
				if (x == y)
					return true;
				if (x == null || y == null)
					return false;
				if (!x.Name.String.Equals(y.Name, StringComparison.InvariantCultureIgnoreCase))
					return false;
				if (x.PublicKey.IsNullOrEmpty != y.PublicKey.IsNullOrEmpty)
					return false;
				if (x.PublicKey.IsNullOrEmpty)
					return true;
				return x.PublicKey.Equals(y.PublicKey);
			}

			public int GetHashCode(AssemblyDef obj)
			{
				return unchecked(obj.Name.ToUpperInvariant().GetHashCode() +
					obj.PublicKey.GetHashCode());
			}
		}

		readonly HashSet<string> importedOrDeclaredNamespaces = new HashSet<string>(StringComparer.Ordinal);
		readonly HashSet<NamespaceRef> importedNamespaces = new HashSet<NamespaceRef>();

		// Note that we store type names with `n suffix, so we automatically disambiguate based on number of type parameters.
		readonly HashSet<string> availableTypeNames = new HashSet<string>(StringComparer.Ordinal);
		readonly HashSet<string> ambiguousTypeNames = new HashSet<string>(StringComparer.Ordinal);

		sealed class FindRequiredImports : DepthFirstAstVisitor<object, object>
		{
			readonly IntroduceUsingDeclarations transform;
			string currentNamespace;

			public FindRequiredImports(IntroduceUsingDeclarations transform)
			{
				this.transform = transform;
				this.currentNamespace = transform.context.CurrentType != null ? transform.context.CurrentType.Namespace.String : string.Empty;
			}

			bool IsParentOfCurrentNamespace(StringBuilder sb)
			{
				if (sb.Length == 0)
					return true;
				if (currentNamespace.StartsWith(sb)) {
					if (currentNamespace.Length == sb.Length)
						return true;
					if (currentNamespace[sb.Length] == '.')
						return true;
				}
				return false;
			}

			public override object VisitSimpleType(SimpleType simpleType, object data)
			{
				ITypeDefOrRef tr = simpleType.Annotation<ITypeDefOrRef>();
				if (tr != null) {
					var sb = GetNamespace(tr);
					if (!IsParentOfCurrentNamespace(sb)) {
						string ns = sb.ToString();
						transform.importedNamespaces.Add(new NamespaceRef(tr.DefinitionAssembly, ns));
						transform.importedOrDeclaredNamespaces.Add(ns);
					}
				}
				return base.VisitSimpleType(simpleType, data); // also visit type arguments
			}

			StringBuilder GetNamespace(IType type)
			{
				this.transform.stringBuilder.Clear();
				if (type == null)
					return this.transform.stringBuilder;
				return FullNameFactory.NamespaceSB(type, false, this.transform.stringBuilder);
			}

			public override object VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
			{
				string oldNamespace = currentNamespace;
				foreach (string ident in namespaceDeclaration.Identifiers) {
					currentNamespace = NamespaceDeclaration.BuildQualifiedName(currentNamespace, ident);
					transform.importedOrDeclaredNamespaces.Add(currentNamespace);
				}
				base.VisitNamespaceDeclaration(namespaceDeclaration, data);
				currentNamespace = oldNamespace;
				return null;
			}
		}

		void BuildAmbiguousTypeNamesTable(Dictionary<string, List<TypeDef>> dict, IEnumerable<TypeDef> types, Func<AssemblyDef, bool> internalsVisible)
		{
			foreach (TypeDef type in types) {
				if (!type.IsPublic && !internalsVisible(type.Module.Assembly))
					continue;
				string ns = type.Namespace;
				if (!dict.TryGetValue(ns, out var list))
					dict.Add(ns, list = new List<TypeDef>());
				list.Add(type);
			}
		}

		void FindAmbiguousTypeNames(Dictionary<string, List<TypeDef>> dict, Func<AssemblyDef, bool> internalsVisible)
		{
			foreach (var ns in importedOrDeclaredNamespaces) {
				if (!dict.TryGetValue(ns, out var list))
					continue;
				foreach (var type in list) {
					if (!type.IsPublic && !internalsVisible(type.Module.Assembly))
						continue;
					string name = type.Name;
					if (!availableTypeNames.Add(name))
						ambiguousTypeNames.Add(name);
				}
			}
		}

		sealed class FullyQualifyAmbiguousTypeNamesVisitor : DepthFirstAstVisitor<object, object>
		{
			readonly IntroduceUsingDeclarations transform;
			string currentNamespace;
			HashSet<string> currentMemberTypes;
			Dictionary<string, IMemberRef> currentMembers;
			bool isWithinTypeReferenceExpression;

			public FullyQualifyAmbiguousTypeNamesVisitor(IntroduceUsingDeclarations transform)
			{
				this.transform = transform;
				this.currentNamespace = transform.context.CurrentType != null ? transform.context.CurrentType.Namespace.String : string.Empty;
			}

			public override object VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
			{
				string oldNamespace = currentNamespace;
				foreach (string ident in namespaceDeclaration.Identifiers) {
					currentNamespace = NamespaceDeclaration.BuildQualifiedName(currentNamespace, ident);
				}
				base.VisitNamespaceDeclaration(namespaceDeclaration, data);
				currentNamespace = oldNamespace;
				return null;
			}

			public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
			{
				currentMemberTypes = currentMemberTypes != null ? new HashSet<string>(currentMemberTypes) : new HashSet<string>();

				Dictionary<string, IMemberRef> oldMembers = currentMembers;
				currentMembers = new Dictionary<string, IMemberRef>();

				TypeDef typeDef = typeDeclaration.Annotation<TypeDef>();
				bool privateMembersVisible = true;
				ModuleDef internalMembersVisibleInModule = typeDef?.Module;
				while (typeDef != null) {
					foreach (GenericParam gp in typeDef.GenericParameters) {
						currentMemberTypes.Add(gp.Name);
					}
					foreach (TypeDef t in typeDef.NestedTypes) {
						if (privateMembersVisible || IsVisible(t, internalMembersVisibleInModule))
							currentMemberTypes.Add(t.Name.Substring(t.Name.LastIndexOf('+') + 1));
					}

					foreach (MethodDef method in typeDef.Methods) {
						if (privateMembersVisible || IsVisible(method, internalMembersVisibleInModule))
							AddCurrentMember(method);
					}
					foreach (PropertyDef property in typeDef.Properties) {
						if (privateMembersVisible || IsVisible(property.GetMethod, internalMembersVisibleInModule) || IsVisible(property.SetMethod, internalMembersVisibleInModule))
							AddCurrentMember(property);
					}
					foreach (EventDef ev in typeDef.Events) {
						if (privateMembersVisible || IsVisible(ev.AddMethod, internalMembersVisibleInModule) || IsVisible(ev.RemoveMethod, internalMembersVisibleInModule))
							AddCurrentMember(ev);
					}
					foreach (FieldDef f in typeDef.Fields) {
						if (privateMembersVisible || IsVisible(f, internalMembersVisibleInModule))
							AddCurrentMember(f);
					}
					// repeat with base class:
					typeDef = typeDef.BaseType?.ResolveTypeDef();
					privateMembersVisible = false;
				}

				// Now add current members from outer classes:
				if (oldMembers != null) {
					foreach (var pair in oldMembers) {
						// add members from outer classes only if the inner class doesn't define the member
						if (!currentMembers.ContainsKey(pair.Key))
							currentMembers.Add(pair.Key, pair.Value);
					}
				}

				base.VisitTypeDeclaration(typeDeclaration, data);
				currentMembers = oldMembers;
				return null;
			}

			void AddCurrentMember(IMemberRef m)
			{
				if (currentMembers.TryGetValue(m.Name, out var existingMember)) {
					// We keep the existing member assignment if it was from another class (=from a derived class),
					// because members in derived classes have precedence over members in base classes.
					if (existingMember != null && existingMember.DeclaringType == m.DeclaringType) {
						// Use null as value to signalize multiple members with the same name
						currentMembers[m.Name] = null;
					}
				} else {
					currentMembers.Add(m.Name, m);
				}
			}

			bool IsVisible(MethodDef m, ModuleDef internalMembersVisibleInModule)
			{
				if (m == null)
					return false;
				switch (m.Attributes & MethodAttributes.MemberAccessMask) {
					case MethodAttributes.FamANDAssem:
					case MethodAttributes.Assembly:
						if (m.Module is null)
							return false;
						if (m.Module == internalMembersVisibleInModule)
							return true;
						var methodAsm = m.Module.Assembly;
						var currentAsm = internalMembersVisibleInModule.Assembly;
						return methodAsm is not null && currentAsm is not null && currentAsm.IsFriendAssemblyOf(methodAsm);
					case MethodAttributes.Family:
					case MethodAttributes.FamORAssem:
					case MethodAttributes.Public:
						return true;
					default:
						return false;
				}
			}

			bool IsVisible(FieldDef f, ModuleDef internalMembersVisibleInModule)
			{
				if (f == null)
					return false;
				switch (f.Attributes & FieldAttributes.FieldAccessMask) {
					case FieldAttributes.FamANDAssem:
					case FieldAttributes.Assembly:
						if (f.Module is null)
							return false;
						if (f.Module == internalMembersVisibleInModule)
							return true;
						var fieldAsm = f.Module.Assembly;
						var currentAsm = internalMembersVisibleInModule.Assembly;
						return fieldAsm is not null && currentAsm is not null && currentAsm.IsFriendAssemblyOf(fieldAsm);
					case FieldAttributes.Family:
					case FieldAttributes.FamORAssem:
					case FieldAttributes.Public:
						return true;
					default:
						return false;
				}
			}

			bool IsVisible(TypeDef t, ModuleDef internalMembersVisibleInModule)
			{
				if (t == null)
					return false;
				switch (t.Attributes & TypeAttributes.VisibilityMask) {
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedAssembly:
					case TypeAttributes.NestedFamANDAssem:
						if (t.Module is null)
							return false;
						if (t.Module == internalMembersVisibleInModule)
							return true;
						var typeAsm = t.Module.Assembly;
						var currentAsm = internalMembersVisibleInModule.Assembly;
						return typeAsm is not null && currentAsm is not null && currentAsm.IsFriendAssemblyOf(typeAsm);
					case TypeAttributes.NestedFamily:
					case TypeAttributes.NestedFamORAssem:
					case TypeAttributes.NestedPublic:
					case TypeAttributes.Public:
						return true;
					default:
						return false;
				}
			}

			public override object VisitSimpleType(SimpleType simpleType, object data)
			{
				// Handle type arguments first, so that the fixed-up type arguments get moved over to the MemberType,
				// if we're also creating one here.
				base.VisitSimpleType(simpleType, data);
				ITypeDefOrRef tr = simpleType.Annotation<ITypeDefOrRef>();
				// Fully qualify any ambiguous type names.
				if (tr == null)
					return null;
				var nss = GetNamespace(tr).ToString();
				if (IsAmbiguous(nss, null, GetName(tr))) {
					AstType ns;
					if (string.IsNullOrEmpty(nss)) {
						ns = new SimpleType("global").WithAnnotation(BoxedTextColor.Keyword);
					} else {
						var sb = transform.stringBuilder;
						string[] parts = nss.Split('.');
						var nsAsm = tr.DefinitionAssembly;
						sb.Clear();
						sb.Append(parts[0]);
						if (IsAmbiguous(string.Empty, parts[0], null)) {
							// conflict between namespace and type name/member name
							SimpleType simpleType2;
							ns = new MemberType { Target = simpleType2 = new SimpleType("global").WithAnnotation(BoxedTextColor.Keyword), IsDoubleColon = true, MemberNameToken = Identifier.Create(parts[0]).WithAnnotation(BoxedTextColor.Namespace) }.WithAnnotation(BoxedTextColor.Namespace);
							simpleType2.IdentifierToken.WithAnnotation(BoxedTextColor.Keyword);
						} else {
							SimpleType simpleType2;
							ns = simpleType2 = new SimpleType(parts[0]).WithAnnotation(BoxedTextColor.Namespace);
							simpleType2.IdentifierToken.WithAnnotation(BoxedTextColor.Namespace).WithAnnotation(new NamespaceReference(nsAsm, parts[0]));
						}
						for (int i = 1; i < parts.Length; i++) {
							sb.Append('.');
							sb.Append(parts[i]);
							var nsPart = sb.ToString();
							ns = new MemberType { Target = ns, MemberNameToken = Identifier.Create(parts[i]).WithAnnotation(BoxedTextColor.Namespace).WithAnnotation(new NamespaceReference(nsAsm, nsPart)) }.WithAnnotation(BoxedTextColor.Namespace);
						}
					}
					MemberType mt = new MemberType {
						Target = ns,
						IsDoubleColon = string.IsNullOrEmpty(nss),
						MemberNameToken = (Identifier)simpleType.IdentifierToken.Clone()
					};
					mt.CopyAnnotationsFrom(simpleType);
					simpleType.TypeArguments.MoveTo(mt.TypeArguments);
					simpleType.ReplaceWith(mt);
				}
				return null;
			}

			public override object VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, object data)
			{
				isWithinTypeReferenceExpression = true;
				base.VisitTypeReferenceExpression(typeReferenceExpression, data);
				isWithinTypeReferenceExpression = false;
				return null;
			}

			bool IsAmbiguous(string ns, string name, StringBuilder sbName)
			{
				if (transform.context.Settings.FullyQualifyAllTypes)
					return true;
				// If the type name conflicts with an inner class/type parameter, we need to fully-qualify it:
				if (currentMemberTypes != null && currentMemberTypes.Contains(name ??= sbName.ToString()))
					return true;
				// If the type name conflicts with a field/property etc. on the current class, we need to fully-qualify it,
				// if we're inside an expression.
				if (isWithinTypeReferenceExpression && currentMembers != null) {
					name ??= sbName.ToString();
					if (currentMembers.TryGetValue(name, out var mr)) {
						// However, in the special case where the member is a field or property with the same type
						// as is requested, then we can use the short name (if it's not otherwise ambiguous)
						PropertyDef prop = mr as PropertyDef;
						FieldDef field = mr as FieldDef;
						if (!(prop != null && GetNamespace(prop.PropertySig.GetRetType()).CheckEquals(ns) && GetName(prop.PropertySig.GetRetType()).CheckEquals(name))
							&& !(field != null && field.FieldType != null && GetNamespace(field.FieldType).CheckEquals(ns) && GetName(field.FieldType).CheckEquals(name)))
							return true;
					}
				}
				// If the type is defined in the current namespace,
				// then we can use the short name even if we imported type with same name from another namespace.
				if (ns == currentNamespace && !string.IsNullOrEmpty(ns))
					return false;
				return transform.ambiguousTypeNames.Contains(name ?? sbName.ToString());
			}

			StringBuilder GetNamespace(IType type)
			{
				this.transform.stringBuilder.Clear();
				if (type == null)
					return this.transform.stringBuilder;
				return FullNameFactory.NamespaceSB(type, false, this.transform.stringBuilder);
			}

			StringBuilder GetName(IType type)
			{
				this.transform.stringBuilder.Clear();
				if (type == null)
					return this.transform.stringBuilder;
				return FullNameFactory.NameSB(type, false, this.transform.stringBuilder);
			}
		}
	}
}
