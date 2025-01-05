using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Decompiler.XmlDoc;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.IL;
using dnSpy.Decompiler.ILSpy.Core.CSharp;
using dnSpy.Decompiler.ILSpy.Core.IL;
using dnSpy.Decompiler.ILSpy.Core.Settings;
using dnSpy.Decompiler.ILSpy.Core.Text;
using dnSpy.Decompiler.ILSpy.Core.XmlDoc;
using dnSpy.Decompiler.Utils;
using ICSharpCode.Decompiler.Disassembler;

namespace dnSpy.Decompiler.ILSpy.Core.Mixed {
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
			yield return new ILWithCSharpDecompiler(decompilerSettingsService.ILDecompilerSettings, decompilerSettingsService.CSharpVBDecompilerSettings);
		}
	}

	sealed class ILWithCSharpDecompiler : DecompilerBase {
		public override DecompilerSettingsBase Settings => ilLangSettings;
		readonly ILDecompilerSettings ilLangSettings;
		readonly CSharpVBDecompilerSettings csharpLangSettings;
		readonly Func<BuilderCache> createBuilderCache;

		public ILWithCSharpDecompiler(ILDecompilerSettings ilLangSettings, CSharpVBDecompilerSettings csharpLangSettings) {
			this.ilLangSettings = ilLangSettings;
			this.csharpLangSettings = csharpLangSettings;
			createBuilderCache = () => new BuilderCache(this.csharpLangSettings.Settings.SettingsVersion);
		}

		public override double OrderUI => DecompilerConstants.IL_WITH_CSHARP_ILSPY_ORDERUI;
		public override string ContentTypeString => ContentTypesInternal.ILWithCSharpILSpy;
		public override string GenericNameUI => DecompilerConstants.GENERIC_NAMEUI_IL_WITH_CSHARP;
		public override string UniqueNameUI => "IL with C#";
		public override Guid GenericGuid => DecompilerConstants.LANGUAGE_IL;
		public override Guid UniqueGuid => DecompilerConstants.LANGUAGE_IL_WITH_CSHARP_ILSPY;
		public override string FileExtension => ".il";

		ReflectionDisassembler CreateReflectionDisassembler(IDecompilerOutput output, DecompilationContext ctx, IMemberDef member) =>
			CreateReflectionDisassembler(output, ctx, member.Module);

		ReflectionDisassembler CreateReflectionDisassembler(IDecompilerOutput output, DecompilationContext ctx, ModuleDef ownerModule) {
			var disOpts = new DisassemblerOptions(ilLangSettings.Settings.SettingsVersion, ctx.CancellationToken, ownerModule);
			if (ilLangSettings.Settings.ShowILComments)
				disOpts.GetOpCodeDocumentation = ILLanguageHelper.GetOpCodeDocumentation;
			var sb = new StringBuilder();
			if (ilLangSettings.Settings.ShowXmlDocumentation)
				disOpts.GetXmlDocComments = a => ILDecompiler.GetXmlDocComments(a, sb);
			disOpts.CreateInstructionBytesReader = m => InstructionBytesReader.Create(m, ctx.IsBodyModified is not null && ctx.IsBodyModified(m));
			disOpts.ShowTokenAndRvaComments = ilLangSettings.Settings.ShowTokenAndRvaComments;
			disOpts.ShowILBytes = ilLangSettings.Settings.ShowILBytes;
			disOpts.SortMembers = ilLangSettings.Settings.SortMembers;
			disOpts.ShowPdbInfo = ilLangSettings.Settings.ShowPdbInfo;
			disOpts.MaxStringLength = ilLangSettings.Settings.MaxStringLength;
			disOpts.HexadecimalNumbers = ilLangSettings.Settings.HexadecimalNumbers;
			return new ReflectionDisassembler(output, disOpts, new CustomMethodBodyDisassembler(output, true, disOpts, sb, def => GetDebugInfo(ctx, def, sb)), sb);
		}

		SourceStatementProvider GetDebugInfo(DecompilationContext ctx, MethodDef method, StringBuilder sb) {
			var output = new DecompilerOutputImpl(sb);

			MethodDebugInfo debugInfo;
			if (StateMachineHelpers.TryGetKickoffMethod(method, out var containingMethod)) {
				var info = TryDecompileCode(containingMethod, ctx, output, false);
				if (info.stateMachineDebugInfo is null || info.stateMachineDebugInfo.Method != method) {
					// The decompiler can't decompile the iterator / async method, try again,
					// but only decompile the MoveNext method
					info = TryDecompileCode(method, ctx, output, true);
				}
				debugInfo = info.stateMachineDebugInfo ?? info.debugInfo;
			}
			else
				debugInfo = TryDecompileCode(method, ctx, output, true).debugInfo;

			if (debugInfo is null)
				return default;
			return new SourceStatementProvider(output.ToString(), debugInfo);
		}

		(MethodDebugInfo debugInfo, MethodDebugInfo? stateMachineDebugInfo) TryDecompileCode(MethodDef method, DecompilationContext ctx, DecompilerOutputImpl output, bool disableStateMachineDecompilation) {
			output.Initialize(method);
			var settings = csharpLangSettings.Settings.Clone();
			settings.UsingDeclarations = false;
			if (disableStateMachineDecompilation) {
				settings.AsyncAwait = false;
				settings.YieldReturn = false;
			}
			var state = new BuilderState(ctx, ctx.GetOrCreate(createBuilderCache), MetadataTextColorProvider);
			try {
				state.AstBuilder.Context.CurrentModule = method.DeclaringType?.Module;
				state.AstBuilder.Context.CurrentType = method.DeclaringType;
				state.AstBuilder.Context.CancellationToken = ctx.CancellationToken;
				state.AstBuilder.Context.Settings = settings;
				state.AstBuilder.AddMethod(method);
				state.AstBuilder.RunTransformations();
				state.AstBuilder.GenerateCode(output);
			}
			finally {
				state.Dispose();
			}
			return output.TryGetMethodDebugInfo();
		}

		sealed class CustomMethodBodyDisassembler : MethodBodyDisassembler {
			SourceStatementProvider _statementProvider;
			readonly Func<MethodDef, SourceStatementProvider> createSourceStatementProvider;

			public CustomMethodBodyDisassembler(IDecompilerOutput output, bool detectControlStructure, DisassemblerOptions options, StringBuilder stringBuilder, Func<MethodDef, SourceStatementProvider> createSourceStatementProvider)
				: base(output, detectControlStructure, options, stringBuilder) => this.createSourceStatementProvider = createSourceStatementProvider;


			public override void Disassemble(MethodDef method, MethodDebugInfoBuilder builder, InstructionOperandConverter instructionOperandConverter) {
				_statementProvider = createSourceStatementProvider(method);
				base.Disassemble(method, builder, instructionOperandConverter);
			}

			protected override int WriteInstruction(InstructionOperandConverter instructionOperandConverter, long baseOffs, IInstructionBytesReader byteReader, PdbAsyncMethodCustomDebugInfo pdbAsyncInfo, MethodDef method, Instruction inst) {
				var statement = _statementProvider.GetStatement(inst.Offset);

				if (statement.line is null)
					return base.WriteInstruction(instructionOperandConverter, baseOffs, byteReader, pdbAsyncInfo, method, inst);

				output.WriteLine();

				Debug.Assert(statement.span.End <= statement.line.Length);
				int pos = 0;
				while (pos < statement.line.Length) {
					int nextLineOffset;
					int eol = statement.line.IndexOf('\n', pos);
					if (eol < 0) {
						eol = statement.line.Length;
						nextLineOffset = eol;
					}
					else {
						nextLineOffset = eol + 1;
						if (eol > 0 && statement.line[eol - 1] == '\r')
							eol--;
					}

					output.Write("// ", BoxedTextColor.XmlDocCommentText);
					output.Write(statement.line, pos, eol - pos, BoxedTextColor.XmlDocCommentText);
					output.WriteLine();

					if (pos == 0) {
						int nonSpace = FindNonSpace(statement.line, pos, eol);
						int stmtEnd = Math.Min(eol, statement.span.End);
						if (!(nonSpace >= statement.span.Start && stmtEnd == eol)) {
							output.Write("// ", BoxedTextColor.XmlDocCommentText);
							output.Write(new string(' ', statement.span.Start - pos), BoxedTextColor.XmlDocCommentText);
							output.Write(new string('^', stmtEnd - statement.span.Start), BoxedTextColor.XmlDocCommentText);
							output.WriteLine();
						}
					}

					pos = nextLineOffset;
				}

				output.WriteLine();

				return base.WriteInstruction(instructionOperandConverter, baseOffs, byteReader, pdbAsyncInfo, method, inst);
			}

			static int FindNonSpace(string lines, int pos, int end) {
				while (pos < end) {
					if (!char.IsWhiteSpace(lines[pos]))
						return pos;
					pos++;
				}
				return -1;
			}
		}

		public override void Decompile(MethodDef method, IDecompilerOutput output, DecompilationContext ctx) {
			var dis = CreateReflectionDisassembler(output, ctx, method);
			dis.DisassembleMethod(method, true);
		}

		public override void Decompile(FieldDef field, IDecompilerOutput output, DecompilationContext ctx) {
			var dis = CreateReflectionDisassembler(output, ctx, field);
			dis.DisassembleField(field, false);
		}

		public override void Decompile(PropertyDef property, IDecompilerOutput output, DecompilationContext ctx) {
			ReflectionDisassembler rd = CreateReflectionDisassembler(output, ctx, property);
			rd.DisassembleProperty(property, addLineSep: true);
			if (property.GetMethod is not null) {
				output.WriteLine();
				rd.DisassembleMethod(property.GetMethod, true);
			}
			if (property.SetMethod is not null) {
				output.WriteLine();
				rd.DisassembleMethod(property.SetMethod, true);
			}
			foreach (var m in property.OtherMethods) {
				output.WriteLine();
				rd.DisassembleMethod(m, true);
			}
		}

		public override void Decompile(EventDef ev, IDecompilerOutput output, DecompilationContext ctx) {
			ReflectionDisassembler rd = CreateReflectionDisassembler(output, ctx, ev);
			rd.DisassembleEvent(ev, addLineSep: true);
			if (ev.AddMethod is not null) {
				output.WriteLine();
				rd.DisassembleMethod(ev.AddMethod, true);
			}
			if (ev.RemoveMethod is not null) {
				output.WriteLine();
				rd.DisassembleMethod(ev.RemoveMethod, true);
			}
			foreach (var m in ev.OtherMethods) {
				output.WriteLine();
				rd.DisassembleMethod(m, true);
			}
		}

		public override void Decompile(TypeDef type, IDecompilerOutput output, DecompilationContext ctx) {
			var dis = CreateReflectionDisassembler(output, ctx, type);
			dis.DisassembleType(type, true);
		}

		public override void Decompile(AssemblyDef asm, IDecompilerOutput output, DecompilationContext ctx) {
			output.WriteLine("// " + asm.ManifestModule.Location, BoxedTextColor.Comment);
			PrintEntryPoint(asm.ManifestModule, output);
			output.WriteLine();

			ReflectionDisassembler rd = CreateReflectionDisassembler(output, ctx, asm.ManifestModule);
			rd.WriteAssemblyHeader(asm);
		}

		public override void Decompile(ModuleDef mod, IDecompilerOutput output, DecompilationContext ctx) {
			output.WriteLine("// " + mod.Location, BoxedTextColor.Comment);
			PrintEntryPoint(mod, output);
			output.WriteLine();

			ReflectionDisassembler rd = CreateReflectionDisassembler(output, ctx, mod);
			output.WriteLine();
			rd.WriteModuleHeader(mod);
		}

		protected override void TypeToString(IDecompilerOutput output, ITypeDefOrRef? t, bool includeNamespace, IHasCustomAttribute? attributeProvider = null) =>
			t.WriteTo(output, new StringBuilder(), includeNamespace ? ILNameSyntax.TypeName : ILNameSyntax.ShortTypeName);

		public override void WriteToolTip(ITextColorWriter output, IMemberRef member, IHasCustomAttribute? typeAttributes) {
			if (!(member is ITypeDefOrRef) && ILDecompilerUtils.Write(TextColorWriterToDecompilerOutput.Create(output), member))
				return;

			base.WriteToolTip(output, member, typeAttributes);
		}
	}
}
