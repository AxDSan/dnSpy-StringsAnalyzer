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
using System.Text;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.ILSpy.Core.CSharp;
using dnSpy.Decompiler.ILSpy.Core.Settings;
using dnSpy.Decompiler.ILSpy.Core.Text;
using dnSpy.Decompiler.VisualBasic;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.VB;
using ICSharpCode.NRefactory.VB.Visitors;

namespace dnSpy.Decompiler.ILSpy.Core.VisualBasic {
	sealed class DecompilerProvider : IDecompilerProvider {
		readonly DecompilerSettingsService decompilerSettingsService;

		// Keep the default ctor. It's used by dnSpy.Console.exe
		public DecompilerProvider()
			: this(DecompilerSettingsService.__Instance_DONT_USE) {
		}

		public DecompilerProvider(DecompilerSettingsService decompilerSettingsService) {
			Debug2.Assert(decompilerSettingsService is not null);
			this.decompilerSettingsService = decompilerSettingsService ?? throw new ArgumentNullException(nameof(decompilerSettingsService));
		}

		public IEnumerable<IDecompiler> Create() {
			yield return new VBDecompiler(decompilerSettingsService.CSharpVBDecompilerSettings);
		}
	}

	/// <summary>
	/// Decompiler logic for VB.
	/// </summary>
	sealed class VBDecompiler : DecompilerBase {
		readonly Predicate<IAstTransform>? transformAbortCondition = null;
		readonly bool showAllMembers = false;
		readonly Func<BuilderCache> createBuilderCache;

		public override DecompilerSettingsBase Settings => langSettings;
		readonly CSharpVBDecompilerSettings langSettings;

		public override double OrderUI => DecompilerConstants.VISUALBASIC_ILSPY_ORDERUI;
		public override MetadataTextColorProvider MetadataTextColorProvider => VisualBasicMetadataTextColorProvider.Instance;

		public VBDecompiler(CSharpVBDecompilerSettings langSettings) {
			this.langSettings = langSettings;
			createBuilderCache = () => new BuilderCache(this.langSettings.Settings.SettingsVersion);
		}

		public override string ContentTypeString => ContentTypesInternal.VisualBasicILSpy;
		public override string GenericNameUI => DecompilerConstants.GENERIC_NAMEUI_VISUALBASIC;
		public override string UniqueNameUI => "Visual Basic";
		public override Guid GenericGuid => DecompilerConstants.LANGUAGE_VISUALBASIC;
		public override Guid UniqueGuid => DecompilerConstants.LANGUAGE_VISUALBASIC_ILSPY;
		public override string FileExtension => ".vb";
		public override string? ProjectFileExtension => ".vbproj";

		public override void WriteCommentBegin(IDecompilerOutput output, bool addSpace) {
			if (addSpace)
				output.Write("' ", BoxedTextColor.Comment);
			else
				output.Write("'", BoxedTextColor.Comment);
		}

		public override void WriteCommentEnd(IDecompilerOutput output, bool addSpace) { }

		DecompilerSettings GetDecompilerSettings() {
			var settings = langSettings.Settings.Clone();
			// Different default access modifiers between C#/VB, so for now ignore the options
			settings.TypeAddInternalModifier = true;
			settings.MemberAddPrivateModifier = true;
			return settings;
		}

		public override void Decompile(AssemblyDef asm, IDecompilerOutput output, DecompilationContext ctx) {
			WriteAssembly(asm, output, ctx);

			using (ctx.DisableAssemblyLoad()) {
				var state = CreateAstBuilder(ctx, GetDecompilerSettings(), currentModule: asm.ManifestModule);
				try {
					state.AstBuilder.AddAssembly(asm.ManifestModule, true, true, false);
					RunTransformsAndGenerateCode(ref state, output, ctx);
				}
				finally {
					state.Dispose();
				}
			}
		}

		public override void Decompile(ModuleDef mod, IDecompilerOutput output, DecompilationContext ctx) {
			WriteModule(mod, output, ctx);

			using (ctx.DisableAssemblyLoad()) {
				var state = CreateAstBuilder(ctx, GetDecompilerSettings(), currentModule: mod);
				try {
					state.AstBuilder.AddAssembly(mod, true, false, true);
					RunTransformsAndGenerateCode(ref state, output, ctx);
				}
				finally {
					state.Dispose();
				}
			}
		}

		public override void Decompile(MethodDef method, IDecompilerOutput output, DecompilationContext ctx) {
			WriteCommentLineDeclaringType(output, method);
			var state = CreateAstBuilder(ctx, GetDecompilerSettings(), currentType: method.DeclaringType, isSingleMember: true);
			try {
				state.AstBuilder.AddMethod(method);
				RunTransformsAndGenerateCode(ref state, output, ctx);
			}
			finally {
				state.Dispose();
			}
		}

		public override void Decompile(PropertyDef property, IDecompilerOutput output, DecompilationContext ctx) {
			WriteCommentLineDeclaringType(output, property);
			var state = CreateAstBuilder(ctx, GetDecompilerSettings(), currentType: property.DeclaringType, isSingleMember: true);
			try {
				state.AstBuilder.AddProperty(property);
				RunTransformsAndGenerateCode(ref state, output, ctx);
			}
			finally {
				state.Dispose();
			}
		}

		public override void Decompile(FieldDef field, IDecompilerOutput output, DecompilationContext ctx) {
			WriteCommentLineDeclaringType(output, field);
			var state = CreateAstBuilder(ctx, GetDecompilerSettings(), currentType: field.DeclaringType, isSingleMember: true);
			try {
				state.AstBuilder.AddField(field);
				RunTransformsAndGenerateCode(ref state, output, ctx);
			}
			finally {
				state.Dispose();
			}
		}

		public override void Decompile(EventDef ev, IDecompilerOutput output, DecompilationContext ctx) {
			WriteCommentLineDeclaringType(output, ev);
			var state = CreateAstBuilder(ctx, GetDecompilerSettings(), currentType: ev.DeclaringType, isSingleMember: true);
			try {
				state.AstBuilder.AddEvent(ev);
				RunTransformsAndGenerateCode(ref state, output, ctx);
			}
			finally {
				state.Dispose();
			}
		}

		public override void Decompile(TypeDef type, IDecompilerOutput output, DecompilationContext ctx) {
			var state = CreateAstBuilder(ctx, GetDecompilerSettings(), currentType: type);
			try {
				state.AstBuilder.AddType(type);
				RunTransformsAndGenerateCode(ref state, output, ctx);
			}
			finally {
				state.Dispose();
			}
		}

		public override bool ShowMember(IMemberRef member) => CSharpDecompiler.ShowMember(member, showAllMembers, GetDecompilerSettings());

		VBFormattingOptions CreateVBFormattingOptions(DecompilerSettings settings) =>
			new VBFormattingOptions() {
				NumberFormatter = ICSharpCode.NRefactory.NumberFormatter.GetVBInstance(hex: settings.HexadecimalNumbers, upper: true),
			};

		void RunTransformsAndGenerateCode(ref BuilderState state, IDecompilerOutput output, DecompilationContext ctx, IAstTransform? additionalTransform = null) {
			var astBuilder = state.AstBuilder;
			astBuilder.RunTransformations(transformAbortCondition);
			if (additionalTransform is not null) {
				additionalTransform.Run(astBuilder.SyntaxTree);
			}
			var settings = GetDecompilerSettings();
			CSharpDecompiler.AddXmlDocumentation(ref state, settings, astBuilder);
			var csharpUnit = astBuilder.SyntaxTree;
			csharpUnit.AcceptVisitor(new ICSharpCode.NRefactory.CSharp.InsertParenthesesVisitor() { InsertParenthesesForReadability = true });
			GenericGrammarAmbiguityVisitor.ResolveAmbiguities(csharpUnit);
			var unit = csharpUnit.AcceptVisitor(new CSharpToVBConverterVisitor(state.AstBuilder.Context.CurrentModule, new ILSpyEnvironmentProvider(state.State.XmlDoc_StringBuilder)), null);
			var outputFormatter = new VBTextOutputFormatter(output, astBuilder.Context);
			var formattingPolicy = CreateVBFormattingOptions(settings);
			unit.AcceptVisitor(new OutputVisitor(outputFormatter, formattingPolicy), null);
		}

		BuilderState CreateAstBuilder(DecompilationContext ctx, DecompilerSettings settings, ModuleDef? currentModule = null, TypeDef? currentType = null, bool isSingleMember = false) {
			if (currentModule is null)
				currentModule = currentType?.Module;
			settings = settings.Clone();
			if (isSingleMember)
				settings.UsingDeclarations = false;
			settings.IntroduceIncrementAndDecrement = false;
			settings.MakeAssignmentExpressions = false;
			settings.QueryExpressions = false;
			settings.AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject = true;
			var cache = ctx.GetOrCreate(createBuilderCache);
			var state = new BuilderState(ctx, cache, MetadataTextColorProvider);
			state.AstBuilder.Context.CurrentModule = currentModule;
			state.AstBuilder.Context.CancellationToken = ctx.CancellationToken;
			state.AstBuilder.Context.CurrentType = currentType;
			state.AstBuilder.Context.Settings = settings;
			return state;
		}

		protected override void FormatTypeName(IDecompilerOutput output, TypeDef type) {
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			TypeToString(output, ConvertTypeOptions.DoNotUsePrimitiveTypeNames | ConvertTypeOptions.IncludeTypeParameterDefinitions | ConvertTypeOptions.DoNotIncludeEnclosingType, type);
		}

		protected override void TypeToString(IDecompilerOutput output, ITypeDefOrRef? type, bool includeNamespace, IHasCustomAttribute? typeAttributes = null) {
			ConvertTypeOptions options = ConvertTypeOptions.IncludeTypeParameterDefinitions;
			if (includeNamespace)
				options |= ConvertTypeOptions.IncludeNamespace;

			TypeToString(output, options, type, typeAttributes);
		}

		void TypeToString(IDecompilerOutput output, ConvertTypeOptions options, ITypeDefOrRef? type, IHasCustomAttribute? typeAttributes = null) {
			if (type is null)
				return;
			var envProvider = new ILSpyEnvironmentProvider();
			var converter = new CSharpToVBConverterVisitor(type.Module, envProvider);
			var astType = AstBuilder.ConvertType(type, new StringBuilder(), typeAttributes, options);

			if (type.TryGetByRefSig() is not null) {
				output.Write("ByRef", BoxedTextColor.Keyword);
				output.Write(" ", BoxedTextColor.Text);
				if (astType is ICSharpCode.NRefactory.CSharp.ComposedType && ((ICSharpCode.NRefactory.CSharp.ComposedType)astType).PointerRank > 0)
					((ICSharpCode.NRefactory.CSharp.ComposedType)astType).PointerRank--;
			}

			var vbAstType = astType.AcceptVisitor(converter, null);
			var settings = GetDecompilerSettings();
			var ctx = new DecompilerContext(settings.SettingsVersion, type.Module, MetadataTextColorProvider);
			vbAstType.AcceptVisitor(new OutputVisitor(new VBTextOutputFormatter(output, ctx), CreateVBFormattingOptions(settings)), null);
		}

		public override bool CanDecompile(DecompilationType decompilationType) {
			switch (decompilationType) {
			case DecompilationType.PartialType:
			case DecompilationType.AssemblyInfo:
			case DecompilationType.TypeMethods:
				return true;
			}
			return base.CanDecompile(decompilationType);
		}

		public override void Decompile(DecompilationType decompilationType, object data) {
			switch (decompilationType) {
			case DecompilationType.PartialType:
				DecompilePartial((DecompilePartialType)data);
				return;
			case DecompilationType.AssemblyInfo:
				DecompileAssemblyInfo((DecompileAssemblyInfo)data);
				return;
			case DecompilationType.TypeMethods:
				DecompileTypeMethods((DecompileTypeMethods)data);
				return;
			}
			base.Decompile(decompilationType, data);
		}

		void DecompilePartial(DecompilePartialType info) {
			var state = CreateAstBuilder(info.Context, CSharpDecompiler.CreateDecompilerSettings(GetDecompilerSettings(), info.UseUsingDeclarations), currentType: info.Type);
			try {
				state.AstBuilder.AddType(info.Type);
				RunTransformsAndGenerateCode(ref state, info.Output, info.Context, new DecompilePartialTransform(info.Type, info.Definitions, info.ShowDefinitions, info.AddPartialKeyword, info.InterfacesToRemove));
			}
			finally {
				state.Dispose();
			}
		}

		void DecompileAssemblyInfo(DecompileAssemblyInfo info) {
			var state = CreateAstBuilder(info.Context, GetDecompilerSettings(), currentModule: info.Module);
			try {
				state.AstBuilder.AddAssembly(info.Module, true, info.Module.IsManifestModule, true);
				RunTransformsAndGenerateCode(ref state, info.Output, info.Context, info.KeepAllAttributes ? null : new AssemblyInfoTransform());
			}
			finally {
				state.Dispose();
			}
		}

		void DecompileTypeMethods(DecompileTypeMethods info) {
			var state = CreateAstBuilder(info.Context, CSharpDecompiler.CreateDecompilerSettings_DecompileTypeMethods(GetDecompilerSettings(), !info.DecompileHidden, info.ShowAll), currentType: info.Type);
			try {
				state.AstBuilder.GetDecompiledBodyKind = (builder, method) => CSharpDecompiler.GetDecompiledBodyKind(info, builder, method);
				state.AstBuilder.AddType(info.Type);
				RunTransformsAndGenerateCode(ref state, info.Output, info.Context, new DecompileTypeMethodsTransform(info.Types, info.Methods, !info.DecompileHidden, info.ShowAll));
			}
			finally {
				state.Dispose();
			}
		}

		public override void WriteToolTip(ITextColorWriter output, IMemberRef member, IHasCustomAttribute? typeAttributes) =>
			new VisualBasicFormatter(output, DefaultFormatterOptions, null).WriteToolTip(member);
		public override void WriteToolTip(ITextColorWriter output, ISourceVariable variable) =>
			new VisualBasicFormatter(output, DefaultFormatterOptions, null).WriteToolTip(variable);
		public override void WriteNamespaceToolTip(ITextColorWriter output, string? @namespace) =>
			new VisualBasicFormatter(output, DefaultFormatterOptions, null).WriteNamespaceToolTip(@namespace);
		public override void Write(ITextColorWriter output, IMemberRef member, FormatterOptions flags) =>
			new VisualBasicFormatter(output, flags, null).Write(member);
	}
}
