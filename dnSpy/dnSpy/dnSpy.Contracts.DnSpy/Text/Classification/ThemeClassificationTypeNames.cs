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

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Classification type names
	/// </summary>
	public static class ThemeClassificationTypeNames {
		/// <summary>
		/// Identifier
		/// </summary>
		public const string Identifier = "identifier";

		/// <summary>
		/// Literal
		/// </summary>
		public const string Literal = "literal";

		/// <summary>
		/// <see cref="TextColor.Text"/>
		/// </summary>
		public const string Text = RoslynClassificationTypeNames.Text;

		/// <summary>
		/// <see cref="TextColor.Operator"/>
		/// </summary>
		public const string Operator = PredefinedClassificationTypeNames.Operator;

		/// <summary>
		/// <see cref="TextColor.Punctuation"/>
		/// </summary>
		public const string Punctuation = RoslynClassificationTypeNames.Punctuation;

		/// <summary>
		/// <see cref="TextColor.Number"/>
		/// </summary>
		public const string Number = PredefinedClassificationTypeNames.Number;

		/// <summary>
		/// <see cref="TextColor.Comment"/>
		/// </summary>
		public const string Comment = PredefinedClassificationTypeNames.Comment;

		/// <summary>
		/// <see cref="TextColor.Keyword"/>
		/// </summary>
		public const string Keyword = PredefinedClassificationTypeNames.Keyword;

		/// <summary>
		/// <see cref="TextColor.String"/>
		/// </summary>
		public const string String = PredefinedClassificationTypeNames.String;

		/// <summary>
		/// <see cref="TextColor.VerbatimString"/>
		/// </summary>
		public const string VerbatimString = RoslynClassificationTypeNames.VerbatimStringLiteral;

		/// <summary>
		/// <see cref="TextColor.Char"/>
		/// </summary>
		public const string Char = PredefinedClassificationTypeNames.Character;

		/// <summary>
		/// <see cref="TextColor.Namespace"/>
		/// </summary>
		public const string Namespace = "Theme-" + nameof(Namespace);

		/// <summary>
		/// <see cref="TextColor.Type"/>
		/// </summary>
		public const string Type = RoslynClassificationTypeNames.ClassName;

		/// <summary>
		/// <see cref="TextColor.SealedType"/>
		/// </summary>
		public const string SealedType = "Theme-" + nameof(SealedType);

		/// <summary>
		/// <see cref="TextColor.StaticType"/>
		/// </summary>
		public const string StaticType = "Theme-" + nameof(StaticType);

		/// <summary>
		/// <see cref="TextColor.Delegate"/>
		/// </summary>
		public const string Delegate = RoslynClassificationTypeNames.DelegateName;

		/// <summary>
		/// <see cref="TextColor.Enum"/>
		/// </summary>
		public const string Enum = RoslynClassificationTypeNames.EnumName;

		/// <summary>
		/// <see cref="TextColor.Interface"/>
		/// </summary>
		public const string Interface = RoslynClassificationTypeNames.InterfaceName;

		/// <summary>
		/// <see cref="TextColor.ValueType"/>
		/// </summary>
		public const string ValueType = RoslynClassificationTypeNames.StructName;

		/// <summary>
		/// <see cref="TextColor.Module"/>
		/// </summary>
		public const string Module = RoslynClassificationTypeNames.ModuleName;

		/// <summary>
		/// <see cref="TextColor.TypeGenericParameter"/>
		/// </summary>
		public const string TypeGenericParameter = RoslynClassificationTypeNames.TypeParameterName;

		/// <summary>
		/// <see cref="TextColor.MethodGenericParameter"/>
		/// </summary>
		public const string MethodGenericParameter = "Theme-" + nameof(MethodGenericParameter);

		/// <summary>
		/// <see cref="TextColor.InstanceMethod"/>
		/// </summary>
		public const string InstanceMethod = "Theme-" + nameof(InstanceMethod);

		/// <summary>
		/// <see cref="TextColor.StaticMethod"/>
		/// </summary>
		public const string StaticMethod = "Theme-" + nameof(StaticMethod);

		/// <summary>
		/// <see cref="TextColor.ExtensionMethod"/>
		/// </summary>
		public const string ExtensionMethod = "Theme-" + nameof(ExtensionMethod);

		/// <summary>
		/// <see cref="TextColor.InstanceField"/>
		/// </summary>
		public const string InstanceField = "Theme-" + nameof(InstanceField);

		/// <summary>
		/// <see cref="TextColor.EnumField"/>
		/// </summary>
		public const string EnumField = "Theme-" + nameof(EnumField);

		/// <summary>
		/// <see cref="TextColor.LiteralField"/>
		/// </summary>
		public const string LiteralField = "Theme-" + nameof(LiteralField);

		/// <summary>
		/// <see cref="TextColor.StaticField"/>
		/// </summary>
		public const string StaticField = "Theme-" + nameof(StaticField);

		/// <summary>
		/// <see cref="TextColor.InstanceEvent"/>
		/// </summary>
		public const string InstanceEvent = "Theme-" + nameof(InstanceEvent);

		/// <summary>
		/// <see cref="TextColor.StaticEvent"/>
		/// </summary>
		public const string StaticEvent = "Theme-" + nameof(StaticEvent);

		/// <summary>
		/// <see cref="TextColor.InstanceProperty"/>
		/// </summary>
		public const string InstanceProperty = "Theme-" + nameof(InstanceProperty);

		/// <summary>
		/// <see cref="TextColor.StaticProperty"/>
		/// </summary>
		public const string StaticProperty = "Theme-" + nameof(StaticProperty);

		/// <summary>
		/// <see cref="TextColor.Local"/>
		/// </summary>
		public const string Local = "Theme-" + nameof(Local);

		/// <summary>
		/// <see cref="TextColor.Parameter"/>
		/// </summary>
		public const string Parameter = "Theme-" + nameof(Parameter);

		/// <summary>
		/// <see cref="TextColor.PreprocessorKeyword"/>
		/// </summary>
		public const string PreprocessorKeyword = PredefinedClassificationTypeNames.PreprocessorKeyword;

		/// <summary>
		/// <see cref="TextColor.PreprocessorText"/>
		/// </summary>
		public const string PreprocessorText = RoslynClassificationTypeNames.PreprocessorText;

		/// <summary>
		/// <see cref="TextColor.Label"/>
		/// </summary>
		public const string Label = "Theme-" + nameof(Label);

		/// <summary>
		/// <see cref="TextColor.OpCode"/>
		/// </summary>
		public const string OpCode = "Theme-" + nameof(OpCode);

		/// <summary>
		/// <see cref="TextColor.ILDirective"/>
		/// </summary>
		public const string ILDirective = "Theme-" + nameof(ILDirective);

		/// <summary>
		/// <see cref="TextColor.ILModule"/>
		/// </summary>
		public const string ILModule = "Theme-" + nameof(ILModule);

		/// <summary>
		/// <see cref="TextColor.ExcludedCode"/>
		/// </summary>
		public const string ExcludedCode = PredefinedClassificationTypeNames.ExcludedCode;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentAttributeName"/>
		/// </summary>
		public const string XmlDocCommentAttributeName = RoslynClassificationTypeNames.XmlDocCommentAttributeName;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentAttributeQuotes"/>
		/// </summary>
		public const string XmlDocCommentAttributeQuotes = RoslynClassificationTypeNames.XmlDocCommentAttributeQuotes;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentAttributeValue"/>
		/// </summary>
		public const string XmlDocCommentAttributeValue = RoslynClassificationTypeNames.XmlDocCommentAttributeValue;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentCDataSection"/>
		/// </summary>
		public const string XmlDocCommentCDataSection = RoslynClassificationTypeNames.XmlDocCommentCDataSection;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentComment"/>
		/// </summary>
		public const string XmlDocCommentComment = RoslynClassificationTypeNames.XmlDocCommentComment;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentDelimiter"/>
		/// </summary>
		public const string XmlDocCommentDelimiter = RoslynClassificationTypeNames.XmlDocCommentDelimiter;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentEntityReference"/>
		/// </summary>
		public const string XmlDocCommentEntityReference = RoslynClassificationTypeNames.XmlDocCommentEntityReference;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentName"/>
		/// </summary>
		public const string XmlDocCommentName = RoslynClassificationTypeNames.XmlDocCommentName;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentProcessingInstruction"/>
		/// </summary>
		public const string XmlDocCommentProcessingInstruction = RoslynClassificationTypeNames.XmlDocCommentProcessingInstruction;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentText"/>
		/// </summary>
		public const string XmlDocCommentText = RoslynClassificationTypeNames.XmlDocCommentText;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralAttributeName"/>
		/// </summary>
		public const string XmlLiteralAttributeName = RoslynClassificationTypeNames.XmlLiteralAttributeName;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralAttributeQuotes"/>
		/// </summary>
		public const string XmlLiteralAttributeQuotes = RoslynClassificationTypeNames.XmlLiteralAttributeQuotes;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralAttributeValue"/>
		/// </summary>
		public const string XmlLiteralAttributeValue = RoslynClassificationTypeNames.XmlLiteralAttributeValue;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralCDataSection"/>
		/// </summary>
		public const string XmlLiteralCDataSection = RoslynClassificationTypeNames.XmlLiteralCDataSection;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralComment"/>
		/// </summary>
		public const string XmlLiteralComment = RoslynClassificationTypeNames.XmlLiteralComment;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralDelimiter"/>
		/// </summary>
		public const string XmlLiteralDelimiter = RoslynClassificationTypeNames.XmlLiteralDelimiter;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralEmbeddedExpression"/>
		/// </summary>
		public const string XmlLiteralEmbeddedExpression = RoslynClassificationTypeNames.XmlLiteralEmbeddedExpression;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralEntityReference"/>
		/// </summary>
		public const string XmlLiteralEntityReference = RoslynClassificationTypeNames.XmlLiteralEntityReference;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralName"/>
		/// </summary>
		public const string XmlLiteralName = RoslynClassificationTypeNames.XmlLiteralName;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralProcessingInstruction"/>
		/// </summary>
		public const string XmlLiteralProcessingInstruction = RoslynClassificationTypeNames.XmlLiteralProcessingInstruction;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralText"/>
		/// </summary>
		public const string XmlLiteralText = RoslynClassificationTypeNames.XmlLiteralText;

		/// <summary>
		/// <see cref="TextColor.XmlAttribute"/>
		/// </summary>
		public const string XmlAttribute = "Theme-" + nameof(XmlAttribute);

		/// <summary>
		/// <see cref="TextColor.XmlAttributeQuotes"/>
		/// </summary>
		public const string XmlAttributeQuotes = "Theme-" + nameof(XmlAttributeQuotes);

		/// <summary>
		/// <see cref="TextColor.XmlAttributeValue"/>
		/// </summary>
		public const string XmlAttributeValue = "Theme-" + nameof(XmlAttributeValue);

		/// <summary>
		/// <see cref="TextColor.XmlCDataSection"/>
		/// </summary>
		public const string XmlCDataSection = "Theme-" + nameof(XmlCDataSection);

		/// <summary>
		/// <see cref="TextColor.XmlComment"/>
		/// </summary>
		public const string XmlComment = "Theme-" + nameof(XmlComment);

		/// <summary>
		/// <see cref="TextColor.XmlDelimiter"/>
		/// </summary>
		public const string XmlDelimiter = "Theme-" + nameof(XmlDelimiter);

		/// <summary>
		/// <see cref="TextColor.XmlKeyword"/>
		/// </summary>
		public const string XmlKeyword = "Theme-" + nameof(XmlKeyword);

		/// <summary>
		/// <see cref="TextColor.XmlName"/>
		/// </summary>
		public const string XmlName = "Theme-" + nameof(XmlName);

		/// <summary>
		/// <see cref="TextColor.XmlProcessingInstruction"/>
		/// </summary>
		public const string XmlProcessingInstruction = "Theme-" + nameof(XmlProcessingInstruction);

		/// <summary>
		/// <see cref="TextColor.XmlText"/>
		/// </summary>
		public const string XmlText = "Theme-" + nameof(XmlText);

		/// <summary>
		/// <see cref="TextColor.XamlAttribute"/>
		/// </summary>
		public const string XamlAttribute = "Theme-" + nameof(XamlAttribute);

		/// <summary>
		/// <see cref="TextColor.XamlAttributeQuotes"/>
		/// </summary>
		public const string XamlAttributeQuotes = "Theme-" + nameof(XamlAttributeQuotes);

		/// <summary>
		/// <see cref="TextColor.XamlAttributeValue"/>
		/// </summary>
		public const string XamlAttributeValue = "Theme-" + nameof(XamlAttributeValue);

		/// <summary>
		/// <see cref="TextColor.XamlCDataSection"/>
		/// </summary>
		public const string XamlCDataSection = "Theme-" + nameof(XamlCDataSection);

		/// <summary>
		/// <see cref="TextColor.XamlComment"/>
		/// </summary>
		public const string XamlComment = "Theme-" + nameof(XamlComment);

		/// <summary>
		/// <see cref="TextColor.XamlDelimiter"/>
		/// </summary>
		public const string XamlDelimiter = "Theme-" + nameof(XamlDelimiter);

		/// <summary>
		/// <see cref="TextColor.XamlKeyword"/>
		/// </summary>
		public const string XamlKeyword = "Theme-" + nameof(XamlKeyword);

		/// <summary>
		/// <see cref="TextColor.XamlMarkupExtensionClass"/>
		/// </summary>
		public const string XamlMarkupExtensionClass = "Theme-" + nameof(XamlMarkupExtensionClass);

		/// <summary>
		/// <see cref="TextColor.XamlMarkupExtensionParameterName"/>
		/// </summary>
		public const string XamlMarkupExtensionParameterName = "Theme-" + nameof(XamlMarkupExtensionParameterName);

		/// <summary>
		/// <see cref="TextColor.XamlMarkupExtensionParameterValue"/>
		/// </summary>
		public const string XamlMarkupExtensionParameterValue = "Theme-" + nameof(XamlMarkupExtensionParameterValue);

		/// <summary>
		/// <see cref="TextColor.XamlName"/>
		/// </summary>
		public const string XamlName = "Theme-" + nameof(XamlName);

		/// <summary>
		/// <see cref="TextColor.XamlProcessingInstruction"/>
		/// </summary>
		public const string XamlProcessingInstruction = "Theme-" + nameof(XamlProcessingInstruction);

		/// <summary>
		/// <see cref="TextColor.XamlText"/>
		/// </summary>
		public const string XamlText = "Theme-" + nameof(XamlText);

		/// <summary>
		/// <see cref="TextColor.XmlDocToolTipHeader"/>
		/// </summary>
		public const string XmlDocToolTipHeader = "Theme-" + nameof(XmlDocToolTipHeader);

		/// <summary>
		/// <see cref="TextColor.Assembly"/>
		/// </summary>
		public const string Assembly = "Theme-" + nameof(Assembly);

		/// <summary>
		/// <see cref="TextColor.AssemblyExe"/>
		/// </summary>
		public const string AssemblyExe = "Theme-" + nameof(AssemblyExe);

		/// <summary>
		/// <see cref="TextColor.AssemblyModule"/>
		/// </summary>
		public const string AssemblyModule = "Theme-" + nameof(AssemblyModule);

		/// <summary>
		/// <see cref="TextColor.DirectoryPart"/>
		/// </summary>
		public const string DirectoryPart = "Theme-" + nameof(DirectoryPart);

		/// <summary>
		/// <see cref="TextColor.FileNameNoExtension"/>
		/// </summary>
		public const string FileNameNoExtension = "Theme-" + nameof(FileNameNoExtension);

		/// <summary>
		/// <see cref="TextColor.FileExtension"/>
		/// </summary>
		public const string FileExtension = "Theme-" + nameof(FileExtension);

		/// <summary>
		/// <see cref="TextColor.Error"/>
		/// </summary>
		public const string Error = "Theme-" + nameof(Error);

		/// <summary>
		/// <see cref="TextColor.ToStringEval"/>
		/// </summary>
		public const string ToStringEval = "Theme-" + nameof(ToStringEval);

		/// <summary>
		/// <see cref="TextColor.ReplPrompt1"/>
		/// </summary>
		public const string ReplPrompt1 = "Theme-" + nameof(ReplPrompt1);

		/// <summary>
		/// <see cref="TextColor.ReplPrompt2"/>
		/// </summary>
		public const string ReplPrompt2 = "Theme-" + nameof(ReplPrompt2);

		/// <summary>
		/// <see cref="TextColor.ReplOutputText"/>
		/// </summary>
		public const string ReplOutputText = "Theme-" + nameof(ReplOutputText);

		/// <summary>
		/// <see cref="TextColor.ReplScriptOutputText"/>
		/// </summary>
		public const string ReplScriptOutputText = "Theme-" + nameof(ReplScriptOutputText);

		/// <summary>
		/// <see cref="TextColor.Black"/>
		/// </summary>
		public const string Black = "Theme-" + nameof(Black);

		/// <summary>
		/// <see cref="TextColor.Blue"/>
		/// </summary>
		public const string Blue = "Theme-" + nameof(Blue);

		/// <summary>
		/// <see cref="TextColor.Cyan"/>
		/// </summary>
		public const string Cyan = "Theme-" + nameof(Cyan);

		/// <summary>
		/// <see cref="TextColor.DarkBlue"/>
		/// </summary>
		public const string DarkBlue = "Theme-" + nameof(DarkBlue);

		/// <summary>
		/// <see cref="TextColor.DarkCyan"/>
		/// </summary>
		public const string DarkCyan = "Theme-" + nameof(DarkCyan);

		/// <summary>
		/// <see cref="TextColor.DarkGray"/>
		/// </summary>
		public const string DarkGray = "Theme-" + nameof(DarkGray);

		/// <summary>
		/// <see cref="TextColor.DarkGreen"/>
		/// </summary>
		public const string DarkGreen = "Theme-" + nameof(DarkGreen);

		/// <summary>
		/// <see cref="TextColor.DarkMagenta"/>
		/// </summary>
		public const string DarkMagenta = "Theme-" + nameof(DarkMagenta);

		/// <summary>
		/// <see cref="TextColor.DarkRed"/>
		/// </summary>
		public const string DarkRed = "Theme-" + nameof(DarkRed);

		/// <summary>
		/// <see cref="TextColor.DarkYellow"/>
		/// </summary>
		public const string DarkYellow = "Theme-" + nameof(DarkYellow);

		/// <summary>
		/// <see cref="TextColor.Gray"/>
		/// </summary>
		public const string Gray = "Theme-" + nameof(Gray);

		/// <summary>
		/// <see cref="TextColor.Green"/>
		/// </summary>
		public const string Green = "Theme-" + nameof(Green);

		/// <summary>
		/// <see cref="TextColor.Magenta"/>
		/// </summary>
		public const string Magenta = "Theme-" + nameof(Magenta);

		/// <summary>
		/// <see cref="TextColor.Red"/>
		/// </summary>
		public const string Red = "Theme-" + nameof(Red);

		/// <summary>
		/// <see cref="TextColor.White"/>
		/// </summary>
		public const string White = "Theme-" + nameof(White);

		/// <summary>
		/// <see cref="TextColor.Yellow"/>
		/// </summary>
		public const string Yellow = "Theme-" + nameof(Yellow);

		/// <summary>
		/// <see cref="TextColor.InvBlack"/>
		/// </summary>
		public const string InvBlack = "Theme-" + nameof(InvBlack);

		/// <summary>
		/// <see cref="TextColor.InvBlue"/>
		/// </summary>
		public const string InvBlue = "Theme-" + nameof(InvBlue);

		/// <summary>
		/// <see cref="TextColor.InvCyan"/>
		/// </summary>
		public const string InvCyan = "Theme-" + nameof(InvCyan);

		/// <summary>
		/// <see cref="TextColor.InvDarkBlue"/>
		/// </summary>
		public const string InvDarkBlue = "Theme-" + nameof(InvDarkBlue);

		/// <summary>
		/// <see cref="TextColor.InvDarkCyan"/>
		/// </summary>
		public const string InvDarkCyan = "Theme-" + nameof(InvDarkCyan);

		/// <summary>
		/// <see cref="TextColor.InvDarkGray"/>
		/// </summary>
		public const string InvDarkGray = "Theme-" + nameof(InvDarkGray);

		/// <summary>
		/// <see cref="TextColor.InvDarkGreen"/>
		/// </summary>
		public const string InvDarkGreen = "Theme-" + nameof(InvDarkGreen);

		/// <summary>
		/// <see cref="TextColor.InvDarkMagenta"/>
		/// </summary>
		public const string InvDarkMagenta = "Theme-" + nameof(InvDarkMagenta);

		/// <summary>
		/// <see cref="TextColor.InvDarkRed"/>
		/// </summary>
		public const string InvDarkRed = "Theme-" + nameof(InvDarkRed);

		/// <summary>
		/// <see cref="TextColor.InvDarkYellow"/>
		/// </summary>
		public const string InvDarkYellow = "Theme-" + nameof(InvDarkYellow);

		/// <summary>
		/// <see cref="TextColor.InvGray"/>
		/// </summary>
		public const string InvGray = "Theme-" + nameof(InvGray);

		/// <summary>
		/// <see cref="TextColor.InvGreen"/>
		/// </summary>
		public const string InvGreen = "Theme-" + nameof(InvGreen);

		/// <summary>
		/// <see cref="TextColor.InvMagenta"/>
		/// </summary>
		public const string InvMagenta = "Theme-" + nameof(InvMagenta);

		/// <summary>
		/// <see cref="TextColor.InvRed"/>
		/// </summary>
		public const string InvRed = "Theme-" + nameof(InvRed);

		/// <summary>
		/// <see cref="TextColor.InvWhite"/>
		/// </summary>
		public const string InvWhite = "Theme-" + nameof(InvWhite);

		/// <summary>
		/// <see cref="TextColor.InvYellow"/>
		/// </summary>
		public const string InvYellow = "Theme-" + nameof(InvYellow);

		/// <summary>
		/// <see cref="TextColor.DebugLogExceptionHandled"/>
		/// </summary>
		public const string DebugLogExceptionHandled = "Theme-" + nameof(DebugLogExceptionHandled);

		/// <summary>
		/// <see cref="TextColor.DebugLogExceptionUnhandled"/>
		/// </summary>
		public const string DebugLogExceptionUnhandled = "Theme-" + nameof(DebugLogExceptionUnhandled);

		/// <summary>
		/// <see cref="TextColor.DebugLogStepFiltering"/>
		/// </summary>
		public const string DebugLogStepFiltering = "Theme-" + nameof(DebugLogStepFiltering);

		/// <summary>
		/// <see cref="TextColor.DebugLogLoadModule"/>
		/// </summary>
		public const string DebugLogLoadModule = "Theme-" + nameof(DebugLogLoadModule);

		/// <summary>
		/// <see cref="TextColor.DebugLogUnloadModule"/>
		/// </summary>
		public const string DebugLogUnloadModule = "Theme-" + nameof(DebugLogUnloadModule);

		/// <summary>
		/// <see cref="TextColor.DebugLogExitProcess"/>
		/// </summary>
		public const string DebugLogExitProcess = "Theme-" + nameof(DebugLogExitProcess);

		/// <summary>
		/// <see cref="TextColor.DebugLogExitThread"/>
		/// </summary>
		public const string DebugLogExitThread = "Theme-" + nameof(DebugLogExitThread);

		/// <summary>
		/// <see cref="TextColor.DebugLogProgramOutput"/>
		/// </summary>
		public const string DebugLogProgramOutput = "Theme-" + nameof(DebugLogProgramOutput);

		/// <summary>
		/// <see cref="TextColor.DebugLogMDA"/>
		/// </summary>
		public const string DebugLogMDA = "Theme-" + nameof(DebugLogMDA);

		/// <summary>
		/// <see cref="TextColor.DebugLogTimestamp"/>
		/// </summary>
		public const string DebugLogTimestamp = "Theme-" + nameof(DebugLogTimestamp);

		/// <summary>
		/// <see cref="TextColor.LineNumber"/>
		/// </summary>
		public const string LineNumber = "line number";

		/// <summary>
		/// <see cref="TextColor.ReplLineNumberInput1"/>
		/// </summary>
		public const string ReplLineNumberInput1 = "Theme-" + nameof(ReplLineNumberInput1);

		/// <summary>
		/// <see cref="TextColor.ReplLineNumberInput2"/>
		/// </summary>
		public const string ReplLineNumberInput2 = "Theme-" + nameof(ReplLineNumberInput2);

		/// <summary>
		/// <see cref="TextColor.ReplLineNumberOutput"/>
		/// </summary>
		public const string ReplLineNumberOutput = "Theme-" + nameof(ReplLineNumberOutput);

		/// <summary>
		/// <see cref="TextColor.VisibleWhitespace"/>
		/// </summary>
		public const string VisibleWhitespace = "Theme-" + nameof(VisibleWhitespace);

		/// <summary>
		/// <see cref="TextColor.SelectedText"/>
		/// </summary>
		public const string SelectedText = "Theme-" + nameof(SelectedText);

		/// <summary>
		/// <see cref="TextColor.InactiveSelectedText"/>
		/// </summary>
		public const string InactiveSelectedText = "Theme-" + nameof(InactiveSelectedText);

		/// <summary>
		/// <see cref="TextColor.HighlightedReference"/>
		/// </summary>
		public const string HighlightedReference = "Theme-" + nameof(HighlightedReference);

		/// <summary>
		/// <see cref="TextColor.HighlightedWrittenReference"/>
		/// </summary>
		public const string HighlightedWrittenReference = "Theme-" + nameof(HighlightedWrittenReference);

		/// <summary>
		/// <see cref="TextColor.HighlightedDefinition"/>
		/// </summary>
		public const string HighlightedDefinition = "Theme-" + nameof(HighlightedDefinition);

		/// <summary>
		/// <see cref="TextColor.CurrentStatement"/>
		/// </summary>
		public const string CurrentStatement = "Theme-" + nameof(CurrentStatement);

		/// <summary>
		/// <see cref="TextColor.CurrentStatementMarker"/>
		/// </summary>
		public const string CurrentStatementMarker = "Theme-" + nameof(CurrentStatementMarker);

		/// <summary>
		/// <see cref="TextColor.CallReturn"/>
		/// </summary>
		public const string CallReturn = "Theme-" + nameof(CallReturn);

		/// <summary>
		/// <see cref="TextColor.CallReturnMarker"/>
		/// </summary>
		public const string CallReturnMarker = "Theme-" + nameof(CallReturnMarker);

		/// <summary>
		/// <see cref="TextColor.ActiveStatementMarker"/>
		/// </summary>
		public const string ActiveStatementMarker = "Theme-" + nameof(ActiveStatementMarker);

		/// <summary>
		/// <see cref="TextColor.BreakpointStatement"/>
		/// </summary>
		public const string BreakpointStatement = "Theme-" + nameof(BreakpointStatement);

		/// <summary>
		/// <see cref="TextColor.BreakpointStatementMarker"/>
		/// </summary>
		public const string BreakpointStatementMarker = "Theme-" + nameof(BreakpointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.SelectedBreakpointStatementMarker"/>
		/// </summary>
		public const string SelectedBreakpointStatementMarker = "Theme-" + nameof(SelectedBreakpointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.DisabledBreakpointStatementMarker"/>
		/// </summary>
		public const string DisabledBreakpointStatementMarker = "Theme-" + nameof(DisabledBreakpointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.CurrentLine"/>
		/// </summary>
		public const string CurrentLine = "Theme-" + nameof(CurrentLine);

		/// <summary>
		/// <see cref="TextColor.CurrentLineNoFocus"/>
		/// </summary>
		public const string CurrentLineNoFocus = "Theme-" + nameof(CurrentLineNoFocus);

		/// <summary>
		/// <see cref="TextColor.HexText"/>
		/// </summary>
		public const string HexText = "Theme-" + nameof(HexText);

		/// <summary>
		/// <see cref="TextColor.HexOffset"/>
		/// </summary>
		public const string HexOffset = "Theme-" + nameof(HexOffset);

		/// <summary>
		/// <see cref="TextColor.HexByte0"/>
		/// </summary>
		public const string HexByte0 = "Theme-" + nameof(HexByte0);

		/// <summary>
		/// <see cref="TextColor.HexByte1"/>
		/// </summary>
		public const string HexByte1 = "Theme-" + nameof(HexByte1);

		/// <summary>
		/// <see cref="TextColor.HexByteError"/>
		/// </summary>
		public const string HexByteError = "Theme-" + nameof(HexByteError);

		/// <summary>
		/// <see cref="TextColor.HexAscii"/>
		/// </summary>
		public const string HexAscii = "Theme-" + nameof(HexAscii);

		/// <summary>
		/// <see cref="TextColor.HexCaret"/>
		/// </summary>
		public const string HexCaret = "Theme-" + nameof(HexCaret);

		/// <summary>
		/// <see cref="TextColor.HexInactiveCaret"/>
		/// </summary>
		public const string HexInactiveCaret = "Theme-" + nameof(HexInactiveCaret);

		/// <summary>
		/// <see cref="TextColor.HexSelection"/>
		/// </summary>
		public const string HexSelection = "Theme-" + nameof(HexSelection);

		/// <summary>
		/// <see cref="TextColor.GlyphMargin"/>
		/// </summary>
		public const string GlyphMargin = "Theme-" + nameof(GlyphMargin);

		/// <summary>
		/// <see cref="TextColor.BraceMatching"/>
		/// </summary>
		public const string BraceMatching = "brace matching";

		/// <summary>
		/// <see cref="TextColor.LineSeparator"/>
		/// </summary>
		public const string LineSeparator = "Theme-" + nameof(LineSeparator);

		/// <summary>
		/// <see cref="TextColor.FindMatchHighlightMarker"/>
		/// </summary>
		public const string FindMatchHighlightMarker = "Theme-" + nameof(FindMatchHighlightMarker);

		/// <summary>
		/// <see cref="TextColor.BlockStructureNamespace"/>
		/// </summary>
		public const string BlockStructureNamespace = "Theme-" + nameof(BlockStructureNamespace);

		/// <summary>
		/// <see cref="TextColor.BlockStructureType"/>
		/// </summary>
		public const string BlockStructureType = "Theme-" + nameof(BlockStructureType);

		/// <summary>
		/// <see cref="TextColor.BlockStructureModule"/>
		/// </summary>
		public const string BlockStructureModule = "Theme-" + nameof(BlockStructureModule);

		/// <summary>
		/// <see cref="TextColor.BlockStructureValueType"/>
		/// </summary>
		public const string BlockStructureValueType = "Theme-" + nameof(BlockStructureValueType);

		/// <summary>
		/// <see cref="TextColor.BlockStructureInterface"/>
		/// </summary>
		public const string BlockStructureInterface = "Theme-" + nameof(BlockStructureInterface);

		/// <summary>
		/// <see cref="TextColor.BlockStructureMethod"/>
		/// </summary>
		public const string BlockStructureMethod = "Theme-" + nameof(BlockStructureMethod);

		/// <summary>
		/// <see cref="TextColor.BlockStructureAccessor"/>
		/// </summary>
		public const string BlockStructureAccessor = "Theme-" + nameof(BlockStructureAccessor);

		/// <summary>
		/// <see cref="TextColor.BlockStructureAnonymousMethod"/>
		/// </summary>
		public const string BlockStructureAnonymousMethod = "Theme-" + nameof(BlockStructureAnonymousMethod);

		/// <summary>
		/// <see cref="TextColor.BlockStructureConstructor"/>
		/// </summary>
		public const string BlockStructureConstructor = "Theme-" + nameof(BlockStructureConstructor);

		/// <summary>
		/// <see cref="TextColor.BlockStructureDestructor"/>
		/// </summary>
		public const string BlockStructureDestructor = "Theme-" + nameof(BlockStructureDestructor);

		/// <summary>
		/// <see cref="TextColor.BlockStructureOperator"/>
		/// </summary>
		public const string BlockStructureOperator = "Theme-" + nameof(BlockStructureOperator);

		/// <summary>
		/// <see cref="TextColor.BlockStructureConditional"/>
		/// </summary>
		public const string BlockStructureConditional = "Theme-" + nameof(BlockStructureConditional);

		/// <summary>
		/// <see cref="TextColor.BlockStructureLoop"/>
		/// </summary>
		public const string BlockStructureLoop = "Theme-" + nameof(BlockStructureLoop);

		/// <summary>
		/// <see cref="TextColor.BlockStructureProperty"/>
		/// </summary>
		public const string BlockStructureProperty = "Theme-" + nameof(BlockStructureProperty);

		/// <summary>
		/// <see cref="TextColor.BlockStructureEvent"/>
		/// </summary>
		public const string BlockStructureEvent = "Theme-" + nameof(BlockStructureEvent);

		/// <summary>
		/// <see cref="TextColor.BlockStructureTry"/>
		/// </summary>
		public const string BlockStructureTry = "Theme-" + nameof(BlockStructureTry);

		/// <summary>
		/// <see cref="TextColor.BlockStructureCatch"/>
		/// </summary>
		public const string BlockStructureCatch = "Theme-" + nameof(BlockStructureCatch);

		/// <summary>
		/// <see cref="TextColor.BlockStructureFilter"/>
		/// </summary>
		public const string BlockStructureFilter = "Theme-" + nameof(BlockStructureFilter);

		/// <summary>
		/// <see cref="TextColor.BlockStructureFinally"/>
		/// </summary>
		public const string BlockStructureFinally = "Theme-" + nameof(BlockStructureFinally);

		/// <summary>
		/// <see cref="TextColor.BlockStructureFault"/>
		/// </summary>
		public const string BlockStructureFault = "Theme-" + nameof(BlockStructureFault);

		/// <summary>
		/// <see cref="TextColor.BlockStructureLock"/>
		/// </summary>
		public const string BlockStructureLock = "Theme-" + nameof(BlockStructureLock);

		/// <summary>
		/// <see cref="TextColor.BlockStructureUsing"/>
		/// </summary>
		public const string BlockStructureUsing = "Theme-" + nameof(BlockStructureUsing);

		/// <summary>
		/// <see cref="TextColor.BlockStructureFixed"/>
		/// </summary>
		public const string BlockStructureFixed = "Theme-" + nameof(BlockStructureFixed);

		/// <summary>
		/// <see cref="TextColor.BlockStructureSwitch"/>
		/// </summary>
		public const string BlockStructureSwitch = "Theme-" + nameof(BlockStructureSwitch);

		/// <summary>
		/// <see cref="TextColor.BlockStructureCase"/>
		/// </summary>
		public const string BlockStructureCase = "Theme-" + nameof(BlockStructureCase);

		/// <summary>
		/// <see cref="TextColor.BlockStructureLocalFunction"/>
		/// </summary>
		public const string BlockStructureLocalFunction = "Theme-" + nameof(BlockStructureLocalFunction);

		/// <summary>
		/// <see cref="TextColor.BlockStructureOther"/>
		/// </summary>
		public const string BlockStructureOther = "Theme-" + nameof(BlockStructureOther);

		/// <summary>
		/// <see cref="TextColor.BlockStructureXml"/>
		/// </summary>
		public const string BlockStructureXml = "Theme-" + nameof(BlockStructureXml);

		/// <summary>
		/// <see cref="TextColor.BlockStructureXaml"/>
		/// </summary>
		public const string BlockStructureXaml = "Theme-" + nameof(BlockStructureXaml);

		/// <summary>
		/// <see cref="TextColor.CompletionMatchHighlight"/>
		/// </summary>
		public const string CompletionMatchHighlight = "Theme-" + nameof(CompletionMatchHighlight);

		/// <summary>
		/// <see cref="TextColor.CompletionSuffix"/>
		/// </summary>
		public const string CompletionSuffix = "Theme-" + nameof(CompletionSuffix);

		/// <summary>
		/// <see cref="TextColor.SignatureHelpDocumentation"/>
		/// </summary>
		public const string SignatureHelpDocumentation = "sighelp-documentation";

		/// <summary>
		/// <see cref="TextColor.SignatureHelpCurrentParameter"/>
		/// </summary>
		public const string SignatureHelpCurrentParameter = "currentParam";

		/// <summary>
		/// <see cref="TextColor.SignatureHelpParameter"/>
		/// </summary>
		public const string SignatureHelpParameter = "Theme-" + nameof(SignatureHelpParameter);

		/// <summary>
		/// <see cref="TextColor.SignatureHelpParameterDocumentation"/>
		/// </summary>
		public const string SignatureHelpParameterDocumentation = "Theme-" + nameof(SignatureHelpParameterDocumentation);

		/// <summary>
		/// <see cref="TextColor.Url"/>
		/// </summary>
		public const string Url = "url";

		/// <summary>
		/// <see cref="TextColor.HexPeDosHeader"/>
		/// </summary>
		public const string HexPeDosHeader = "Theme-" + nameof(HexPeDosHeader);

		/// <summary>
		/// <see cref="TextColor.HexPeFileHeader"/>
		/// </summary>
		public const string HexPeFileHeader = "Theme-" + nameof(HexPeFileHeader);

		/// <summary>
		/// <see cref="TextColor.HexPeOptionalHeader32"/>
		/// </summary>
		public const string HexPeOptionalHeader32 = "Theme-" + nameof(HexPeOptionalHeader32);

		/// <summary>
		/// <see cref="TextColor.HexPeOptionalHeader64"/>
		/// </summary>
		public const string HexPeOptionalHeader64 = "Theme-" + nameof(HexPeOptionalHeader64);

		/// <summary>
		/// <see cref="TextColor.HexPeSection"/>
		/// </summary>
		public const string HexPeSection = "Theme-" + nameof(HexPeSection);

		/// <summary>
		/// <see cref="TextColor.HexPeSectionName"/>
		/// </summary>
		public const string HexPeSectionName = "Theme-" + nameof(HexPeSectionName);

		/// <summary>
		/// <see cref="TextColor.HexCor20Header"/>
		/// </summary>
		public const string HexCor20Header = "Theme-" + nameof(HexCor20Header);

		/// <summary>
		/// <see cref="TextColor.HexStorageSignature"/>
		/// </summary>
		public const string HexStorageSignature = "Theme-" + nameof(HexStorageSignature);

		/// <summary>
		/// <see cref="TextColor.HexStorageHeader"/>
		/// </summary>
		public const string HexStorageHeader = "Theme-" + nameof(HexStorageHeader);

		/// <summary>
		/// <see cref="TextColor.HexStorageStream"/>
		/// </summary>
		public const string HexStorageStream = "Theme-" + nameof(HexStorageStream);

		/// <summary>
		/// <see cref="TextColor.HexStorageStreamName"/>
		/// </summary>
		public const string HexStorageStreamName = "Theme-" + nameof(HexStorageStreamName);

		/// <summary>
		/// <see cref="TextColor.HexStorageStreamNameInvalid"/>
		/// </summary>
		public const string HexStorageStreamNameInvalid = "Theme-" + nameof(HexStorageStreamNameInvalid);

		/// <summary>
		/// <see cref="TextColor.HexTablesStream"/>
		/// </summary>
		public const string HexTablesStream = "Theme-" + nameof(HexTablesStream);

		/// <summary>
		/// <see cref="TextColor.HexTableName"/>
		/// </summary>
		public const string HexTableName = "Theme-" + nameof(HexTableName);

		/// <summary>
		/// <see cref="TextColor.DocumentListMatchHighlight"/>
		/// </summary>
		public const string DocumentListMatchHighlight = "Theme-" + nameof(DocumentListMatchHighlight);

		/// <summary>
		/// <see cref="TextColor.GacMatchHighlight"/>
		/// </summary>
		public const string GacMatchHighlight = "Theme-" + nameof(GacMatchHighlight);

		/// <summary>
		/// <see cref="TextColor.AppSettingsTreeViewNodeMatchHighlight"/>
		/// </summary>
		public const string AppSettingsTreeViewNodeMatchHighlight = "Theme-" + nameof(AppSettingsTreeViewNodeMatchHighlight);

		/// <summary>
		/// <see cref="TextColor.AppSettingsTextMatchHighlight"/>
		/// </summary>
		public const string AppSettingsTextMatchHighlight = "Theme-" + nameof(AppSettingsTextMatchHighlight);

		/// <summary>
		/// <see cref="TextColor.HexCurrentLine"/>
		/// </summary>
		public const string HexCurrentLine = "Theme-" + nameof(HexCurrentLine);

		/// <summary>
		/// <see cref="TextColor.HexCurrentLineNoFocus"/>
		/// </summary>
		public const string HexCurrentLineNoFocus = "Theme-" + nameof(HexCurrentLineNoFocus);

		/// <summary>
		/// <see cref="TextColor.HexInactiveSelectedText"/>
		/// </summary>
		public const string HexInactiveSelectedText = "Theme-" + nameof(HexInactiveSelectedText);

		/// <summary>
		/// <see cref="TextColor.HexColumnLine0"/>
		/// </summary>
		public const string HexColumnLine0 = "Theme-" + nameof(HexColumnLine0);

		/// <summary>
		/// <see cref="TextColor.HexColumnLine1"/>
		/// </summary>
		public const string HexColumnLine1 = "Theme-" + nameof(HexColumnLine1);

		/// <summary>
		/// <see cref="TextColor.HexColumnLineGroup0"/>
		/// </summary>
		public const string HexColumnLineGroup0 = "Theme-" + nameof(HexColumnLineGroup0);

		/// <summary>
		/// <see cref="TextColor.HexColumnLineGroup1"/>
		/// </summary>
		public const string HexColumnLineGroup1 = "Theme-" + nameof(HexColumnLineGroup1);

		/// <summary>
		/// <see cref="TextColor.HexHighlightedValuesColumn"/>
		/// </summary>
		public const string HexHighlightedValuesColumn = "Theme-" + nameof(HexHighlightedValuesColumn);

		/// <summary>
		/// <see cref="TextColor.HexHighlightedAsciiColumn"/>
		/// </summary>
		public const string HexHighlightedAsciiColumn = "Theme-" + nameof(HexHighlightedAsciiColumn);

		/// <summary>
		/// <see cref="TextColor.HexGlyphMargin"/>
		/// </summary>
		public const string HexGlyphMargin = "Theme-" + nameof(HexGlyphMargin);

		/// <summary>
		/// <see cref="TextColor.HexCurrentValueCell"/>
		/// </summary>
		public const string HexCurrentValueCell = "Theme-" + nameof(HexCurrentValueCell);

		/// <summary>
		/// <see cref="TextColor.HexCurrentAsciiCell"/>
		/// </summary>
		public const string HexCurrentAsciiCell = "Theme-" + nameof(HexCurrentAsciiCell);

		/// <summary>
		/// <see cref="TextColor.OutputWindowText"/>
		/// </summary>
		public const string OutputWindowText = "Theme-" + nameof(OutputWindowText);

		/// <summary>
		/// <see cref="TextColor.HexFindMatchHighlightMarker"/>
		/// </summary>
		public const string HexFindMatchHighlightMarker = "Theme-" + nameof(HexFindMatchHighlightMarker);

		/// <summary>
		/// <see cref="TextColor.HexToolTipServiceField0"/>
		/// </summary>
		public const string HexToolTipServiceField0 = "Theme-" + nameof(HexToolTipServiceField0);

		/// <summary>
		/// <see cref="TextColor.HexToolTipServiceField1"/>
		/// </summary>
		public const string HexToolTipServiceField1 = "Theme-" + nameof(HexToolTipServiceField1);

		/// <summary>
		/// <see cref="TextColor.HexToolTipServiceCurrentField"/>
		/// </summary>
		public const string HexToolTipServiceCurrentField = "Theme-" + nameof(HexToolTipServiceCurrentField);

		/// <summary>
		/// <see cref="TextColor.ListFindMatchHighlight"/>
		/// </summary>
		public const string ListFindMatchHighlight = "Theme-" + nameof(ListFindMatchHighlight);

		/// <summary>
		/// <see cref="TextColor.AdvancedBreakpointStatement"/>
		/// </summary>
		public const string AdvancedBreakpointStatement = "Theme-" + nameof(AdvancedBreakpointStatement);

		/// <summary>
		/// <see cref="TextColor.AdvancedBreakpointStatementMarker"/>
		/// </summary>
		public const string AdvancedBreakpointStatementMarker = "Theme-" + nameof(AdvancedBreakpointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.SelectedAdvancedBreakpointStatementMarker"/>
		/// </summary>
		public const string SelectedAdvancedBreakpointStatementMarker = "Theme-" + nameof(SelectedAdvancedBreakpointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.DisabledAdvancedBreakpointStatement"/>
		/// </summary>
		public const string DisabledAdvancedBreakpointStatement = "Theme-" + nameof(DisabledAdvancedBreakpointStatement);

		/// <summary>
		/// <see cref="TextColor.DisabledAdvancedBreakpointStatementMarker"/>
		/// </summary>
		public const string DisabledAdvancedBreakpointStatementMarker = "Theme-" + nameof(DisabledAdvancedBreakpointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.SelectedDisabledAdvancedBreakpointStatementMarker"/>
		/// </summary>
		public const string SelectedDisabledAdvancedBreakpointStatementMarker = "Theme-" + nameof(SelectedDisabledAdvancedBreakpointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.BreakpointWarningStatement"/>
		/// </summary>
		public const string BreakpointWarningStatement = "Theme-" + nameof(BreakpointWarningStatement);

		/// <summary>
		/// <see cref="TextColor.BreakpointWarningStatementMarker"/>
		/// </summary>
		public const string BreakpointWarningStatementMarker = "Theme-" + nameof(BreakpointWarningStatementMarker);

		/// <summary>
		/// <see cref="TextColor.SelectedBreakpointWarningStatementMarker"/>
		/// </summary>
		public const string SelectedBreakpointWarningStatementMarker = "Theme-" + nameof(SelectedBreakpointWarningStatementMarker);

		/// <summary>
		/// <see cref="TextColor.BreakpointErrorStatement"/>
		/// </summary>
		public const string BreakpointErrorStatement = "Theme-" + nameof(BreakpointErrorStatement);

		/// <summary>
		/// <see cref="TextColor.BreakpointErrorStatementMarker"/>
		/// </summary>
		public const string BreakpointErrorStatementMarker = "Theme-" + nameof(BreakpointErrorStatementMarker);

		/// <summary>
		/// <see cref="TextColor.SelectedBreakpointErrorStatementMarker"/>
		/// </summary>
		public const string SelectedBreakpointErrorStatementMarker = "Theme-" + nameof(SelectedBreakpointErrorStatementMarker);

		/// <summary>
		/// <see cref="TextColor.AdvancedBreakpointWarningStatement"/>
		/// </summary>
		public const string AdvancedBreakpointWarningStatement = "Theme-" + nameof(AdvancedBreakpointWarningStatement);

		/// <summary>
		/// <see cref="TextColor.AdvancedBreakpointWarningStatementMarker"/>
		/// </summary>
		public const string AdvancedBreakpointWarningStatementMarker = "Theme-" + nameof(AdvancedBreakpointWarningStatementMarker);

		/// <summary>
		/// <see cref="TextColor.SelectedAdvancedBreakpointWarningStatementMarker"/>
		/// </summary>
		public const string SelectedAdvancedBreakpointWarningStatementMarker = "Theme-" + nameof(SelectedAdvancedBreakpointWarningStatementMarker);

		/// <summary>
		/// <see cref="TextColor.AdvancedBreakpointErrorStatement"/>
		/// </summary>
		public const string AdvancedBreakpointErrorStatement = "Theme-" + nameof(AdvancedBreakpointErrorStatement);

		/// <summary>
		/// <see cref="TextColor.AdvancedBreakpointErrorStatementMarker"/>
		/// </summary>
		public const string AdvancedBreakpointErrorStatementMarker = "Theme-" + nameof(AdvancedBreakpointErrorStatementMarker);

		/// <summary>
		/// <see cref="TextColor.SelectedAdvancedBreakpointErrorStatementMarker"/>
		/// </summary>
		public const string SelectedAdvancedBreakpointErrorStatementMarker = "Theme-" + nameof(SelectedAdvancedBreakpointErrorStatementMarker);

		/// <summary>
		/// <see cref="TextColor.TracepointStatement"/>
		/// </summary>
		public const string TracepointStatement = "Theme-" + nameof(TracepointStatement);

		/// <summary>
		/// <see cref="TextColor.TracepointStatementMarker"/>
		/// </summary>
		public const string TracepointStatementMarker = "Theme-" + nameof(TracepointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.SelectedTracepointStatementMarker"/>
		/// </summary>
		public const string SelectedTracepointStatementMarker = "Theme-" + nameof(SelectedTracepointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.DisabledTracepointStatement"/>
		/// </summary>
		public const string DisabledTracepointStatement = "Theme-" + nameof(DisabledTracepointStatement);

		/// <summary>
		/// <see cref="TextColor.DisabledTracepointStatementMarker"/>
		/// </summary>
		public const string DisabledTracepointStatementMarker = "Theme-" + nameof(DisabledTracepointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.SelectedDisabledTracepointStatementMarker"/>
		/// </summary>
		public const string SelectedDisabledTracepointStatementMarker = "Theme-" + nameof(SelectedDisabledTracepointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.AdvancedTracepointStatement"/>
		/// </summary>
		public const string AdvancedTracepointStatement = "Theme-" + nameof(AdvancedTracepointStatement);

		/// <summary>
		/// <see cref="TextColor.AdvancedTracepointStatementMarker"/>
		/// </summary>
		public const string AdvancedTracepointStatementMarker = "Theme-" + nameof(AdvancedTracepointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.SelectedAdvancedTracepointStatementMarker"/>
		/// </summary>
		public const string SelectedAdvancedTracepointStatementMarker = "Theme-" + nameof(SelectedAdvancedTracepointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.DisabledAdvancedTracepointStatement"/>
		/// </summary>
		public const string DisabledAdvancedTracepointStatement = "Theme-" + nameof(DisabledAdvancedTracepointStatement);

		/// <summary>
		/// <see cref="TextColor.DisabledAdvancedTracepointStatementMarker"/>
		/// </summary>
		public const string DisabledAdvancedTracepointStatementMarker = "Theme-" + nameof(DisabledAdvancedTracepointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.SelectedDisabledAdvancedTracepointStatementMarker"/>
		/// </summary>
		public const string SelectedDisabledAdvancedTracepointStatementMarker = "Theme-" + nameof(SelectedDisabledAdvancedTracepointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.TracepointWarningStatement"/>
		/// </summary>
		public const string TracepointWarningStatement = "Theme-" + nameof(TracepointWarningStatement);

		/// <summary>
		/// <see cref="TextColor.TracepointWarningStatementMarker"/>
		/// </summary>
		public const string TracepointWarningStatementMarker = "Theme-" + nameof(TracepointWarningStatementMarker);

		/// <summary>
		/// <see cref="TextColor.SelectedTracepointWarningStatementMarker"/>
		/// </summary>
		public const string SelectedTracepointWarningStatementMarker = "Theme-" + nameof(SelectedTracepointWarningStatementMarker);

		/// <summary>
		/// <see cref="TextColor.TracepointErrorStatement"/>
		/// </summary>
		public const string TracepointErrorStatement = "Theme-" + nameof(TracepointErrorStatement);

		/// <summary>
		/// <see cref="TextColor.TracepointErrorStatementMarker"/>
		/// </summary>
		public const string TracepointErrorStatementMarker = "Theme-" + nameof(TracepointErrorStatementMarker);

		/// <summary>
		/// <see cref="TextColor.SelectedTracepointErrorStatementMarker"/>
		/// </summary>
		public const string SelectedTracepointErrorStatementMarker = "Theme-" + nameof(SelectedTracepointErrorStatementMarker);

		/// <summary>
		/// <see cref="TextColor.AdvancedTracepointWarningStatement"/>
		/// </summary>
		public const string AdvancedTracepointWarningStatement = "Theme-" + nameof(AdvancedTracepointWarningStatement);

		/// <summary>
		/// <see cref="TextColor.AdvancedTracepointWarningStatementMarker"/>
		/// </summary>
		public const string AdvancedTracepointWarningStatementMarker = "Theme-" + nameof(AdvancedTracepointWarningStatementMarker);

		/// <summary>
		/// <see cref="TextColor.SelectedAdvancedTracepointWarningStatementMarker"/>
		/// </summary>
		public const string SelectedAdvancedTracepointWarningStatementMarker = "Theme-" + nameof(SelectedAdvancedTracepointWarningStatementMarker);

		/// <summary>
		/// <see cref="TextColor.AdvancedTracepointErrorStatement"/>
		/// </summary>
		public const string AdvancedTracepointErrorStatement = "Theme-" + nameof(AdvancedTracepointErrorStatement);

		/// <summary>
		/// <see cref="TextColor.AdvancedTracepointErrorStatementMarker"/>
		/// </summary>
		public const string AdvancedTracepointErrorStatementMarker = "Theme-" + nameof(AdvancedTracepointErrorStatementMarker);

		/// <summary>
		/// <see cref="TextColor.SelectedAdvancedTracepointErrorStatementMarker"/>
		/// </summary>
		public const string SelectedAdvancedTracepointErrorStatementMarker = "Theme-" + nameof(SelectedAdvancedTracepointErrorStatementMarker);

		/// <summary>
		/// <see cref="TextColor.BookmarkName"/>
		/// </summary>
		public const string BookmarkName = "Theme-" + nameof(BookmarkName);

		/// <summary>
		/// <see cref="TextColor.ActiveBookmarkName"/>
		/// </summary>
		public const string ActiveBookmarkName = "Theme-" + nameof(ActiveBookmarkName);

		/// <summary>
		/// <see cref="TextColor.DebugLogTrace"/>
		/// </summary>
		public const string DebugLogTrace = "Theme-" + nameof(DebugLogTrace);

		/// <summary>
		/// <see cref="TextColor.DebugLogExtensionMessage"/>
		/// </summary>
		public const string DebugLogExtensionMessage = "Theme-" + nameof(DebugLogExtensionMessage);

		/// <summary>
		/// <see cref="TextColor.DebuggerValueChangedHighlight"/>
		/// </summary>
		public const string DebuggerValueChangedHighlight = "Theme-" + nameof(DebuggerValueChangedHighlight);

		/// <summary>
		/// <see cref="TextColor.DebugExceptionName"/>
		/// </summary>
		public const string DebugExceptionName = "Theme-" + nameof(DebugExceptionName);

		/// <summary>
		/// <see cref="TextColor.DebugStowedExceptionName"/>
		/// </summary>
		public const string DebugStowedExceptionName = "Theme-" + nameof(DebugStowedExceptionName);

		/// <summary>
		/// <see cref="TextColor.DebugReturnValueName"/>
		/// </summary>
		public const string DebugReturnValueName = "Theme-" + nameof(DebugReturnValueName);

		/// <summary>
		/// <see cref="TextColor.DebugVariableName"/>
		/// </summary>
		public const string DebugVariableName = "Theme-" + nameof(DebugVariableName);

		/// <summary>
		/// <see cref="TextColor.DebugObjectIdName"/>
		/// </summary>
		public const string DebugObjectIdName = "Theme-" + nameof(DebugObjectIdName);

		/// <summary>
		/// <see cref="TextColor.DebuggerDisplayAttributeEval"/>
		/// </summary>
		public const string DebuggerDisplayAttributeEval = "Theme-" + nameof(DebuggerDisplayAttributeEval);

		/// <summary>
		/// <see cref="TextColor.DebuggerNoStringQuotesEval"/>
		/// </summary>
		public const string DebuggerNoStringQuotesEval = "Theme-" + nameof(DebuggerNoStringQuotesEval);

		/// <summary>
		/// <see cref="TextColor.DebugViewPropertyName"/>
		/// </summary>
		public const string DebugViewPropertyName = "Theme-" + nameof(DebugViewPropertyName);

		/// <summary>
		/// <see cref="TextColor.AsmComment"/>
		/// </summary>
		public const string AsmComment = "Theme-" + nameof(AsmComment);

		/// <summary>
		/// <see cref="TextColor.AsmDirective"/>
		/// </summary>
		public const string AsmDirective = "Theme-" + nameof(AsmDirective);

		/// <summary>
		/// <see cref="TextColor.AsmPrefix"/>
		/// </summary>
		public const string AsmPrefix = "Theme-" + nameof(AsmPrefix);

		/// <summary>
		/// <see cref="TextColor.AsmMnemonic"/>
		/// </summary>
		public const string AsmMnemonic = "Theme-" + nameof(AsmMnemonic);

		/// <summary>
		/// <see cref="TextColor.AsmKeyword"/>
		/// </summary>
		public const string AsmKeyword = "Theme-" + nameof(AsmKeyword);

		/// <summary>
		/// <see cref="TextColor.AsmOperator"/>
		/// </summary>
		public const string AsmOperator = "Theme-" + nameof(AsmOperator);

		/// <summary>
		/// <see cref="TextColor.AsmPunctuation"/>
		/// </summary>
		public const string AsmPunctuation = "Theme-" + nameof(AsmPunctuation);

		/// <summary>
		/// <see cref="TextColor.AsmNumber"/>
		/// </summary>
		public const string AsmNumber = "Theme-" + nameof(AsmNumber);

		/// <summary>
		/// <see cref="TextColor.AsmRegister"/>
		/// </summary>
		public const string AsmRegister = "Theme-" + nameof(AsmRegister);

		/// <summary>
		/// <see cref="TextColor.AsmSelectorValue"/>
		/// </summary>
		public const string AsmSelectorValue = "Theme-" + nameof(AsmSelectorValue);

		/// <summary>
		/// <see cref="TextColor.AsmLabelAddress"/>
		/// </summary>
		public const string AsmLabelAddress = "Theme-" + nameof(AsmLabelAddress);

		/// <summary>
		/// <see cref="TextColor.AsmFunctionAddress"/>
		/// </summary>
		public const string AsmFunctionAddress = "Theme-" + nameof(AsmFunctionAddress);

		/// <summary>
		/// <see cref="TextColor.AsmLabel"/>
		/// </summary>
		public const string AsmLabel = "Theme-" + nameof(AsmLabel);

		/// <summary>
		/// <see cref="TextColor.AsmFunction"/>
		/// </summary>
		public const string AsmFunction = "Theme-" + nameof(AsmFunction);

		/// <summary>
		/// <see cref="TextColor.AsmData"/>
		/// </summary>
		public const string AsmData = "Theme-" + nameof(AsmData);

		/// <summary>
		/// <see cref="TextColor.AsmAddress"/>
		/// </summary>
		public const string AsmAddress = "Theme-" + nameof(AsmAddress);

		/// <summary>
		/// <see cref="TextColor.AsmHexBytes"/>
		/// </summary>
		public const string AsmHexBytes = "Theme-" + nameof(AsmHexBytes);
	}
}
