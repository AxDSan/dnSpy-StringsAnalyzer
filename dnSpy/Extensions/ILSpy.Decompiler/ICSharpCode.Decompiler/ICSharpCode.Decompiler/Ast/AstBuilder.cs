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
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.Decompiler.Ast {
	using Ast = ICSharpCode.NRefactory.CSharp;
	using VarianceModifier = ICSharpCode.NRefactory.TypeSystem.VarianceModifier;

	[Flags]
	public enum ConvertTypeOptions
	{
		None = 0,
		IncludeNamespace = 1,
		IncludeTypeParameterDefinitions = 2,
		DoNotUsePrimitiveTypeNames = 4,
		DoNotIncludeEnclosingType = 8,
	}

	public enum DecompiledBodyKind {
		/// <summary>
		/// Decompile the body
		/// </summary>
		Full,

		/// <summary>
		/// Create an empty body, but add extra statements if necessary in order for the code to compile.
		/// </summary>
		Empty,

		/// <summary>
		/// Don't use a body
		/// </summary>
		None,
	}

	enum MethodKind {
		Method,
		Property,
		Event,
	}

	public class AstBuilder
	{
		public DecompilerContext Context {
			get { return context; }
		}
		readonly DecompilerContext context;
		SyntaxTree syntaxTree;
		readonly Dictionary<string, NamespaceDeclaration> astNamespaces = new Dictionary<string, NamespaceDeclaration>();
		bool transformationsHaveRun;
		readonly StringBuilder stringBuilder;// PERF: prevent extra created strings
		readonly char[] commentBuffer;// PERF: prevent extra created strings
		readonly List<Task<AsyncMethodBodyResult>> methodBodyTasks = new List<Task<AsyncMethodBodyResult>>();
		readonly List<AsyncMethodBodyDecompilationState> asyncMethodBodyDecompilationStates = new List<AsyncMethodBodyDecompilationState>();
		internal AutoPropertyProvider AutoPropertyProvider { get; } = new AutoPropertyProvider();
		readonly List<Comment> comments = new List<Comment>();

		struct AsyncMethodBodyResult {
			public readonly EntityDeclaration MethodNode;
			public readonly MethodDef Method;
			public readonly BlockStatement Body;
			public readonly MethodDebugInfoBuilder Builder;
			public readonly FieldToVariableMap VariableMap;
			public readonly bool CurrentMethodIsAsync;
			public readonly bool CurrentMethodIsYieldReturn;

			public AsyncMethodBodyResult(EntityDeclaration methodNode, MethodDef method, BlockStatement body, MethodDebugInfoBuilder builder, FieldToVariableMap variableMap, bool currentMethodIsAsync, bool currentMethodIsYieldReturn) {
				this.MethodNode = methodNode;
				this.Method = method;
				this.Body = body;
				this.Builder = builder;
				this.VariableMap = variableMap;
				this.CurrentMethodIsAsync = currentMethodIsAsync;
				this.CurrentMethodIsYieldReturn = currentMethodIsYieldReturn;
			}
		}
		sealed class AsyncMethodBodyDecompilationState {
			public readonly StringBuilder StringBuilder = new StringBuilder();
		}

		AsyncMethodBodyDecompilationState GetAsyncMethodBodyDecompilationState() {
			lock (asyncMethodBodyDecompilationStates) {
				if (asyncMethodBodyDecompilationStates.Count > 0) {
					var state = asyncMethodBodyDecompilationStates[asyncMethodBodyDecompilationStates.Count - 1];
					asyncMethodBodyDecompilationStates.RemoveAt(asyncMethodBodyDecompilationStates.Count - 1);
					return state;
				}
			}
			return new AsyncMethodBodyDecompilationState();
		}

		void Return(AsyncMethodBodyDecompilationState state) {
			lock (asyncMethodBodyDecompilationStates)
				asyncMethodBodyDecompilationStates.Add(state);
		}

		// "0x" + hexChars(uint)
		const int COMMENT_BUFFER_LENGTH = 2 + 8;

		public Func<AstBuilder, MethodDef, DecompiledBodyKind> GetDecompiledBodyKind { get; set; }

		public AstBuilder(DecompilerContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.context = context;
			this.stringBuilder = new StringBuilder();
			this.commentBuffer = new char[COMMENT_BUFFER_LENGTH];
			this.syntaxTree = new SyntaxTree();
			this.transformationsHaveRun = false;
			this.GetDecompiledBodyKind = null;
		}

		public void Reset()
		{
			this.GetDecompiledBodyKind = null;
			this.syntaxTree = new SyntaxTree();
			this.transformationsHaveRun = false;
			this.astNamespaces.Clear();
			this.stringBuilder.Clear();
			this.context.Reset();
			this.AutoPropertyProvider.Reset();
			this.methodBodyTasks.Clear();
		}

		void WaitForBodies() {
			if (methodBodyTasks.Count == 0)
				return;
			try {
				for (int i = 0; i < methodBodyTasks.Count; i++) {
					var result = methodBodyTasks[i].GetAwaiter().GetResult();
					context.CancellationToken.ThrowIfCancellationRequested();
					if (result.CurrentMethodIsAsync)
						result.MethodNode.Modifiers |= Modifiers.Async;
					result.MethodNode.SetChildByRole(Roles.Body, result.Body);
					result.MethodNode.AddAnnotation(result.Builder);
					result.MethodNode.AddAnnotation(result.VariableMap);
					ConvertAttributes(result.MethodNode, result.Method, result.CurrentMethodIsAsync, result.CurrentMethodIsYieldReturn);

					comments.Clear();
					comments.AddRange(result.MethodNode.GetChildrenByRole(Roles.Comment));
					for (int j = comments.Count - 1; j >= 0; j--) {
						var c = comments[j];
						c.Remove();
						result.MethodNode.InsertChildAfter(null, c, Roles.Comment);
					}
				}
			}
			finally {
				methodBodyTasks.Clear();
			}
		}

		public static bool MemberIsHidden(IMemberRef member, DecompilerSettings settings)
		{
			MethodDef method = member as MethodDef;
			if (method != null) {
				if (method.IsGetter || method.IsSetter || method.IsAddOn || method.IsRemoveOn)
					return true;
				if (settings.ForceShowAllMembers)
					return false;
				if (settings.AnonymousMethods) {
					if (method.Name.StartsWith("_Lambda$__") && method.IsCompilerGenerated())
						return true;
					if (method.HasGeneratedName() && method.IsCompilerGenerated())
						return !method.Name.Contains(">g__");
				}
				return false;
			}

			TypeDef type = member as TypeDef;
			if (type != null) {
				if (settings.ForceShowAllMembers)
					return false;
				if (type.DeclaringType != null) {
					if (settings.AnonymousMethods && IsClosureType(type))
						return true;
					if (settings.YieldReturn && YieldReturnDecompiler.IsCompilerGeneratorEnumerator(type))
						return true;
					if (settings.AsyncAwait && AsyncDecompiler.IsCompilerGeneratedStateMachine(type))
						return true;
					if (type.IsDynamicCallSiteContainerType())
						return true;
				} else if (type.IsCompilerGenerated()) {
					if (type.Name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal))
						return true;
					if (type.IsAnonymousType())
						return true;
				}
				return false;
			}

			FieldDef field = member as FieldDef;
			if (field != null) {
				if (settings.ForceShowAllMembers)
					return false;
				if (field.IsCompilerGenerated()) {
					if (settings.AnonymousMethods && IsAnonymousMethodCacheField(field))
						return true;
					if (settings.AutomaticProperties && IsAutomaticPropertyBackingField(field))
						return true;
					if (settings.SwitchStatementOnString && IsSwitchOnStringCache(field))
						return true;
				}
				// event-fields are not [CompilerGenerated]
				if (settings.AutomaticEvents) {
					string fieldName = field.Name;
					for (int i = 0; i < field.DeclaringType.Events.Count; i++) {
						if (IsEventBackingFieldName(fieldName, field.DeclaringType.Events[i].Name))
							return true;
					}
				}
				return false;
			}

			return false;
		}

		internal static bool IsEventBackingFieldName(string fieldName, string eventName) {
			if (fieldName == eventName)
				return true;

			const string VB_PATTERN = "Event";
			if (fieldName.Length == VB_PATTERN.Length + eventName.Length && fieldName.StartsWith(eventName, StringComparison.Ordinal) && fieldName.EndsWith(VB_PATTERN, StringComparison.Ordinal))
				return true;

			return false;
		}

		static bool IsSwitchOnStringCache(FieldDef field)
		{
			return field.Name.StartsWith("<>f__switch", StringComparison.Ordinal);
		}

		static bool IsAutomaticPropertyBackingField(FieldDef field)
		{
			string name = field.Name;
			if (string.IsNullOrEmpty(name))
				return false;
			// VB's auto prop backing fields are named "_" + PropertyName
			if (name[0] == '_') {
				for (int i = 0; i < field.DeclaringType.Properties.Count; i++) {
					string propName = field.DeclaringType.Properties[i].Name;
					if (propName.Length == name.Length - 1) {
						bool same = true;
						for (int j = 0; j < propName.Length; j++) {
							if (name[j + 1] != propName[j]) {
								same = false;
								break;
							}
						}
						if (same)
							return true;
					}
				}
			}
			return field.HasGeneratedName() && field.Name.EndsWith("BackingField", StringComparison.Ordinal);
		}

		internal static bool IsAnonymousMethodCacheField(FieldDef field)
		{
			return field.Name.StartsWith("CS$<>", StringComparison.Ordinal) || field.Name.StartsWith("<>f__am", StringComparison.Ordinal) || field.Name.StartsWith("<>f__mg", StringComparison.Ordinal);
		}

		static bool IsClosureType(TypeDef type)
		{
			if (!type.IsCompilerGenerated())
				return false;
			if (type.Name.StartsWith("_Closure$__"))
				return true;
			return type.HasGeneratedName() && (type.Name == "<>c" || type.Name.StartsWith("<>c__") || type.Name.Contains("DisplayClass") || type.Name.Contains("AnonStorey"));
		}

		/// <summary>
		/// Runs the C# transformations on the compilation unit.
		/// </summary>
		public void RunTransformations()
		{
			RunTransformations(null);
		}

		public void RunTransformations(Predicate<IAstTransform> transformAbortCondition)
		{
			WaitForBodies();
			TransformationPipeline.RunTransformationsUntil(syntaxTree, transformAbortCondition, context);
			transformationsHaveRun = true;
		}

		/// <summary>
		/// Gets the abstract source tree.
		/// </summary>
		public SyntaxTree SyntaxTree {
			get { return syntaxTree; }
		}

		/// <summary>
		/// Generates C# code from the abstract source tree.
		/// </summary>
		/// <remarks>This method adds ParenthesizedExpressions into the AST, and will run transformations if <see cref="RunTransformations"/> was not called explicitly</remarks>
		public void GenerateCode(IDecompilerOutput output)
		{
			if (!transformationsHaveRun)
				RunTransformations();

			syntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
			GenericGrammarAmbiguityVisitor.ResolveAmbiguities(syntaxTree);
			var outputFormatter = new TextTokenWriter(output, context) { FoldBraces = false };
			var formattingPolicy = context.Settings.CSharpFormattingOptions;
			syntaxTree.AcceptVisitor(new CSharpOutputVisitor(outputFormatter, formattingPolicy, context.CancellationToken));
		}

		public void AddAssembly(AssemblyDef assemblyDefinition, bool onlyAssemblyLevel = false)
		{
			AddAssembly(assemblyDefinition.ManifestModule, onlyAssemblyLevel, true, true);
		}

		public void AddAssembly(ModuleDef moduleDefinition, bool onlyAssemblyLevel, bool decompileAsm, bool decompileMod)
		{
			if (decompileAsm && moduleDefinition.Assembly != null)
				ConvertCustomAttributes(Context.MetadataTextColorProvider, syntaxTree, moduleDefinition.Assembly, context.Settings, stringBuilder, "assembly");
			if (decompileMod)
				ConvertCustomAttributes(Context.MetadataTextColorProvider, syntaxTree, moduleDefinition, context.Settings, stringBuilder, "module");

			if (decompileMod && !onlyAssemblyLevel) {
				for (int i = 0; i < moduleDefinition.Types.Count; i++) {
					var typeDef = moduleDefinition.Types[i];
					// Skip the <Module> class
					if (typeDef.IsGlobalModuleType) continue;
					// Skip any hidden types
					if (AstBuilder.MemberIsHidden(typeDef, context.Settings))
						continue;

					AddType(typeDef);
				}
			}
		}

		NamespaceDeclaration GetCodeNamespace(string name, IAssembly asm)
		{
			if (string.IsNullOrEmpty(name)) {
				return null;
			}
			if (astNamespaces.ContainsKey(name)) {
				return astNamespaces[name];
			} else {
				// Create the namespace
				NamespaceDeclaration astNamespace = new NamespaceDeclaration(name, asm);
				syntaxTree.Members.Add(astNamespace);
				astNamespaces[name] = astNamespace;
				return astNamespace;
			}
		}

		char ToHexChar(int val) {
			Debug.Assert(0 <= val && val <= 0x0F);
			if (0 <= val && val <= 9)
				return (char)('0' + val);
			return (char)('A' + val - 10);
		}

		string ToHex(uint value) {
			commentBuffer[0] = '0';
			commentBuffer[1] = 'x';
			int j = 2;
			for (int i = 0; i < 4; i++) {
				commentBuffer[j++] = ToHexChar((int)(value >> 28) & 0x0F);
				commentBuffer[j++] = ToHexChar((int)(value >> 24) & 0x0F);
				value <<= 8;
			}
			return new string(commentBuffer, 0, j);
		}

		void AddComment(AstNode node, IMemberDef member, string text = null)
		{
			if (!this.context.Settings.ShowTokenAndRvaComments)
				return;
			uint rva;
			long fileOffset;
			member.GetRVA(out rva, out fileOffset);

			var creator = new CommentReferencesCreator(stringBuilder);
			creator.AddText(" ");
			if (text != null) {
				creator.AddText("(");
				creator.AddText(text);
				creator.AddText(") ");
			}
			creator.AddText("Token: ");
			creator.AddReference(ToHex(member.MDToken.Raw), new TokenReference(member));
			creator.AddText(" RID: ");
			creator.AddText(member.MDToken.Rid.ToString());
			if (rva != 0) {
				var mod = member.Module;
				var filename = mod == null ? null : mod.Location;
				creator.AddText(" RVA: ");
				creator.AddReference(ToHex(rva), new AddressReference(filename, true, rva, 0));
				creator.AddText(" File Offset: ");
				creator.AddReference(ToHex((uint)fileOffset), new AddressReference(filename, false, (ulong)fileOffset, 0));
			}

			var cmt = new Comment(creator.Text);
			cmt.References = creator.CommentReferences;
			node.InsertChildAfter(null, cmt, Roles.Comment);
		}

		public void AddType(TypeDef typeDef)
		{
			var astType = CreateType(typeDef);
			NamespaceDeclaration astNS = GetCodeNamespace(typeDef.Namespace, typeDef.DefinitionAssembly);
			if (astNS != null) {
				astNS.Members.Add(astType);
			} else {
				syntaxTree.Members.Add(astType);
			}
		}

		public void AddMethod(MethodDef method)
		{
			AstNode node = method.IsConstructor ? (AstNode)CreateConstructor(method) : CreateMethod(method);
			syntaxTree.Members.Add(node);
		}

		public void AddProperty(PropertyDef property)
		{
			syntaxTree.Members.Add(CreateProperty(property));
		}

		public void AddField(FieldDef field)
		{
			syntaxTree.Members.Add(CreateField(field));
		}

		public void AddEvent(EventDef ev)
		{
			syntaxTree.Members.Add(CreateEvent(ev));
		}

		/// <summary>
		/// Creates the AST for a type definition.
		/// </summary>
		/// <param name="typeDef"></param>
		/// <returns>TypeDeclaration or DelegateDeclaration.</returns>
		public EntityDeclaration CreateType(TypeDef typeDef)
		{
			// create type
			TypeDef oldCurrentType = context.CurrentType;
			context.CurrentType = typeDef;
			TypeDeclaration astType = new TypeDeclaration();
			ConvertAttributes(astType, typeDef);
			astType.AddAnnotation(typeDef);
			astType.Modifiers = ConvertModifiers(typeDef);
			astType.NameToken = Identifier.Create(CleanName(typeDef.Name)).WithAnnotation(typeDef);

			if (typeDef.IsEnum) {  // NB: Enum is value type
				astType.ClassType = ClassType.Enum;
				astType.Modifiers &= ~Modifiers.Sealed;
			} else if (DnlibExtensions.IsValueType(typeDef)) {
				astType.ClassType = ClassType.Struct;
				astType.Modifiers &= ~(Modifiers.Sealed | Modifiers.Abstract | Modifiers.Static);
				if (DnlibExtensions.HasIsReadOnlyAttribute(typeDef))
					astType.Modifiers |= Modifiers.Readonly;
				if (DnlibExtensions.HasIsByRefLikeAttribute(typeDef))
					astType.Modifiers |= Modifiers.Ref;
			}
			else if (typeDef.IsInterface) {
				astType.ClassType = ClassType.Interface;
				astType.Modifiers &= ~Modifiers.Abstract;
			} else {
				astType.ClassType = ClassType.Class;
			}

			IList<GenericParam> genericParameters = typeDef.GenericParameters;
			if (typeDef.DeclaringType != null && typeDef.DeclaringType.HasGenericParameters) {
				int parentGenericCount = typeDef.DeclaringType.GenericParameters.Count;
				int genericParametersCount = genericParameters.Count;

				var newGenericParameters = new List<GenericParam>(genericParametersCount - parentGenericCount);
				for (int i = parentGenericCount; i < genericParametersCount; i++)
					newGenericParameters.Add(genericParameters[i]);

				genericParameters = newGenericParameters;
			}
			astType.TypeParameters.AddRange(MakeTypeParameters(genericParameters));
			astType.Constraints.AddRange(MakeConstraints(genericParameters));

			EntityDeclaration result = astType;
			if (typeDef.IsEnum) {
				long expectedEnumMemberValue = 0;
				bool forcePrintingInitializers = IsFlagsEnum(typeDef);
				var enumType = typeDef.Fields.FirstOrDefault(f => !f.IsStatic);
				for (int i = 0; i < typeDef.Fields.Count; i++) {
					var field = typeDef.Fields[i];
					if (!field.IsStatic) {
						// the value__ field
						if (!new SigComparer().Equals(field.FieldType, typeDef.Module.CorLibTypes.Int32)) {
							astType.AddChild(ConvertType(field.FieldType, stringBuilder), Roles.BaseType);
						}
					} else {
						EnumMemberDeclaration enumMember = new EnumMemberDeclaration();
						ConvertCustomAttributes(Context.MetadataTextColorProvider, enumMember, field, context.Settings, stringBuilder);
						enumMember.AddAnnotation(field);
						enumMember.NameToken = Identifier.Create(CleanName(field.Name)).WithAnnotation(field);
						TryGetConstant(field, out var constant);
						TypeCode c = constant == null ? TypeCode.Empty : Type.GetTypeCode(constant.GetType());
						if (c < TypeCode.Char || c > TypeCode.Decimal)
							continue;
						long memberValue = (long)CSharpPrimitiveCast.Cast(TypeCode.Int64, constant, false);
						if (forcePrintingInitializers || memberValue != expectedEnumMemberValue) {
							enumMember.AddChild(new PrimitiveExpression(ConvertConstant(enumType == null ? null : enumType.FieldSig.GetFieldType(), constant)), EnumMemberDeclaration.InitializerRole);
						}
						expectedEnumMemberValue = memberValue + 1;
						astType.AddChild(enumMember, Roles.TypeMemberRole);
						AddComment(enumMember, field);
					}
				}
			} else if (IsNormalDelegate(typeDef)) {
				DelegateDeclaration dd = new DelegateDeclaration();
				dd.Modifiers = astType.Modifiers & ~Modifiers.Sealed;
				dd.NameToken = (Identifier)astType.NameToken.Clone();
				dd.AddAnnotation(typeDef);
				astType.Attributes.MoveTo(dd.Attributes);
				astType.TypeParameters.MoveTo(dd.TypeParameters);
				astType.Constraints.MoveTo(dd.Constraints);
				for (int i = 0; i < typeDef.Methods.Count; i++) {
					var m = typeDef.Methods[i];
					if (m.Name == "Invoke") {
						dd.ReturnType = ConvertType(m.ReturnType, stringBuilder, m.Parameters.ReturnParameter.ParamDef);
						dd.Parameters.AddRange(MakeParameters(Context.MetadataTextColorProvider, m, context.Settings, stringBuilder));
						ConvertAttributes(dd, m.Parameters.ReturnParameter);
						AddComment(dd, m, "Invoke");
					}
				}
				AddComment(dd, typeDef);
				result = dd;
			} else {
				// Base type
				if (typeDef.BaseType != null && !DnlibExtensions.IsValueType(typeDef) && !typeDef.BaseType.IsSystemObject()) {
					astType.AddChild(ConvertType(typeDef.BaseType, stringBuilder), Roles.BaseType);
				}
				var interfaceImpls = GetInterfaceImpls(typeDef);
				for (int i = 0; i < interfaceImpls.Count; i++)
					astType.AddChild(ConvertType(interfaceImpls[i].Interface, stringBuilder), Roles.BaseType);

				AddTypeMembers(astType, typeDef);

				if (astType.Members.OfType<IndexerDeclaration>().Any(idx => idx.PrivateImplementationType.IsNull)) {
					// Remove the [DefaultMember] attribute if the class contains indexers
					foreach (AttributeSection section in astType.Attributes) {
						foreach (Ast.Attribute attr in section.Attributes) {
							ITypeDefOrRef tr = attr.Type.Annotation<ITypeDefOrRef>();
							if (tr != null && tr.Compare(systemReflectionString, defaultMemberAttributeString)) {
								attr.Remove();
							}
						}
						if (section.Attributes.Count == 0)
							section.Remove();
					}
				}
			}

			AddComment(astType, typeDef);
			context.CurrentType = oldCurrentType;
			return result;
		}
		static readonly UTF8String systemReflectionString = new UTF8String("System.Reflection");
		static readonly UTF8String defaultMemberAttributeString = new UTF8String("DefaultMemberAttribute");
		static readonly UTF8String systemString = new UTF8String("System");
		static readonly UTF8String multicastDelegateString = new UTF8String("MulticastDelegate");

		static void RemoveFirst<T>(IList<T> collection, int count) {
			if (collection is List<T> list)
				list.RemoveRange(0, count);
			else {
				for (int i = count - 1; i >= 0; i--)
					collection.RemoveAt(i);
			}
		}

		bool IsNormalDelegate(TypeDef td)
		{
			if (!td.BaseType.Compare(systemString, multicastDelegateString))
				return false;

			if (td.HasFields)
				return false;
			if (td.HasProperties)
				return false;
			if (td.HasEvents)
				return false;
			if (td.Methods.Any(m => m.Body != null))
				return false;

			return true;
		}

		internal static string CleanName(string name)
		{
			int pos = name.LastIndexOf('`');
			if (pos >= 0)
				name = name.Substring(0, pos);
			// Could be a compiler-generated name, eg. "<.ctor>b__0_0"
			if (name.Length == 0 || name[0] != '<') {
				pos = name.LastIndexOf('.');
				if (pos >= 0)
					name = name.Substring(pos + 1);
			}
			return name;
		}

		#region Create TypeOf Expression
		/// <summary>
		/// Creates a typeof-expression for the specified type.
		/// </summary>
		public static TypeOfExpression CreateTypeOfExpression(ITypeDefOrRef type, StringBuilder sb)
		{
			return new TypeOfExpression(AddEmptyTypeArgumentsForUnboundGenerics(ConvertType(type, sb)));
		}

		static AstType AddEmptyTypeArgumentsForUnboundGenerics(AstType type)
		{
			ITypeDefOrRef typeRef = type.Annotation<ITypeDefOrRef>();
			if (typeRef == null)
				return type;
			TypeDef typeDef = typeRef.ResolveTypeDef(); // need to resolve to figure out the number of type parameters
			if (typeDef == null || !typeDef.HasGenericParameters)
				return type;
			SimpleType sType = type as SimpleType;
			MemberType mType = type as MemberType;
			if (sType != null) {
				while (typeDef.GenericParameters.Count > sType.TypeArguments.Count) {
					sType.TypeArguments.Add(new SimpleType("").WithAnnotation(BoxedTextColor.TypeGenericParameter).WithAnnotation(SimpleType.DummyTypeGenericParam));
				}
			}

			if (mType != null) {
				AddEmptyTypeArgumentsForUnboundGenerics(mType.Target);

				int outerTypeParamCount = typeDef.DeclaringType == null ? 0 : typeDef.DeclaringType.GenericParameters.Count;

				while (typeDef.GenericParameters.Count - outerTypeParamCount > mType.TypeArguments.Count) {
					mType.TypeArguments.Add(new SimpleType("").WithAnnotation(BoxedTextColor.TypeGenericParameter).WithAnnotation(SimpleType.DummyTypeGenericParam));
				}
			}

			return type;
		}
		#endregion

		#region Convert Type Reference
		/// <summary>
		/// Converts a type reference.
		/// </summary>
		/// <param name="type">The type reference that should be converted into
		/// a type system type reference.</param>
		/// <param name="typeAttributes">Attributes associated with the type reference.
		/// This is used to support the 'dynamic' type.</param>
		public static AstType ConvertType(ITypeDefOrRef type, StringBuilder sb, IHasCustomAttribute typeAttributes = null, ConvertTypeOptions options = ConvertTypeOptions.None)
		{
			int typeIndex = 0;
			return ConvertType(type, typeAttributes, ref typeIndex, options, 0, sb);
		}

		/// <summary>
		/// Converts a type reference.
		/// </summary>
		/// <param name="type">The type reference that should be converted into
		/// a type system type reference.</param>
		/// <param name="typeAttributes">Attributes associated with the type reference.
		/// This is used to support the 'dynamic' type.</param>
		public static AstType ConvertType(TypeSig type, StringBuilder sb, IHasCustomAttribute typeAttributes = null, ConvertTypeOptions options = ConvertTypeOptions.None)
		{
			int typeIndex = 0;
			return ConvertType(type, typeAttributes, ref typeIndex, options, 0, sb);
		}

		const int MAX_CONVERTTYPE_DEPTH = 50;
		static AstType ConvertType(TypeSig type, IHasCustomAttribute typeAttributes, ref int typeIndex, ConvertTypeOptions options, int depth, StringBuilder sb)
		{
			if (depth++ > MAX_CONVERTTYPE_DEPTH)
				return AstType.Null;
			type = type.RemovePinned();
			if (type == null) {
				return AstType.Null;
			}

			if (type is ByRefSig) {
				typeIndex++;
				// by reference type cannot be represented in C#; so we'll represent it as a pointer instead
				return ConvertType((type as ByRefSig).Next, typeAttributes, ref typeIndex, options, depth, sb)
					.MakePointerType();
			} else if (type is PtrSig) {
				typeIndex++;
				return ConvertType((type as PtrSig).Next, typeAttributes, ref typeIndex, options, depth, sb)
					.MakePointerType();
			} else if (type is ArraySigBase) {
				typeIndex++;
				return ConvertType((type as ArraySigBase).Next, typeAttributes, ref typeIndex, options, depth, sb)
					.MakeArrayType((int)(type as ArraySigBase).Rank);
			} else if (type is GenericInstSig) {
				GenericInstSig gType = (GenericInstSig)type;
				if (gType.GenericType != null && gType.GenericArguments.Count == 1 && gType.GenericType.IsSystemNullable()) {
					typeIndex++;
					return new ComposedType {
						BaseType = ConvertType(gType.GenericArguments[0], typeAttributes, ref typeIndex, options, depth, sb),
						HasNullableSpecifier = true
					};
				}
				AstType baseType = ConvertType(gType.GenericType?.TypeDefOrRef, typeAttributes, ref typeIndex, options & ~ConvertTypeOptions.IncludeTypeParameterDefinitions, depth, sb);
				List<AstType> typeArguments = new List<AstType>(gType.GenericArguments.Count);
				for (int i = 0; i < gType.GenericArguments.Count; i++) {
					typeIndex++;
					typeArguments.Add(ConvertType(gType.GenericArguments[i], typeAttributes, ref typeIndex, options, depth, sb));
				}
				ApplyTypeArgumentsTo(baseType, typeArguments);
				return baseType;
			} else if (type is GenericSig) {
				var sig = (GenericSig)type;
				var simpleType = new SimpleType(sig.GetName(sb)).WithAnnotation(sig.GenericParam).WithAnnotation(type);
				simpleType.IdentifierToken.WithAnnotation(sig.GenericParam).WithAnnotation(type);
				return simpleType;
			} else if (type is TypeDefOrRefSig) {
				return ConvertType(((TypeDefOrRefSig)type).TypeDefOrRef, typeAttributes, ref typeIndex, options, depth, sb);
			} else if (type is ModifierSig modifierSig) {
				typeIndex++;
				return ConvertType(modifierSig.Next, typeAttributes, ref typeIndex, options, depth, sb);
			} else
				return ConvertType(type.ToTypeDefOrRef(), typeAttributes, ref typeIndex, options, depth, sb);
		}

		static AstType ConvertType(ITypeDefOrRef type, IHasCustomAttribute typeAttributes, ref int typeIndex, ConvertTypeOptions options, int depth, StringBuilder sb)
		{
			if (depth++ > MAX_CONVERTTYPE_DEPTH || type == null)
				return AstType.Null;

			var ts = type as TypeSpec;
			if (ts != null && !(ts.TypeSig is FnPtrSig))
				return ConvertType(ts.TypeSig, typeAttributes, ref typeIndex, options, depth, sb);

			if (type.DeclaringType != null && (options & ConvertTypeOptions.DoNotIncludeEnclosingType) == 0) {
				AstType typeRef = ConvertType(type.DeclaringType, typeAttributes, ref typeIndex, options & ~ConvertTypeOptions.IncludeTypeParameterDefinitions, depth, sb);
				string namepart = ICSharpCode.NRefactory.TypeSystem.ReflectionHelper.SplitTypeParameterCountFromReflectionName(type.Name);
				MemberType memberType = new MemberType { Target = typeRef, MemberNameToken = Identifier.Create(namepart).WithAnnotation(type) };
				memberType.AddAnnotation(type);
				if ((options & ConvertTypeOptions.IncludeTypeParameterDefinitions) == ConvertTypeOptions.IncludeTypeParameterDefinitions) {
					AddTypeParameterDefininitionsTo(type, memberType);
				}
				return memberType;
			} else {
				string ns = type.GetNamespace(sb) ?? string.Empty;
				string name = type.GetName(sb);
				if (ts != null)
					name = DnlibExtensions.GetFnPtrName(ts.TypeSig as FnPtrSig);
				if (name == null)
					throw new InvalidOperationException("type.Name returned null. Type: " + type.ToString());

				if (name == "Object" && ns == "System" && HasDynamicAttribute(typeAttributes, typeIndex)) {
					return new PrimitiveType("dynamic");
				} else {
					if (ns == "System") {
						if ((options & ConvertTypeOptions.DoNotUsePrimitiveTypeNames)
							!= ConvertTypeOptions.DoNotUsePrimitiveTypeNames) {
							switch (name) {
								case "SByte":
									return new PrimitiveType("sbyte").WithAnnotation(type);
								case "Int16":
									return new PrimitiveType("short").WithAnnotation(type);
								case "Int32":
									return new PrimitiveType("int").WithAnnotation(type);
								case "Int64":
									return new PrimitiveType("long").WithAnnotation(type);
								case "Byte":
									return new PrimitiveType("byte").WithAnnotation(type);
								case "UInt16":
									return new PrimitiveType("ushort").WithAnnotation(type);
								case "UInt32":
									return new PrimitiveType("uint").WithAnnotation(type);
								case "UInt64":
									return new PrimitiveType("ulong").WithAnnotation(type);
								case "String":
									return new PrimitiveType("string").WithAnnotation(type);
								case "Single":
									return new PrimitiveType("float").WithAnnotation(type);
								case "Double":
									return new PrimitiveType("double").WithAnnotation(type);
								case "Decimal":
									return new PrimitiveType("decimal").WithAnnotation(type);
								case "Char":
									return new PrimitiveType("char").WithAnnotation(type);
								case "Boolean":
									return new PrimitiveType("bool").WithAnnotation(type);
								case "Void":
									return new PrimitiveType("void").WithAnnotation(type);
								case "Object":
									return new PrimitiveType("object").WithAnnotation(type);
							}
						}
					}

					name = ICSharpCode.NRefactory.TypeSystem.ReflectionHelper.SplitTypeParameterCountFromReflectionName(name);

					AstType astType;
					if ((options & ConvertTypeOptions.IncludeNamespace) == ConvertTypeOptions.IncludeNamespace && ns.Length > 0) {
						string[] parts = ns.Split('.');
						var nsAsm = type.DefinitionAssembly;
						sb.Clear();
						sb.Append(parts[0]);
						SimpleType simpleType;
						AstType nsType = simpleType = new SimpleType(parts[0]).WithAnnotation(BoxedTextColor.Namespace);
						simpleType.IdentifierToken.WithAnnotation(BoxedTextColor.Namespace).WithAnnotation(new NamespaceReference(nsAsm, parts[0]));
						for (int i = 1; i < parts.Length; i++) {
							sb.Append('.');
							sb.Append(parts[i]);
							var nsPart = sb.ToString();
							nsType = new MemberType { Target = nsType, MemberNameToken = Identifier.Create(parts[i]).WithAnnotation(BoxedTextColor.Namespace).WithAnnotation(new NamespaceReference(nsAsm, nsPart)) }.WithAnnotation(BoxedTextColor.Namespace);
						}
						astType = new MemberType { Target = nsType, MemberNameToken = Identifier.Create(name).WithAnnotation(type) };
					} else {
						astType = new SimpleType(name);
					}
					astType.AddAnnotation(type);

					if ((options & ConvertTypeOptions.IncludeTypeParameterDefinitions) == ConvertTypeOptions.IncludeTypeParameterDefinitions) {
						AddTypeParameterDefininitionsTo(type, astType);
					}
					return astType;
				}
			}
		}

		static void AddTypeParameterDefininitionsTo(ITypeDefOrRef type, AstType astType)
		{
			TypeDef typeDef = type.ResolveTypeDef();
			if (typeDef != null && typeDef.HasGenericParameters) {
				List<AstType> typeArguments = new List<AstType>(typeDef.GenericParameters.Count);
				for (int i = 0; i < typeDef.GenericParameters.Count; i++) {
					var gp = typeDef.GenericParameters[i];
					typeArguments.Add(new SimpleType(gp.Name).WithAnnotation(gp));
				}
				ApplyTypeArgumentsTo(astType, typeArguments);
			}
		}

		static void ApplyTypeArgumentsTo(AstType baseType, List<AstType> typeArguments)
		{
			SimpleType st = baseType as SimpleType;
			if (st != null) {
				st.TypeArguments.AddRange(typeArguments);
			}
			MemberType mt = baseType as MemberType;
			if (mt != null) {
				ITypeDefOrRef type = mt.Annotation<ITypeDefOrRef>();
				if (type != null) {
					int typeParameterCount;
					var td = type.ResolveTypeDef();
					if (td is not null) {
						if (td.DeclaringType is not null && td.DeclaringType.HasGenericParameters)
							typeParameterCount = td.GenericParameters.Count - td.DeclaringType.GenericParameters.Count;
						else
							typeParameterCount = td.GenericParameters.Count;
					}
					else {
						// Fallback to type.Name for unresolved type references since they do not store generic parameter information
						typeParameterCount = GetTypeParameterCountFromReflectionName(type.Name);
					}
					if (typeParameterCount > typeArguments.Count)
						typeParameterCount = typeArguments.Count;
					mt.TypeArguments.AddRange(typeArguments.GetRange(typeArguments.Count - typeParameterCount, typeParameterCount));
					typeArguments.RemoveRange(typeArguments.Count - typeParameterCount, typeParameterCount);
					if (typeArguments.Count > 0)
						ApplyTypeArgumentsTo(mt.Target, typeArguments);
				} else {
					mt.TypeArguments.AddRange(typeArguments);
				}
			}
		}

		static int GetTypeParameterCountFromReflectionName(string reflectionName)
		{
			int pos = reflectionName.LastIndexOf('`');
			if (pos < 0)
				return 0;
			if (int.TryParse(reflectionName.Substring(pos + 1), out var typeParameterCount))
				return typeParameterCount;
			return 0;
		}

		static readonly UTF8String systemRuntimeCompilerServicesString = new UTF8String("System.Runtime.CompilerServices");
		static readonly UTF8String dynamicAttributeString = new UTF8String("DynamicAttribute");
		static bool HasDynamicAttribute(IHasCustomAttribute attributeProvider, int typeIndex)
		{
			if (attributeProvider == null)
				return false;
			for (int i = 0; i < attributeProvider.CustomAttributes.Count; i++) {
				var a = attributeProvider.CustomAttributes[i];
				if (a.AttributeType.Compare(systemRuntimeCompilerServicesString, dynamicAttributeString)) {
					if (a.ConstructorArguments.Count == 1) {
						IList<CAArgument> values = a.ConstructorArguments[0].Value as IList<CAArgument>;
						if (values != null && typeIndex < values.Count && values[typeIndex].Value is bool)
							return (bool)values[typeIndex].Value;
					}
					return true;
				}
			}
			return false;
		}
		#endregion

		#region ConvertModifiers
		Modifiers ConvertModifiers(TypeDef typeDef)
		{
			Modifiers modifiers = Modifiers.None;
			if (typeDef.IsNestedPrivate) {
				if (context.Settings.MemberAddPrivateModifier)
					modifiers |= Modifiers.Private;
			} else if (typeDef.IsNotPublic) {
				if (context.Settings.TypeAddInternalModifier)
					modifiers |= Modifiers.Internal;
			} else if (typeDef.IsNestedAssembly || typeDef.IsNestedFamilyAndAssembly)
				modifiers |= Modifiers.Internal;
			else if (typeDef.IsNestedFamily)
				modifiers |= Modifiers.Protected;
			else if (typeDef.IsNestedFamilyOrAssembly)
				modifiers |= Modifiers.Protected | Modifiers.Internal;
			else if (typeDef.IsPublic || typeDef.IsNestedPublic)
				modifiers |= Modifiers.Public;

			if (typeDef.IsAbstract && typeDef.IsSealed)
				modifiers |= Modifiers.Static;
			else if (typeDef.IsAbstract)
				modifiers |= Modifiers.Abstract;
			else if (typeDef.IsSealed)
				modifiers |= Modifiers.Sealed;

			return modifiers;
		}

		Modifiers ConvertModifiers(FieldDef fieldDef)
		{
			Modifiers modifiers = Modifiers.None;
			if (fieldDef.IsPrivate) {
				if (context.Settings.MemberAddPrivateModifier)
					modifiers |= Modifiers.Private;
			} else if (fieldDef.IsAssembly)
				modifiers |= Modifiers.Internal;
			else if (fieldDef.IsFamily)
				modifiers |= Modifiers.Protected;
			else if (fieldDef.IsFamilyOrAssembly)
				modifiers |= Modifiers.Protected | Modifiers.Internal;
			else if (fieldDef.IsPublic)
				modifiers |= Modifiers.Public;
			else if (fieldDef.IsFamilyAndAssembly)
				modifiers |= Modifiers.Private | Modifiers.Protected;

			if (fieldDef.IsLiteral) {
				modifiers |= Modifiers.Const;
			} else {
				if (fieldDef.IsStatic)
					modifiers |= Modifiers.Static;

				if (fieldDef.IsInitOnly)
					modifiers |= Modifiers.Readonly;
			}

			CModReqdSig modreq = fieldDef.FieldType as CModReqdSig;
			if (modreq != null && modreq.Modifier != null && modreq.Modifier.Compare(systemRuntimeCompilerServicesString, isVolatileString))
				modifiers |= Modifiers.Volatile;

			return modifiers;
		}
		static readonly UTF8String isVolatileString = new UTF8String("IsVolatile");

		Modifiers ConvertModifiers(MethodDef methodDef, bool canBeReadOnlyMember)
		{
			if (methodDef == null)
				return Modifiers.None;
			Modifiers modifiers = Modifiers.None;
			if (methodDef.IsPrivate) {
				if (context.Settings.MemberAddPrivateModifier)
					modifiers |= Modifiers.Private;
			}
			else if (methodDef.IsAssembly)
				modifiers |= Modifiers.Internal;
			else if (methodDef.IsFamily)
				modifiers |= Modifiers.Protected;
			else if (methodDef.IsFamilyOrAssembly)
				modifiers |= Modifiers.Protected | Modifiers.Internal;
			else if (methodDef.IsPublic)
				modifiers |= Modifiers.Public;
			else if (methodDef.IsFamilyAndAssembly)
				modifiers |= Modifiers.Private | Modifiers.Protected;

			if (methodDef.IsStatic)
				modifiers |= Modifiers.Static;

			if (methodDef.IsAbstract) {
				modifiers |= Modifiers.Abstract;
				if (!methodDef.IsNewSlot)
					modifiers |= GetOverrideModifierOrDefault(methodDef, Modifiers.None);
			} else if (methodDef.IsFinal) {
				if (!methodDef.IsNewSlot) {
					modifiers |= Modifiers.Sealed | GetOverrideModifierOrDefault(methodDef, Modifiers.None);
				}
			} else if (methodDef.IsVirtual) {
				var virtualModifier = methodDef.DeclaringType.IsSealed ? Modifiers.None : Modifiers.Virtual;
				if (methodDef.IsNewSlot)
					modifiers |= virtualModifier;
				else
					modifiers |= GetOverrideModifierOrDefault(methodDef, virtualModifier);
			}
			if (!methodDef.HasBody && !methodDef.IsAbstract)
				modifiers |= Modifiers.Extern;
			if (canBeReadOnlyMember && IsReadonlyMember(methodDef))
				modifiers |= Modifiers.ReadonlyMember;

			return modifiers;
		}

		bool IsReadonlyMember(MethodDef methodDef)
		{
			return methodDef != null && !methodDef.IsStatic && DnlibExtensions.HasIsReadOnlyAttribute(methodDef);
		}

		// mcs doesn't set IsNewSlot if it doesn't override anything so verify that
		// it's a method override.
		static Modifiers GetOverrideModifierOrDefault(MethodDef method, Modifiers defaultValue) {
			var baseType = method.DeclaringType.BaseType;
			var name = method.Name;
			int paramCount = method.MethodSig.GetParamCount();
			while (baseType != null) {
				var type = baseType.Resolve();
				// If we failed to resolve it, assume it's a method override
				if (type == null)
					return Modifiers.Override;
				for (int i = 0; i < type.Methods.Count; i++) {
					var m = type.Methods[i];
					// This method doesn't handle generic base classes so assume it matches if name
					// and param count matches.
					if (m.IsVirtual && m.Name == name && m.MethodSig.GetParamCount() == paramCount)
						return Modifiers.Override;
				}
				baseType = type.BaseType;
			}
			return defaultValue;
		}

		#endregion

		IList<InterfaceImpl> GetInterfaceImpls(TypeDef type)
		{
			if (context.Settings.UseSourceCodeOrder)
				return type.Interfaces;// These are already sorted by MD token
			return type.GetInterfaceImpls(context.Settings.SortMembers);
		}

		IList<TypeDef> GetNestedTypes(TypeDef type)
		{
			if (context.Settings.UseSourceCodeOrder)
				return type.NestedTypes;// These are already sorted by MD token
			return type.GetNestedTypes(context.Settings.SortMembers);
		}

		IList<FieldDef> GetFields(TypeDef type)
		{
			if (context.Settings.UseSourceCodeOrder)
				return type.Fields;// These are already sorted by MD token
			return type.GetFields(context.Settings.SortMembers);
		}

		void AddTypeMembers(TypeDeclaration astType, TypeDef typeDef)
		{
			bool hasShownMethods = false;
			foreach (var d in this.context.Settings.DecompilationObjects) {
				switch (d) {
				case DecompilationObject.NestedTypes:
					var nestedTypes = GetNestedTypes(typeDef);
					for (int i = 0; i < nestedTypes.Count; i++) {
						var nestedTypeDef = nestedTypes[i];
						if (MemberIsHidden(nestedTypeDef, context.Settings))
							continue;
						var nestedType = CreateType(nestedTypeDef);
						SetNewModifier(nestedType);
						astType.AddChild(nestedType, Roles.TypeMemberRole);
					}
					break;

				case DecompilationObject.Fields:
					var fieldDefs = GetFields(typeDef);
					for (int i = 0; i < fieldDefs.Count; i++) {
						var fieldDef = fieldDefs[i];
						if (MemberIsHidden(fieldDef, context.Settings)) continue;
						astType.AddChild(CreateField(fieldDef), Roles.TypeMemberRole);
					}
					break;

				case DecompilationObject.Events:
					if (hasShownMethods)
						break;
					if (context.Settings.UseSourceCodeOrder || !typeDef.CanSortMethods()) {
						ShowAllMethods(astType, typeDef);
						hasShownMethods = true;
						break;
					}
					var eventDefs = typeDef.GetEvents(context.Settings.SortMembers);
					for (int i = 0; i < eventDefs.Count; i++) {
						var eventDef = eventDefs[i];
						if (eventDef.AddMethod == null && eventDef.RemoveMethod == null)
							continue;
						astType.AddChild(CreateEvent(eventDef), Roles.TypeMemberRole);
					}
					break;

				case DecompilationObject.Properties:
					if (hasShownMethods)
						break;
					if (context.Settings.UseSourceCodeOrder || !typeDef.CanSortMethods()) {
						ShowAllMethods(astType, typeDef);
						hasShownMethods = true;
						break;
					}
					var propertyDefs = typeDef.GetProperties(context.Settings.SortMembers);
					for (int i = 0; i < propertyDefs.Count; i++) {
						var propDef = propertyDefs[i];
						if (propDef.GetMethod == null && propDef.SetMethod == null)
							continue;
						astType.Members.Add(CreateProperty(propDef));
					}
					break;

				case DecompilationObject.Methods:
					if (hasShownMethods)
						break;
					if (context.Settings.UseSourceCodeOrder || !typeDef.CanSortMethods()) {
						ShowAllMethods(astType, typeDef);
						hasShownMethods = true;
						break;
					}
					var methodDefs = typeDef.GetMethods(context.Settings.SortMembers);
					for (int i = 0; i < methodDefs.Count; i++) {
						var methodDef = methodDefs[i];
						if (MemberIsHidden(methodDef, context.Settings)) continue;

						if (methodDef.IsConstructor)
							astType.Members.Add(CreateConstructor(methodDef));
						else
							astType.Members.Add(CreateMethod(methodDef));
					}
					break;

				default: throw new InvalidOperationException();
				}
			}
		}

		void ShowAllMethods(TypeDeclaration astType, TypeDef type)
		{
			foreach (var def in type.GetNonSortedMethodsPropertiesEvents()) {
				var md = def as MethodDef;
				if (md != null) {
					if (MemberIsHidden(md, context.Settings))
						continue;
					if (md.IsConstructor)
						astType.Members.Add(CreateConstructor(md));
					else
						astType.Members.Add(CreateMethod(md));
					continue;
				}

				var pd = def as PropertyDef;
				if (pd != null) {
					if (pd.GetMethod is not null || pd.SetMethod is not null)
						astType.Members.Add(CreateProperty(pd));

					// Methods marked as 'other' are not supported in C#
					for (int i = 0; i < pd.OtherMethods.Count; i++) {
						var otherMethod = pd.OtherMethods[i];
						if (otherMethod.DeclaringType != pd.DeclaringType)
							continue;
						astType.Members.Add(CreateMethod(otherMethod));
					}

					continue;
				}

				var ed = def as EventDef;
				if (ed != null) {
					if (ed.AddMethod is not null || ed.RemoveMethod is not null)
						astType.Members.Add(CreateEvent(ed));

					// Methods marked as 'fire' are not supported in C#
					if (ed.InvokeMethod is not null && ed.InvokeMethod.DeclaringType == ed.DeclaringType)
						astType.Members.Add(CreateMethod(ed.InvokeMethod));

					// Methods marked as 'other' are not supported in C#
					for (int i = 0; i < ed.OtherMethods.Count; i++) {
						var otherMethod = ed.OtherMethods[i];
						if (otherMethod.DeclaringType != ed.DeclaringType)
							continue;
						astType.Members.Add(CreateMethod(otherMethod));
					}

					continue;
				}

				Debug.Fail("Shouldn't be here");
			}
		}

		EntityDeclaration CreateMethod(MethodDef methodDef)
		{
			MethodDeclaration astMethod = new MethodDeclaration();
			EntityDeclaration returnValue = astMethod;
			astMethod.AddAnnotation(methodDef);
			astMethod.ReturnType = ConvertType(methodDef.ReturnType, stringBuilder, methodDef.Parameters.ReturnParameter.ParamDef);
			bool isRefReturnType = methodDef.ReturnType.RemovePinnedAndModifiers().GetElementType() == ElementType.ByRef && UndoByRefToPointer(astMethod.ReturnType);
			astMethod.NameToken = Identifier.Create(CleanName(methodDef.Name)).WithAnnotation(methodDef);
			astMethod.TypeParameters.AddRange(MakeTypeParameters(methodDef.GenericParameters));
			astMethod.Parameters.AddRange(MakeParameters(Context.MetadataTextColorProvider, methodDef, context.Settings, stringBuilder));
			bool createMethodBody = false;
			// constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
			if (!methodDef.IsVirtual || (methodDef.IsNewSlot && !methodDef.IsPrivate)) astMethod.Constraints.AddRange(MakeConstraints(methodDef.GenericParameters));
			if (!methodDef.DeclaringType.IsInterface) {
				if (IsExplicitInterfaceImplementation(methodDef)) {
					var methDecl = methodDef.Overrides.First().MethodDeclaration;
					astMethod.PrivateImplementationType = ConvertType(methDecl == null ? null : methDecl.DeclaringType, stringBuilder);
					if (IsReadonlyMember(methodDef))
						astMethod.Modifiers |= Modifiers.ReadonlyMember;
				} else {
					astMethod.Modifiers = ConvertModifiers(methodDef, true);
					if (methodDef.IsVirtual == methodDef.IsNewSlot)
						SetNewModifier(astMethod);
				}
				createMethodBody = true;
			} else if (methodDef.IsStatic) {
				// decompile static method in interface
				astMethod.Modifiers = ConvertModifiers(methodDef, true);
				createMethodBody = true;
			} else {
				createMethodBody = true;
			}

			OperatorDeclaration op = null;
			OperatorType? opType = null;
			if (methodDef.IsSpecialName && !methodDef.HasGenericParameters) {
				opType = OperatorDeclaration.GetOperatorType(methodDef.Name);
				if (opType != null)
					op = new OperatorDeclaration();
			}

			if (createMethodBody) {
				if (op != null)
					AddMethodBody(op, out _, methodDef, astMethod.Parameters, false, MethodKind.Method);
				else
					AddMethodBody(astMethod, out returnValue, methodDef, astMethod.Parameters, false, MethodKind.Method);
			}
			else {
				ClearCurrentMethodState();
				ConvertAttributes(astMethod, methodDef);
			}
			if (astMethod.Parameters.Count > 0) {
				if (methodDef.IsDefined(systemRuntimeCompilerServicesString, extensionAttributeString))
					astMethod.Parameters.First().ParameterModifier = ParameterModifier.This;
			}

			// Convert MethodDeclaration to OperatorDeclaration if possible
			if (op != null) {
				op.CopyAnnotationsFrom(astMethod);
				op.ReturnType = astMethod.ReturnType.Detach();
				op.OperatorType = opType.Value;
				op.Modifiers = astMethod.Modifiers;
				astMethod.Parameters.MoveTo(op.Parameters);
				astMethod.Attributes.MoveTo(op.Attributes);
				AddComment(op, methodDef);
				return op;
			}
			if (isRefReturnType)
				astMethod.Modifiers |= Modifiers.Ref;
			if (DnlibExtensions.HasIsReadOnlyAttribute(methodDef.Parameters.ReturnParameter.ParamDef))
				astMethod.Modifiers |= Modifiers.Readonly;
			AddComment(returnValue, methodDef);

			if (methodDef.IsOther)
				returnValue.InsertChildAfter(null, new Comment(" Note: this method is marked as 'other'."), Roles.Comment);
			else if (methodDef.IsFire)
				returnValue.InsertChildAfter(null, new Comment(" Note: this method is marked as 'fire'."), Roles.Comment);

			return returnValue;
		}

		bool IsExplicitInterfaceImplementation(MethodDef methodDef)
		{
			return methodDef != null && methodDef.HasOverrides && methodDef.IsPrivate;
		}

		IEnumerable<TypeParameterDeclaration> MakeTypeParameters(IList<GenericParam> genericParameters) {
			for (int i = 0; i < genericParameters.Count; i++) {
				var gp = genericParameters[i];
				TypeParameterDeclaration tp = new TypeParameterDeclaration();
				tp.AddAnnotation(gp);
				tp.NameToken = Identifier.Create(CleanName(gp.Name)).WithAnnotation(Context.MetadataTextColorProvider.GetColor(gp));
				if (gp.IsContravariant)
					tp.Variance = VarianceModifier.Contravariant;
				else if (gp.IsCovariant)
					tp.Variance = VarianceModifier.Covariant;
				ConvertCustomAttributes(Context.MetadataTextColorProvider, tp, gp, context.Settings, stringBuilder);
				yield return tp;
			}
		}

		IEnumerable<Constraint> MakeConstraints(IList<GenericParam> genericParameters) {
			for (int i = 0; i < genericParameters.Count; i++) {
				var gp = genericParameters[i];
				Constraint c = new Constraint();
				c.TypeParameter = new SimpleType(CleanName(gp.Name)).WithAnnotation(gp);
				c.TypeParameter.IdentifierToken.WithAnnotation(gp);
				// class/struct must be first
				if (gp.HasReferenceTypeConstraint)
					c.BaseTypes.Add(new PrimitiveType("class"));
				if (gp.HasNotNullableValueTypeConstraint)
					c.BaseTypes.Add(new PrimitiveType("struct"));

				for (int j = 0; j < gp.GenericParamConstraints.Count; j++) {
					var constraintType = gp.GenericParamConstraints[j];
					if (constraintType.Constraint == null)
						continue;
					if (gp.HasNotNullableValueTypeConstraint && constraintType.Constraint.Compare(systemString, valueTypeString))
						continue;
					c.BaseTypes.Add(ConvertType(constraintType.Constraint, stringBuilder));
				}

				if (gp.HasDefaultConstructorConstraint && !gp.HasNotNullableValueTypeConstraint)
					c.BaseTypes.Add(new PrimitiveType("new")); // new() must be last
				if (c.BaseTypes.Any())
					yield return c;
			}
		}
		static readonly UTF8String valueTypeString = new UTF8String("ValueType");

		ConstructorDeclaration CreateConstructor(MethodDef methodDef)
		{
			ConstructorDeclaration astMethod = new ConstructorDeclaration();
			astMethod.AddAnnotation(methodDef);
			astMethod.Modifiers = ConvertModifiers(methodDef, false);
			if (methodDef.IsStatic) {
				// don't show visibility for static ctors
				astMethod.Modifiers &= ~Modifiers.VisibilityMask;
			}
			astMethod.NameToken = Identifier.Create(CleanName(methodDef.DeclaringType.Name)).WithAnnotation(methodDef.DeclaringType);
			astMethod.Parameters.AddRange(MakeParameters(Context.MetadataTextColorProvider, methodDef, context.Settings, stringBuilder));
			AddMethodBody(astMethod, out _, methodDef, astMethod.Parameters, false, MethodKind.Method);
			if (methodDef.IsStatic && methodDef.DeclaringType.IsBeforeFieldInit) {
				astMethod.InsertChildAfter(null, new Comment(" Note: this type is marked as 'beforefieldinit'."), Roles.Comment);
			}
			AddComment(astMethod, methodDef);
			return astMethod;
		}

		Modifiers FixUpVisibility(Modifiers m)
		{
			Modifiers v = m & Modifiers.VisibilityMask;
			// If any of the modifiers is public, use that
			if ((v & Modifiers.Public) == Modifiers.Public)
				return Modifiers.Public | (m & ~Modifiers.VisibilityMask);
			// If both modifiers are private, no need to fix anything
			if (v == Modifiers.Private || v == (Modifiers.Private | Modifiers.Protected))
				return m;
			// Otherwise, use the other modifiers (internal and/or protected)
			return m & ~Modifiers.Private;
		}

		EntityDeclaration CreateProperty(PropertyDef propDef)
		{
			PropertyDeclaration astProp = new PropertyDeclaration();
			astProp.AddAnnotation(propDef);
			var accessor = propDef.GetMethod ?? propDef.SetMethod;
			Modifiers getterModifiers = Modifiers.None;
			Modifiers setterModifiers = Modifiers.None;
			if (IsExplicitInterfaceImplementation(accessor)) {
				var methDecl = accessor.Overrides.First().MethodDeclaration;
				astProp.PrivateImplementationType = ConvertType(methDecl == null ? null : methDecl.DeclaringType, stringBuilder);
			} else if (!propDef.DeclaringType.IsInterface) {
				getterModifiers = ConvertModifiers(propDef.GetMethod, true);
				setterModifiers = ConvertModifiers(propDef.SetMethod, true);
				astProp.Modifiers = FixUpVisibility(getterModifiers | setterModifiers);
				try {
					if (accessor != null && accessor.IsVirtual && !accessor.IsNewSlot && (propDef.GetMethod == null || propDef.SetMethod == null)) {
						foreach (var basePropDef in TypesHierarchyHelpers.FindBaseProperties(propDef)) {
							if (basePropDef.GetMethod != null && basePropDef.SetMethod != null) {
								var propVisibilityModifiers = ConvertModifiers(basePropDef.GetMethod, true) | ConvertModifiers(basePropDef.SetMethod, true);
								astProp.Modifiers = FixUpVisibility((astProp.Modifiers & ~Modifiers.VisibilityMask) | (propVisibilityModifiers & Modifiers.VisibilityMask));
								break;
							} else {
								var baseAcc = basePropDef.GetMethod ?? basePropDef.SetMethod;
								if (baseAcc != null && baseAcc.IsNewSlot)
									break;
							}
						}
					}
				} catch (ResolveException) {
					// TODO: add some kind of notification (a comment?) about possible problems with decompiled code due to unresolved references.
				}
			}
			astProp.NameToken = Identifier.Create(CleanName(propDef.Name)).WithAnnotation(propDef);
			astProp.ReturnType = ConvertType(propDef.PropertySig.GetRetType(), stringBuilder, propDef);
			bool isRefReturnType = propDef.PropertySig.RetType.RemovePinnedAndModifiers().GetElementType() == ElementType.ByRef && UndoByRefToPointer(astProp.ReturnType);

			if (propDef.GetMethod != null) {
				astProp.Getter = new Accessor();
				AddMethodBody(astProp.Getter, out _, propDef.GetMethod, null, false, MethodKind.Property);
				astProp.Getter.AddAnnotation(propDef.GetMethod);
				astProp.Getter.Modifiers = getterModifiers & Modifiers.ReadonlyMember;

				if ((getterModifiers & Modifiers.VisibilityMask) != (astProp.Modifiers & Modifiers.VisibilityMask))
					astProp.Getter.Modifiers = getterModifiers & (Modifiers.VisibilityMask | Modifiers.ReadonlyMember);
			}
			if (propDef.SetMethod != null) {
				astProp.Setter = new Accessor();
				AddMethodBody(astProp.Setter, out _, propDef.SetMethod, null, true, MethodKind.Property);
				astProp.Setter.AddAnnotation(propDef.SetMethod);
				astProp.Setter.Modifiers = setterModifiers & Modifiers.ReadonlyMember;
				Parameter lastParam = propDef.SetMethod.Parameters.SkipNonNormal().LastOrDefault();
				if (lastParam != null) {
					ConvertCustomAttributes(Context.MetadataTextColorProvider, astProp.Setter, lastParam.ParamDef, context.Settings, stringBuilder, "param");
				}

				if ((setterModifiers & Modifiers.VisibilityMask) != (astProp.Modifiers & Modifiers.VisibilityMask))
					astProp.Setter.Modifiers = setterModifiers & (Modifiers.VisibilityMask | Modifiers.ReadonlyMember);
			}
			astProp.Modifiers &= ~Modifiers.ReadonlyMember;
			if (astProp.Setter.IsNull && !astProp.Getter.IsNull && (astProp.Getter.Modifiers & Modifiers.ReadonlyMember) != 0) {
				astProp.Getter.Modifiers &= ~Modifiers.ReadonlyMember;
				astProp.Modifiers |= Modifiers.ReadonlyMember;
			}
			else if (!astProp.Setter.IsNull && astProp.Getter.IsNull && (astProp.Setter.Modifiers & Modifiers.ReadonlyMember) != 0) {
				astProp.Setter.Modifiers &= ~Modifiers.ReadonlyMember;
				astProp.Modifiers |= Modifiers.ReadonlyMember;
			}
			else if (!astProp.Setter.IsNull && !astProp.Getter.IsNull && (astProp.Getter.Modifiers & Modifiers.ReadonlyMember) != 0 && (astProp.Setter.Modifiers & Modifiers.ReadonlyMember) != 0) {
				astProp.Getter.Modifiers &= ~Modifiers.ReadonlyMember;
				astProp.Setter.Modifiers &= ~Modifiers.ReadonlyMember;
				astProp.Modifiers |= Modifiers.ReadonlyMember;
			}
			ConvertCustomAttributes(Context.MetadataTextColorProvider, astProp, propDef, context.Settings, stringBuilder);

			EntityDeclaration member = astProp;
			if(propDef.IsIndexer())
				member = ConvertPropertyToIndexer(astProp, propDef);
			if(accessor != null && !accessor.HasOverrides && accessor.DeclaringType != null && !accessor.DeclaringType.IsInterface)
				if (accessor.IsVirtual == accessor.IsNewSlot)
					SetNewModifier(member);
			if (isRefReturnType)
				astProp.Modifiers |= Modifiers.Ref;
			if (DnlibExtensions.HasIsReadOnlyAttribute(accessor.Parameters.ReturnParameter.ParamDef))
				astProp.Modifiers |= Modifiers.Readonly;
			if (propDef.SetMethod != null)
				AddComment(astProp, propDef.SetMethod, "set");
			if (propDef.GetMethod != null)
				AddComment(astProp, propDef.GetMethod, "get");
			AddComment(member, propDef);
			return member;
		}

		IndexerDeclaration ConvertPropertyToIndexer(PropertyDeclaration astProp, PropertyDef propDef)
		{
			var astIndexer = new IndexerDeclaration();
			astIndexer.CopyAnnotationsFrom(astProp);
			astProp.Attributes.MoveTo(astIndexer.Attributes);
			astIndexer.Modifiers = astProp.Modifiers;
			astIndexer.PrivateImplementationType = astProp.PrivateImplementationType.Detach();
			astIndexer.ReturnType = astProp.ReturnType.Detach();
			astIndexer.Getter = astProp.Getter.Detach();
			astIndexer.Setter = astProp.Setter.Detach();
			astIndexer.Parameters.AddRange(MakeParameters(Context.MetadataTextColorProvider, propDef.GetParameters().ToList(), context.Settings, stringBuilder));
			return astIndexer;
		}

		EntityDeclaration CreateEvent(EventDef eventDef)
		{
			if ((eventDef.AddMethod != null && eventDef.AddMethod.IsAbstract) || (eventDef.AddMethod?.Body == null && eventDef.RemoveMethod?.Body == null && eventDef.InvokeMethod?.Body == null)) {
				EventDeclaration astEvent = new EventDeclaration();
				ConvertCustomAttributes(Context.MetadataTextColorProvider, astEvent, eventDef, context.Settings, stringBuilder);
				astEvent.AddAnnotation(eventDef);
				astEvent.Variables.Add(new VariableInitializer(eventDef, CleanName(eventDef.Name)));
				astEvent.ReturnType = ConvertType(eventDef.EventType, stringBuilder, eventDef);
				if (!eventDef.DeclaringType.IsInterface)
					astEvent.Modifiers = ConvertModifiers(eventDef.AddMethod, true);
				if (eventDef.RemoveMethod != null)
					AddComment(astEvent, eventDef.RemoveMethod, "remove");
				if (eventDef.AddMethod != null)
					AddComment(astEvent, eventDef.AddMethod, "add");
				AddComment(astEvent, eventDef);
				return astEvent;
			} else {
				CustomEventDeclaration astEvent = new CustomEventDeclaration();
				ConvertCustomAttributes(Context.MetadataTextColorProvider, astEvent, eventDef, context.Settings, stringBuilder);
				astEvent.AddAnnotation(eventDef);
				astEvent.NameToken = Identifier.Create(CleanName(eventDef.Name)).WithAnnotation(eventDef);
				astEvent.ReturnType = ConvertType(eventDef.EventType, stringBuilder, eventDef);
				if (eventDef.AddMethod == null || !IsExplicitInterfaceImplementation(eventDef.AddMethod))
					astEvent.Modifiers = ConvertModifiers(eventDef.AddMethod, true);
				else {
					var methDecl = eventDef.AddMethod.Overrides.First().MethodDeclaration;
					astEvent.PrivateImplementationType = ConvertType(methDecl == null ? null : methDecl.DeclaringType, stringBuilder);
				}

				if (eventDef.AddMethod != null) {
					astEvent.AddAccessor = new Accessor().WithAnnotation(eventDef.AddMethod);
					AddMethodBody(astEvent.AddAccessor, out _, eventDef.AddMethod, null, true, MethodKind.Event);
				}
				if (eventDef.RemoveMethod != null) {
					astEvent.RemoveAccessor = new Accessor().WithAnnotation(eventDef.RemoveMethod);
					AddMethodBody(astEvent.RemoveAccessor, out _, eventDef.RemoveMethod, null, true, MethodKind.Event);
				}
				MethodDef accessor = eventDef.AddMethod ?? eventDef.RemoveMethod;
				if (accessor != null && accessor.IsVirtual == accessor.IsNewSlot) {
					SetNewModifier(astEvent);
				}
				if (eventDef.RemoveMethod != null)
					AddComment(astEvent, eventDef.RemoveMethod, "remove");
				if (eventDef.AddMethod != null)
					AddComment(astEvent, eventDef.AddMethod, "add");
				AddComment(astEvent, eventDef);
				return astEvent;
			}
		}

		static MethodBaseSig GetMethodBaseSig(ITypeDefOrRef type, MethodBaseSig msig, IList<TypeSig> methodGenArgs = null)
		{
			IList<TypeSig> typeGenArgs = null;
			var ts = type as TypeSpec;
			if (ts != null) {
				var genSig = ts.TypeSig.ToGenericInstSig();
				if (genSig != null)
					typeGenArgs = genSig.GenericArguments;
			}
			if (typeGenArgs == null && methodGenArgs == null)
				return msig;
			return GenericArgumentResolver.Resolve(msig, typeGenArgs, methodGenArgs);
		}

		void ClearCurrentMethodState() {
			context.CurrentMethodIsAsync = false;
			context.CurrentMethodIsYieldReturn = false;
		}

		void AddMethodBody(EntityDeclaration methodNode, out EntityDeclaration updatedNode, MethodDef method, IEnumerable<ParameterDeclaration> parameters, bool valueParameterIsKeyword, MethodKind methodKind) {
			updatedNode = methodNode;
			ClearCurrentMethodState();
			if (method.Body == null) {
				ConvertAttributes(methodNode, method);
				return;
			}

			BlockStatement bs;
			MethodDebugInfoBuilder builder3;
			var bodyKind = GetDecompiledBodyKind?.Invoke(this, method) ?? DecompiledBodyKind.Full;
			// In order for auto events to be optimized from custom to auto events, they must have bodies.
			// DecompileTypeMethodsTransform has a fix to remove the hidden custom events' bodies.
			if (bodyKind == DecompiledBodyKind.Empty && methodKind == MethodKind.Event)
				bodyKind = DecompiledBodyKind.Full;
			switch (bodyKind) {
			case DecompiledBodyKind.Full:
				try {
					if (context.AsyncMethodBodyDecompilation) {
						parameters = parameters?.ToArray();
						var context = this.context.Clone();
						var bodyTask = Task.Run(() => {
							if (context.CancellationToken.IsCancellationRequested)
								return default(AsyncMethodBodyResult);
							var asyncState = GetAsyncMethodBodyDecompilationState();
							var stringBuilder = asyncState.StringBuilder;
							var autoPropertyProvider = new AutoPropertyProvider();
							BlockStatement body;
							MethodDebugInfoBuilder builder2;
							try {
								body = AstMethodBodyBuilder.CreateMethodBody(method, context, autoPropertyProvider, parameters, valueParameterIsKeyword, stringBuilder, out builder2);
							}
							catch (OperationCanceledException) {
								throw;
							}
							catch (Exception ex) {
								CreateBadMethod(context, method, ex, out body, out builder2);
							}
							Return(asyncState);
							return new AsyncMethodBodyResult(methodNode, method, body, builder2, context.variableMap, context.CurrentMethodIsAsync, context.CurrentMethodIsYieldReturn);
						}, context.CancellationToken);
						methodBodyTasks.Add(bodyTask);
					}
					else {
						var body = AstMethodBodyBuilder.CreateMethodBody(method, context, AutoPropertyProvider, parameters, valueParameterIsKeyword, stringBuilder, out var builder);
						if (context.CurrentMethodIsAsync)
							methodNode.Modifiers |= Modifiers.Async;
						methodNode.SetChildByRole(Roles.Body, body);
						methodNode.AddAnnotation(builder);
						methodNode.AddAnnotation(context.variableMap);
						ConvertAttributes(methodNode, method);
					}
					return;
				}
				catch (OperationCanceledException) {
					throw;
				}
				catch (Exception ex) {
					CreateBadMethod(context, method, ex, out bs, out builder3);
				}
				methodNode.SetChildByRole(Roles.Body, bs);
				methodNode.AddAnnotation(builder3);
				ConvertAttributes(methodNode, method);
				return;

			case DecompiledBodyKind.Empty:
				bs = new BlockStatement();
				if (method.IsInstanceConstructor) {
					var baseCtor = GetBaseConstructorForEmptyBody(method);
					if (baseCtor != null) {
						var methodSig = GetMethodBaseSig(method.DeclaringType.BaseType, baseCtor.MethodSig);
						var args = new List<Expression>(methodSig.Params.Count);
						for (int i = 0; i < methodSig.Params.Count; i++)
							args.Add(new DefaultValueExpression(ConvertType(methodSig.Params[i].RemovePinnedAndModifiers(), stringBuilder)));
						var stmt = new ExpressionStatement(new InvocationExpression(new MemberReferenceExpression(new BaseReferenceExpression(), method.Name), args));
						bs.Statements.Add(stmt);
					}
					if (method.DeclaringType.IsValueType && !method.DeclaringType.IsEnum) {
						for (int i = 0; i < method.DeclaringType.Fields.Count; i++) {
							var field = method.DeclaringType.Fields[i];
							if (field.IsStatic)
								continue;
							var defVal = new DefaultValueExpression(ConvertType(field.FieldType.RemovePinnedAndModifiers(), stringBuilder));
							var stmt = new ExpressionStatement(new AssignmentExpression(new MemberReferenceExpression(new ThisReferenceExpression(), field.Name), defVal));
							bs.Statements.Add(stmt);
						}
					}
				}
				if (parameters != null) {
					foreach (var p in parameters) {
						if (p.ParameterModifier != ParameterModifier.Out)
							continue;
						var parameter = p.Annotation<Parameter>();
						var defVal = new DefaultValueExpression(ConvertType(parameter.Type.RemovePinnedAndModifiers().Next, stringBuilder));
						var stmt = new ExpressionStatement(new AssignmentExpression(new IdentifierExpression(p.Name), defVal));
						bs.Statements.Add(stmt);
					}
				}
				if (method.MethodSig.GetRetType().RemovePinnedAndModifiers().GetElementType() != ElementType.Void) {
					if (method.MethodSig.GetRetType().RemovePinnedAndModifiers().GetElementType() == ElementType.ByRef) {
						var @throw = new ThrowStatement(new NullReferenceExpression());
						bs.Statements.Add(@throw);
					}
					else {
						var ret = new ReturnStatement(new DefaultValueExpression(ConvertType(method.MethodSig.GetRetType().RemovePinnedAndModifiers(), stringBuilder)));
						bs.Statements.Add(ret);
					}
				}
				if (method.IsVirtual && method.MethodSig.GetParamCount() == 0 && method.MethodSig.GetRetType().GetElementType() == ElementType.Void && method.Name == name_Finalize) {
					var dd = new DestructorDeclaration();
					dd.AddAnnotation(methodNode.Annotation<MethodDef>());
					methodNode.Attributes.MoveTo(dd.Attributes);
					dd.Modifiers = methodNode.Modifiers & ~(Modifiers.Protected | Modifiers.Override);
					dd.NameToken = Identifier.Create(AstBuilder.CleanName(context.CurrentType.Name));
					updatedNode = dd;
					methodNode = dd;
				}
				methodNode.SetChildByRole(Roles.Body, bs);
				ConvertAttributes(methodNode, method);
				return;

			case DecompiledBodyKind.None:
				ConvertAttributes(methodNode, method);
				return;

			default:
				throw new InvalidOperationException();
			}
		}
		static readonly UTF8String name_Finalize = new UTF8String("Finalize");

		public static void CreateBadMethod(DecompilerContext context, MethodDef method, Exception ex, out BlockStatement bs, out MethodDebugInfoBuilder builder) {
			var msg = string.Format("{0}An exception occurred when decompiling this method ({1:X8}){0}{0}{2}{0}",
					Environment.NewLine, method.MDToken.ToUInt32(), ex.ToString());

			bs = new BlockStatement();
			var emptyStmt = new EmptyStatement();
			if (method.Body != null)
				emptyStmt.AddAnnotation(new List<ILSpan>(1) { new ILSpan(0, (uint)method.Body.GetCodeSize()) });
			bs.Statements.Add(emptyStmt);
			bs.InsertChildAfter(null, new Comment(msg, CommentType.MultiLine), Roles.Comment);
			builder = new MethodDebugInfoBuilder(context.SettingsVersion, StateMachineKind.None, method, null, method.Body.Variables.Select(a => new SourceLocal(a, CreateLocalName(a), a.Type, SourceVariableFlags.None)).ToArray(), null, null);
		}

		static string CreateLocalName(Local local) {
			var name = local.Name;
			if (!string.IsNullOrEmpty(name))
				return name;
			return "V_" + local.Index.ToString();
		}

		static MethodDef GetBaseConstructorForEmptyBody(MethodDef method) {
			var baseType = method.DeclaringType.BaseType.ResolveTypeDef();
			if (baseType == null)
				return null;
			return GetAccessibleConstructorForEmptyBody(baseType, method.DeclaringType);
		}

		static MethodDef GetAccessibleConstructorForEmptyBody(TypeDef baseType, TypeDef type) {
			var list = new List<MethodDef>(baseType.FindConstructors());
			if (list.Count == 0)
				return null;
			bool isAssem = baseType.Module.Assembly == type.Module.Assembly || type.Module.Assembly.IsFriendAssemblyOf(baseType.Module.Assembly);
			list.Sort((a, b) => {
				int c = GetAccessForEmptyBody(a, isAssem) - GetAccessForEmptyBody(b, isAssem);
				if (c != 0)
					return c;
				// Don't prefer ref/out ctors
				c = GetParamTypeOrderForEmtpyBody(a) - GetParamTypeOrderForEmtpyBody(b);
				if (c != 0)
					return c;
				return a.Parameters.Count - b.Parameters.Count;
			});
			return list[0];
		}

		static int GetParamTypeOrderForEmtpyBody(MethodDef m) =>
			m.MethodSig.Params.Any(a => a.RemovePinnedAndModifiers() is ByRefSig) ? 1 : 0;

		static int GetAccessForEmptyBody(MethodDef m, bool isAssem) {
			switch (m.Access) {
			case MethodAttributes.Public:			return 0;
			case MethodAttributes.FamORAssem:		return 0;
			case MethodAttributes.Family:			return 0;
			case MethodAttributes.Assembly:			return isAssem ? 0 : 1;
			case MethodAttributes.FamANDAssem:		return isAssem ? 0 : 1;
			case MethodAttributes.Private:			return 2;
			case MethodAttributes.PrivateScope:		return 3;
			default:								return 3;
			}
		}

		static bool HasConstant(IHasConstant hc, out CustomAttribute constantAttribute) {
			constantAttribute = null;
			if (hc.Constant != null)
				return true;
			for (int i = 0; i < hc.CustomAttributes.Count; i++) {
				var ca = hc.CustomAttributes[i];
				var type = ca.AttributeType;
				while (type != null) {
					var fullName = type.FullName;
					if (fullName == "System.Runtime.CompilerServices.CustomConstantAttribute" ||
						fullName == "System.Runtime.CompilerServices.DecimalConstantAttribute") {
						constantAttribute = ca;
						return true;
					}
					type = type.GetBaseType();
				}
			}
			return false;
		}

		static bool TryGetConstant(IHasConstant hc, out object constant) {
			if (!HasConstant(hc, out var constantAttribute)) {
				constant = null;
				return false;
			}

			if (hc.Constant != null) {
				constant = hc.Constant.Value;
				return true;
			}

			if (constantAttribute != null) {
				if (constantAttribute.TypeFullName == "System.Runtime.CompilerServices.DecimalConstantAttribute") {
					if (TryGetDecimalConstantAttributeValue(constantAttribute, out var decimalValue)) {
						constant = decimalValue;
						return true;
					}
				}
			}

			constant = null;
			return false;
		}

		static bool TryGetDecimalConstantAttributeValue(CustomAttribute ca, out decimal value) {
			value = 0;
			if (ca.ConstructorArguments.Count != 5)
				return false;
			if (!(ca.ConstructorArguments[0].Value is byte scale))
				return false;
			if (!(ca.ConstructorArguments[1].Value is byte sign))
				return false;
			int hi, mid, low;
			if (ca.ConstructorArguments[2].Value is int) {
				if (!(ca.ConstructorArguments[2].Value is int))
					return false;
				if (!(ca.ConstructorArguments[3].Value is int))
					return false;
				if (!(ca.ConstructorArguments[4].Value is int))
					return false;
				hi = (int)ca.ConstructorArguments[2].Value;
				mid = (int)ca.ConstructorArguments[3].Value;
				low = (int)ca.ConstructorArguments[4].Value;
			}
			else if (ca.ConstructorArguments[2].Value is uint) {
				if (!(ca.ConstructorArguments[2].Value is uint))
					return false;
				if (!(ca.ConstructorArguments[3].Value is uint))
					return false;
				if (!(ca.ConstructorArguments[4].Value is uint))
					return false;
				hi = (int)(uint)ca.ConstructorArguments[2].Value;
				mid = (int)(uint)ca.ConstructorArguments[3].Value;
				low = (int)(uint)ca.ConstructorArguments[4].Value;
			}
			else
				return false;
			try {
				value = new decimal(low, mid, hi, sign > 0, scale);
				return true;
			}
			catch (ArgumentOutOfRangeException) {
				return false;
			}
		}

		FieldDeclaration CreateField(FieldDef fieldDef)
		{
			FieldDeclaration astField = new FieldDeclaration();
			astField.AddAnnotation(fieldDef);
			VariableInitializer initializer = new VariableInitializer(fieldDef, CleanName(fieldDef.Name));
			astField.AddChild(initializer, Roles.Variable);
			astField.ReturnType = ConvertType(fieldDef.FieldType, stringBuilder, fieldDef);
			astField.Modifiers = ConvertModifiers(fieldDef);
			if (TryGetConstant(fieldDef, out var constant)) {
				initializer.Initializer = CreateExpressionForConstant(constant, fieldDef.FieldType, stringBuilder, fieldDef.DeclaringType.IsEnum);
			}
			ConvertAttributes(Context.MetadataTextColorProvider, astField, fieldDef, context.Settings, stringBuilder);
			SetNewModifier(astField);
			AddComment(astField, fieldDef);
			return astField;
		}

		static object ConvertConstant(TypeSig type, object constant)
		{
			if (type == null || constant == null)
				return constant;
			TypeCode c = Type.GetTypeCode(constant.GetType());
			if (c < TypeCode.Char || c > TypeCode.Double)
				return constant;

			c = ToTypeCode(type);
			if (c >= TypeCode.Char && c <= TypeCode.Double)
				return CSharpPrimitiveCast.Cast(c, constant, false);
			return constant;
		}

		static TypeCode ToTypeCode(TypeSig type)
		{
			switch (type.GetElementType()) {
			case ElementType.Boolean: return TypeCode.Boolean;
			case ElementType.Char: return TypeCode.Char;
			case ElementType.I1: return TypeCode.SByte;
			case ElementType.U1: return TypeCode.Byte;
			case ElementType.I2: return TypeCode.Int16;
			case ElementType.U2: return TypeCode.UInt16;
			case ElementType.I4: return TypeCode.Int32;
			case ElementType.U4: return TypeCode.UInt32;
			case ElementType.I8: return TypeCode.Int64;
			case ElementType.U8: return TypeCode.UInt64;
			case ElementType.R4: return TypeCode.Single;
			case ElementType.R8: return TypeCode.Double;
			case ElementType.String: return TypeCode.String;
			case ElementType.Object: return TypeCode.Object;
			}
			return TypeCode.Empty;
		}

		static Expression CreateExpressionForConstant(object constant, TypeSig type, StringBuilder sb, bool isEnumMemberDeclaration = false)
		{
			constant = ConvertConstant(type, constant);
			if (constant == null) {
				if (!DnlibExtensions.IsValueType(type) && !(type is GenericSig))
					return new NullReferenceExpression();
				var gis = type as GenericInstSig;
				if (gis == null || !gis.GenericType.IsSystemNullable())
					return new DefaultValueExpression(ConvertType(type, sb));
				return new NullReferenceExpression();
			} else {
				TypeCode c = Type.GetTypeCode(constant.GetType());
				if (c >= TypeCode.SByte && c <= TypeCode.UInt64 && !isEnumMemberDeclaration) {
					return MakePrimitive((long)CSharpPrimitiveCast.Cast(TypeCode.Int64, constant, false), type.ToTypeDefOrRef(), sb);
				} else {
					return new PrimitiveExpression(constant);
				}
			}
		}

		public static IEnumerable<ParameterDeclaration> MakeParameters(MetadataTextColorProvider metadataTextColorProvider, MethodDef method, DecompilerSettings settings, StringBuilder sb, bool isLambda = false)
		{
			var parameters = MakeParameters(metadataTextColorProvider, method.Parameters, settings, sb, isLambda);
			if (method.CallingConvention == dnlib.DotNet.CallingConvention.VarArg ||
				method.CallingConvention == dnlib.DotNet.CallingConvention.NativeVarArg) {
				var pd = new ParameterDeclaration {
					Type = new PrimitiveType("__arglist"),
					NameToken = Identifier.Create("").WithAnnotation(BoxedTextColor.Parameter)
				};
				return parameters.Concat(new[] { pd });
			} else {
				return parameters;
			}
		}

		internal static bool UndoByRefToPointer(AstType type) {
			var ct = type as ComposedType;
			if (ct != null && ct.PointerRank > 0) {
				ct.PointerRank--;
				return true;
			}
			return false;
		}

		static IEnumerable<ParameterDeclaration> MakeParameters(MetadataTextColorProvider metadataTextColorProvider, IList<Parameter> paramCol, DecompilerSettings settings, StringBuilder sb, bool isLambda = false) {
			for (int i = 0; i < paramCol.Count; i++) {
				var paramDef = paramCol[i];
				if (paramDef.IsHiddenThisParameter)
					continue;

				ParameterDeclaration astParam = new ParameterDeclaration();
				astParam.AddAnnotation(paramDef);
				var type = paramDef.Type.RemovePinnedAndModifiers();
				if (!(isLambda && type.ContainsAnonymousType()))
					astParam.Type = ConvertType(type, sb, paramDef.ParamDef);
				astParam.NameToken = Identifier.Create(paramDef.Name).WithAnnotation(paramDef);

				if (type is ByRefSig) {
					var pd = paramDef.ParamDef;
					if (pd == null)
						astParam.ParameterModifier = ParameterModifier.Ref;
					else if (!pd.IsIn && pd.IsOut)
						astParam.ParameterModifier = ParameterModifier.Out;
					else if (DnlibExtensions.HasIsReadOnlyAttribute(pd))
						astParam.ParameterModifier = ParameterModifier.In;
					else
						astParam.ParameterModifier = ParameterModifier.Ref;
					UndoByRefToPointer(astParam.Type);
				}

				if (paramDef.HasParamDef) {
					if (paramDef.ParamDef.IsDefined(systemString, paramArrayAttributeString))
						astParam.ParameterModifier = ParameterModifier.Params;
				}
				if (paramDef.HasParamDef && paramDef.ParamDef.IsOptional && TryGetConstant(paramDef.ParamDef, out var constant)) {
					astParam.DefaultExpression = CreateExpressionForConstant(constant, type, sb);
				}

				ConvertCustomAttributes(metadataTextColorProvider, astParam, paramDef.ParamDef, settings, sb);
				yield return astParam;
			}
		}
		static readonly UTF8String paramArrayAttributeString = new UTF8String("ParamArrayAttribute");

		#region ConvertAttributes
		void ConvertAttributes(EntityDeclaration attributedNode, TypeDef typeDef)
		{
			ConvertCustomAttributes(Context.MetadataTextColorProvider, attributedNode, typeDef, context.Settings, stringBuilder);
		}

		void ConvertAttributes(EntityDeclaration attributedNode, MethodDef methodDef)
		{
			ConvertAttributes(attributedNode, methodDef, context.CurrentMethodIsAsync, context.CurrentMethodIsYieldReturn);
		}

		void ConvertAttributes(EntityDeclaration attributedNode, MethodDef methodDef, bool methodIsAsync, bool methodIsIterator)
		{
			var options = ConvertCustomAttributesFlags.None;
			if (methodIsAsync)
				options |= ConvertCustomAttributesFlags.IsAsync;
			if (methodIsIterator)
				options |= ConvertCustomAttributesFlags.IsYieldReturn;
			ConvertCustomAttributes(Context.MetadataTextColorProvider, attributedNode, methodDef, context.Settings, stringBuilder, options: options);
			ConvertAttributes(attributedNode, methodDef.Parameters.ReturnParameter);
		}

		void ConvertAttributes(EntityDeclaration attributedNode, Parameter methodReturnType)
		{
			ConvertCustomAttributes(Context.MetadataTextColorProvider, attributedNode, methodReturnType.ParamDef, context.Settings, stringBuilder, "return");
		}

		internal static void ConvertAttributes(MetadataTextColorProvider metadataTextColorProvider, EntityDeclaration attributedNode, FieldDef fieldDef, DecompilerSettings settings, StringBuilder sb, string attributeTarget = null)
		{
			ConvertCustomAttributes(metadataTextColorProvider, attributedNode, fieldDef, settings, sb, attributeTarget);
		}

		static IEnumerable<CustomAttribute> SortCustomAttributes(IHasCustomAttribute customAttributeProvider, bool sort, StringBuilder sb)
		{
			var cas = customAttributeProvider.GetCustomAttributes().ToList();
			if (customAttributeProvider is AssemblyDef) {
				// Always sort these pseudo custom attributes
				if (cas.Any(IsTypeForwardedToAttribute)) {
					var newCas = new List<CustomAttribute>(cas.Where(a => !IsTypeForwardedToAttribute(a)));
					var tft = new List<CustomAttribute>(cas.Where(IsTypeForwardedToAttribute));
					tft.Sort(CompareTypeForwardedToAttributes);
					newCas.AddRange(tft);
					cas = newCas;
				}
			}
			if (!sort)
				return cas;
			return cas.OrderBy(a => { sb.Clear(); return FullNameFactory.FullName(a.AttributeType, false, null, sb); });
		}

		static bool IsTypeForwardedToAttribute(CustomAttribute ca) => IsTypeForwardedToAttribute(ca, out _);
		static bool IsTypeForwardedToAttribute(CustomAttribute ca, out ITypeDefOrRef type) {
			type = null;
			if (ca.TypeFullName != "System.Runtime.CompilerServices.TypeForwardedToAttribute")
				return false;
			if (ca.ConstructorArguments.Count != 1)
				return false;
			return ca.ConstructorArguments[0].Value is TypeDefOrRefSig tdrs && !((type = tdrs.TypeDefOrRef) is null);
		}

		static int CompareTypeForwardedToAttributes(CustomAttribute x, CustomAttribute y) {
			Debug.Assert(IsTypeForwardedToAttribute(x));
			Debug.Assert(IsTypeForwardedToAttribute(y));
			return CompareExportedTypes(((TypeDefOrRefSig)x.ConstructorArguments[0].Value).TypeDefOrRef, ((TypeDefOrRefSig)y.ConstructorArguments[0].Value).TypeDefOrRef);
		}

		static int CompareExportedTypes(ITypeDefOrRef x, ITypeDefOrRef y) {
			var xasm = x.DefinitionAssembly;
			var yasm = y.DefinitionAssembly;
			int c = StringComparer.OrdinalIgnoreCase.Compare(xasm.FullNameToken, yasm.FullNameToken);
			if (c != 0) return c;
			c = StringComparer.OrdinalIgnoreCase.Compare(GetName(x), GetName(y));
			if (c != 0) return c;
			return x.MDToken.CompareTo(y.MDToken);
		}

		static string GetName(ITypeDefOrRef type) {
			if (!(type.DeclaringType is ExportedType declType))
				return type.Name;
			if (!(declType.DeclaringType is ExportedType declType2))
				return declType.Name + "." + type.Name;
			var declTypes = new List<ITypeDefOrRef>();
			var t = type;
			while (!(t is null)) {
				declTypes.Add(t);
				t = t.DeclaringType;
			}
			var sb = new StringBuilder();
			for (int i = declTypes.Count - 1; i >= 0; i--) {
				if (i != declTypes.Count - 1)
					sb.Append('.');
				sb.Append(declTypes[i].Name.String);
			}
			return sb.ToString();
		}

		[Flags]
		enum ConvertCustomAttributesFlags {
			None = 0,
			IsAsync = 1,
			IsYieldReturn = 2,
		}
		static readonly UTF8String extensionAttributeString = new UTF8String("ExtensionAttribute");
		static readonly UTF8String systemDiagnosticsString = new UTF8String("System.Diagnostics");
		static readonly UTF8String debuggerStepThroughAttributeString = new UTF8String("DebuggerStepThroughAttribute");
		static readonly UTF8String debuggerHiddenAttributeString = new UTF8String("DebuggerHiddenAttribute");
		static readonly UTF8String asyncStateMachineAttributeString = new UTF8String("AsyncStateMachineAttribute");
		static readonly UTF8String iteratorStateMachineAttributeString = new UTF8String("IteratorStateMachineAttribute");
		static readonly UTF8String isReadOnlyAttributeString = new UTF8String("IsReadOnlyAttribute");
		static readonly UTF8String isByRefLikeAttributeString = new UTF8String("IsByRefLikeAttribute");
		static readonly UTF8String obsoleteAttributeString = new UTF8String("ObsoleteAttribute");
		static void ConvertCustomAttributes(MetadataTextColorProvider metadataTextColorProvider, AstNode attributedNode, IHasCustomAttribute customAttributeProvider, DecompilerSettings settings, StringBuilder sb, string attributeTarget = null, ConvertCustomAttributesFlags options = ConvertCustomAttributesFlags.None)
		{
			if (customAttributeProvider != null) {
				EntityDeclaration entityDecl = attributedNode as EntityDeclaration;
				var attributes = new List<ICSharpCode.NRefactory.CSharp.Attribute>();
				bool isType = attributedNode is TypeDeclaration;
				bool isParameter = attributedNode is ParameterDeclaration;
				bool isMethod = attributedNode is MethodDeclaration || attributedNode is Accessor;
				bool isProperty = attributedNode is PropertyDeclaration;
				bool onePerLine = attributeTarget == "module" || attributeTarget == "assembly" || isParameter ||
					// Params ignore the option
					(settings.OneCustomAttributePerLine && entityDecl != null);
				bool isAsync = (options & ConvertCustomAttributesFlags.IsAsync) != 0;
				bool isYieldReturn = (options & ConvertCustomAttributesFlags.IsYieldReturn) != 0;
				bool isReturnTarget = attributeTarget == "return";
				bool removeObsoleteAttr = false;
				foreach (var customAttribute in SortCustomAttributes(customAttributeProvider, settings.SortCustomAttributes, sb)) {
					var attributeType = customAttribute.AttributeType;
					if (attributeType == null)
						continue;
					if (attributeType.Compare(systemRuntimeCompilerServicesString, extensionAttributeString)) {
						// don't show the ExtensionAttribute (it's converted to the 'this' modifier)
						continue;
					}
					if (attributeType.Compare(systemString, paramArrayAttributeString)) {
						// don't show the ParamArrayAttribute (it's converted to the 'params' modifier)
						continue;
					}
					if ((isYieldReturn || isAsync) && attributeType.Compare(systemDiagnosticsString, debuggerStepThroughAttributeString))
						continue;
					if ((isYieldReturn || isAsync) && attributeType.Compare(systemDiagnosticsString, debuggerHiddenAttributeString))
						continue;
					if (isParameter && (attributeType.Compare(systemRuntimeCompilerServicesString, dynamicAttributeString) || attributeType.Compare(systemRuntimeCompilerServicesString, isReadOnlyAttributeString)))
						continue;
					if (isMethod && (attributeType.Compare(systemRuntimeCompilerServicesString, iteratorStateMachineAttributeString) || attributeType.Compare(systemRuntimeCompilerServicesString, asyncStateMachineAttributeString)))
						continue;
					if (isType) {
						if (attributeType.Compare(systemRuntimeCompilerServicesString, isReadOnlyAttributeString))
							continue;
						if (attributeType.Compare(systemRuntimeCompilerServicesString, isByRefLikeAttributeString)) {
							removeObsoleteAttr = true;
							continue;
						}
						if (removeObsoleteAttr && attributeType.Compare(systemString, obsoleteAttributeString))
							continue;
					}
					if (isProperty && attributeType.Compare(systemRuntimeCompilerServicesString, isReadOnlyAttributeString))
						continue;
					if (isReturnTarget && attributeType.Compare(systemRuntimeCompilerServicesString, isReadOnlyAttributeString))
						continue;
					if (isMethod && attributeType.Compare(systemRuntimeCompilerServicesString, isReadOnlyAttributeString))
						continue;

					var attribute = new ICSharpCode.NRefactory.CSharp.Attribute();
					attribute.AddAnnotation(customAttribute);
					attribute.Type = ConvertType(attributeType, sb);
					attributes.Add(attribute);

					SimpleType st = attribute.Type as SimpleType;
					if (st != null && st.Identifier.EndsWith("Attribute", StringComparison.Ordinal)) {
						var id = Identifier.Create(st.Identifier.Substring(0, st.Identifier.Length - "Attribute".Length));
						id.AddAnnotationsFrom(st.IdentifierToken);
						st.IdentifierToken = id;
					}

					if (customAttribute.IsRawBlob) {
						var emptyExpression = new ErrorExpression();
						emptyExpression.AddChild(new Comment("Failed to decode CustomAttribute blob!", CommentType.MultiLine), Roles.Comment);
						attribute.Arguments.Add(emptyExpression);
					}
					else {
						if (customAttribute.HasConstructorArguments) {
							for (int i = 0; i < customAttribute.ConstructorArguments.Count; i++)
								attribute.Arguments.Add(ConvertArgumentValue(customAttribute.ConstructorArguments[i], sb));
						}
						if (customAttribute.HasNamedArguments) {
							TypeDef resolvedAttributeType = attributeType.ResolveTypeDef();
							foreach (var propertyNamedArg in customAttribute.Properties) {
								var propertyReference = GetProperty(resolvedAttributeType, propertyNamedArg.Name);
								var propertyName = IdentifierExpression.Create(propertyNamedArg.Name, metadataTextColorProvider.GetColor((object)propertyReference ?? BoxedTextColor.InstanceProperty), true).WithAnnotation(propertyReference);
								var argumentValue = ConvertArgumentValue(propertyNamedArg.Argument, sb);
								attribute.Arguments.Add(new AssignmentExpression(propertyName, argumentValue));
							}

							foreach (var fieldNamedArg in customAttribute.Fields) {
								var fieldReference = GetField(resolvedAttributeType, fieldNamedArg.Name);
								var fieldName = IdentifierExpression.Create(fieldNamedArg.Name, metadataTextColorProvider.GetColor((object)fieldReference ?? BoxedTextColor.InstanceField), true).WithAnnotation(fieldReference);
								var argumentValue = ConvertArgumentValue(fieldNamedArg.Argument, sb);
								attribute.Arguments.Add(new AssignmentExpression(fieldName, argumentValue));
							}
						}
					}
				}

				if (onePerLine) {
					bool isAssembly = attributeTarget == "assembly";
					IAssembly lastAssembly = null;

					// use separate section for each attribute
					for (int i = 0; i < attributes.Count; i++) {
						var attribute = attributes[i];
						if (isAssembly && attribute.Annotation<CustomAttribute>() is CustomAttribute ca && IsTypeForwardedToAttribute(ca, out var exportedType)) {
							if (lastAssembly == null || !AssemblyNameComparer.CompareAll.Equals(exportedType.DefinitionAssembly, lastAssembly)) {
								lastAssembly = exportedType.DefinitionAssembly;
								var cmt = new Comment(" " + lastAssembly.FullNameToken);
								attributedNode.AddChild(cmt, Roles.Comment);
							}
						}

						var section = new AttributeSection();
						section.AttributeTarget = attributeTarget;
						section.Attributes.Add(attribute);
						attributedNode.AddChild(section, EntityDeclaration.AttributeRole);
					}
				} else if (attributes.Count > 0) {
					// use single section for all attributes
					var section = new AttributeSection();
					section.AttributeTarget = attributeTarget;
					section.Attributes.AddRange(attributes);
					attributedNode.AddChild(section, EntityDeclaration.AttributeRole);
				}
			}
		}

		static PropertyDef GetProperty(TypeDef type, UTF8String name)
		{
			while (type != null) {
				for (int i = 0; i < type.Properties.Count; i++) {
					var pd = type.Properties[i];
					if (pd.Name == name)
						return pd;
				}
				type = type.BaseType.ResolveTypeDef();
			}
			return null;
		}

		static FieldDef GetField(TypeDef type, UTF8String name)
		{
			while (type != null) {
				for (int i = 0; i < type.Fields.Count; i++) {
					var fd = type.Fields[i];
					if (fd.Name == name)
						return fd;
				}
				type = type.BaseType.ResolveTypeDef();
			}
			return null;
		}

		private static Expression ConvertArgumentValue(CAArgument argument, StringBuilder sb)
		{
			if (argument.Value is IList<CAArgument> argumentValue) {
				ArrayInitializerExpression arrayInit = new ArrayInitializerExpression();
				for (int i = 0; i < argumentValue.Count; i++)
					arrayInit.Elements.Add(ConvertArgumentValue(argumentValue[i], sb));
				ArraySigBase arrayType = argument.Type as ArraySigBase;
				return new ArrayCreateExpression {
					Type = ConvertType(arrayType != null ? arrayType.Next : argument.Type, sb),
					AdditionalArraySpecifiers = { new ArraySpecifier() },
					Initializer = arrayInit
				};
			}
			if (argument.Value is CAArgument value) {
				// occurs with boxed arguments
				return ConvertArgumentValue(value, sb);
			}
			var type = argument.Type.Resolve();
			if (type != null && type.IsEnum && argument.Value != null) {
				try {
					object argVal;
					if (argument.Value is UTF8String utf8String2) {
						try {
							argVal = Convert.ToInt64(utf8String2.String);
						}
						catch (OverflowException) {
							argVal = Convert.ToUInt64(utf8String2.String);
						}
					}
					else
						argVal = argument.Value;
					long val = (long)CSharpPrimitiveCast.Cast(TypeCode.Int64, argVal, false);
					return MakePrimitive(val, type, sb);
				} catch (SystemException) {
				}
			}
			if (argument.Value is TypeSig sig)
				return CreateTypeOfExpression(sig.ToTypeDefOrRef(), sb);
			if (argument.Value is UTF8String utf8String)
				return new PrimitiveExpression(utf8String.String);
			return new PrimitiveExpression(argument.Value);
		}
		#endregion

		internal static Expression MakePrimitive(long val, ITypeDefOrRef type, StringBuilder sb)
		{
			if (val == 0 && type.IsSystemBoolean())
				return new Ast.PrimitiveExpression(false);
			else if (val == 1 && type.IsSystemBoolean())
				return new Ast.PrimitiveExpression(true);
			else if (val == 0 && type.TryGetPtrSig() != null)
				return new Ast.NullReferenceExpression();
			if (type != null)
			{ // cannot rely on type.IsValueType, it's not set for typerefs (but is set for typespecs)
				TypeDef enumDefinition = type.ResolveTypeDef();
				if (enumDefinition != null && enumDefinition.IsEnum) {
					TypeCode enumBaseTypeCode = TypeCode.Int32;
					for (int i = 0; i < enumDefinition.Fields.Count; i++) {
						var field = enumDefinition.Fields[i];
						if (field.IsStatic) {
							TryGetConstant(field, out var constant);
							TypeCode c = constant == null ? TypeCode.Empty : Type.GetTypeCode(constant.GetType());
							if (c >= TypeCode.Char && c <= TypeCode.Decimal &&
								object.Equals(CSharpPrimitiveCast.Cast(TypeCode.Int64, constant, false), val))
								return ConvertType(type, sb).Member(field.Name, field).WithAnnotation(field);
						} else if (!field.IsStatic)
							enumBaseTypeCode = TypeAnalysis.GetTypeCode(field.FieldType); // use primitive type of the enum
					}
					if (IsFlagsEnum(enumDefinition)) {
						long enumValue = val;
						Expression expr = null;
						long negatedEnumValue = ~val;
						// limit negatedEnumValue to the appropriate range
						switch (enumBaseTypeCode) {
							case TypeCode.Byte:
							case TypeCode.SByte:
								negatedEnumValue &= byte.MaxValue;
								break;
							case TypeCode.Char:
							case TypeCode.Int16:
							case TypeCode.UInt16:
								negatedEnumValue &= ushort.MaxValue;
								break;
							case TypeCode.Int32:
							case TypeCode.UInt32:
								negatedEnumValue &= uint.MaxValue;
								break;
						}
						Expression negatedExpr = null;
						foreach (FieldDef field in enumDefinition.Fields.Where(fld => fld.IsStatic)) {
							TryGetConstant(field, out var constant);
							TypeCode c = constant == null ? TypeCode.Empty : Type.GetTypeCode(constant.GetType());
							if (c < TypeCode.Char || c > TypeCode.Decimal)
								continue;
							long fieldValue = (long)CSharpPrimitiveCast.Cast(TypeCode.Int64, constant, false);
							if (fieldValue == 0)
								continue;	// skip None enum value

							if ((fieldValue & enumValue) == fieldValue) {
								var fieldExpression = ConvertType(type, sb).Member(field.Name, field).WithAnnotation(field);
								if (expr == null)
									expr = fieldExpression;
								else
									expr = new BinaryOperatorExpression(expr, BinaryOperatorType.BitwiseOr, fieldExpression);

								enumValue &= ~fieldValue;
							}
							if ((fieldValue & negatedEnumValue) == fieldValue) {
								var fieldExpression = ConvertType(type, sb).Member(field.Name, field).WithAnnotation(field);
								if (negatedExpr == null)
									negatedExpr = fieldExpression;
								else
									negatedExpr = new BinaryOperatorExpression(negatedExpr, BinaryOperatorType.BitwiseOr, fieldExpression);

								negatedEnumValue &= ~fieldValue;
							}
						}
						if (enumValue == 0 && expr != null) {
							if (!(negatedEnumValue == 0 && negatedExpr != null && negatedExpr.Descendants.Count() < expr.Descendants.Count())) {
								return expr;
							}
						}
						if (negatedEnumValue == 0 && negatedExpr != null) {
							return new UnaryOperatorExpression(UnaryOperatorType.BitNot, negatedExpr);
						}
					}
					if (enumBaseTypeCode < TypeCode.Char || enumBaseTypeCode > TypeCode.Decimal)
						enumBaseTypeCode = TypeCode.Int32;
					return new Ast.PrimitiveExpression(CSharpPrimitiveCast.Cast(enumBaseTypeCode, val, false)).CastTo(ConvertType(type, sb));
				}
			}
			TypeCode code = TypeAnalysis.GetTypeCode(type.ToTypeSig());
			if (code < TypeCode.Char || code > TypeCode.Decimal)
				code = TypeCode.Int32;
			return new Ast.PrimitiveExpression(CSharpPrimitiveCast.Cast(code, val, false));
		}

		static bool IsFlagsEnum(TypeDef type)
		{
			return type.IsDefined(systemString, flagsAttributeString);
		}
		static readonly UTF8String flagsAttributeString = new UTF8String("FlagsAttribute");

		/// <summary>
		/// Sets new modifier if the member hides some other member from a base type.
		/// </summary>
		/// <param name="member">The node of the member which new modifier state should be determined.</param>
		static void SetNewModifier(EntityDeclaration member)
		{
			try {
				bool addNewModifier = false;
				if (member is IndexerDeclaration) {
					var propertyDef = member.Annotation<PropertyDef>();
					var baseProperties =
						TypesHierarchyHelpers.FindBaseProperties(propertyDef);
					addNewModifier = baseProperties.Any();
				} else
					addNewModifier = HidesBaseMember(member);

				if (addNewModifier)
					member.Modifiers |= Modifiers.New;
			}
			catch (ResolveException) {
				// TODO: add some kind of notification (a comment?) about possible problems with decompiled code due to unresolved references.
			}
		}

		private static bool HidesBaseMember(EntityDeclaration member)
		{
			var memberDefinition = member.Annotation<IMemberDef>();
			bool addNewModifier = false;
			var methodDefinition = memberDefinition as MethodDef;
			if (methodDefinition != null) {
				addNewModifier = HidesByName(memberDefinition, includeBaseMethods: false);
				if (!addNewModifier)
					addNewModifier = TypesHierarchyHelpers.FindBaseMethods(methodDefinition, compareReturnType: false).Any();
			} else
				addNewModifier = HidesByName(memberDefinition, includeBaseMethods: true);
			return addNewModifier;
		}

		/// <summary>
		/// Determines whether any base class member has the same name as the given member.
		/// </summary>
		/// <param name="member">The derived type's member.</param>
		/// <param name="includeBaseMethods">true if names of methods declared in base types should also be checked.</param>
		/// <returns>true if any base member has the same name as given member, otherwise false.</returns>
		static bool HidesByName(IMemberDef member, bool includeBaseMethods)
		{
			if (member == null)
				return false;
			Debug.Assert(!(member is PropertyDef) || !((PropertyDef)member).IsIndexer());

			if (member.DeclaringType.BaseType != null) {
				var baseTypeRef = member.DeclaringType.BaseType;
				while (baseTypeRef != null) {
					var baseType = baseTypeRef.ResolveTypeDef();
					if (baseType == null)
						break;
					if (baseType.HasProperties && AnyIsHiddenBy(baseType.Properties, member, m => !m.IsIndexer()))
						return true;
					if (baseType.HasEvents && AnyIsHiddenBy(baseType.Events, member))
						return true;
					if (baseType.HasFields && AnyIsHiddenBy(baseType.Fields, member))
						return true;
					if (includeBaseMethods && baseType.HasMethods
					    && AnyIsHiddenBy(baseType.Methods, member, m => !m.IsSpecialName))
						return true;
					if (baseType.HasNestedTypes && AnyIsHiddenBy(baseType.NestedTypes, member))
						return true;
					baseTypeRef = baseType.BaseType;
				}
			}
			return false;
		}

		static bool AnyIsHiddenBy<T>(IEnumerable<T> members, IMemberDef derived, Predicate<T> condition = null)
			where T : IMemberDef
		{
			return members.Any(m => m.Name == derived.Name
			                   && (condition == null || condition(m))
			                   && TypesHierarchyHelpers.IsVisibleFromDerived(m, derived.DeclaringType));
		}
	}
}
