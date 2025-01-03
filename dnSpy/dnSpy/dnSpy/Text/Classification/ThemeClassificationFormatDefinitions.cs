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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Classification {
	static class ThemeClassificationFormatDefinitions {
#pragma warning disable CS0169
		[Export(typeof(ClassificationTypeDefinition))]
		[Name(PredefinedClassificationTypeNames.NaturalLanguage)]
		static ClassificationTypeDefinition? NaturalLanguageClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? FormalLanguageClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Literal)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? LiteralClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Identifier)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? IdentifierClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(PredefinedClassificationTypeNames.Other)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? OtherClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(PredefinedClassificationTypeNames.WhiteSpace)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? WhiteSpaceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Text)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? TextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Operator)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? OperatorClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Punctuation)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? PunctuationClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Number)]
		[BaseDefinition(PredefinedClassificationTypeNames.Literal)]
		static ClassificationTypeDefinition? NumberClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Comment)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? CommentClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Keyword)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? KeywordClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.String)]
		[BaseDefinition(PredefinedClassificationTypeNames.Literal)]
		static ClassificationTypeDefinition? StringClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.VerbatimString)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? VerbatimStringClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Char)]
		[BaseDefinition(PredefinedClassificationTypeNames.Literal)]
		static ClassificationTypeDefinition? CharClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Namespace)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? NamespaceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Type)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? TypeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SealedType)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SealedTypeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.StaticType)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? StaticTypeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Delegate)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DelegateClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Enum)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? EnumClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Interface)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InterfaceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ValueType)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ValueTypeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Module)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ModuleClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.TypeGenericParameter)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? TypeGenericParameterClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.MethodGenericParameter)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? MethodGenericParameterClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InstanceMethod)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InstanceMethodClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.StaticMethod)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? StaticMethodClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ExtensionMethod)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ExtensionMethodClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InstanceField)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InstanceFieldClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.EnumField)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? EnumFieldClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.LiteralField)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? LiteralFieldClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.StaticField)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? StaticFieldClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InstanceEvent)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InstanceEventClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.StaticEvent)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? StaticEventClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InstanceProperty)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InstancePropertyClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.StaticProperty)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? StaticPropertyClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Local)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? LocalClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Parameter)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ParameterClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.PreprocessorKeyword)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? PreprocessorKeywordClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.PreprocessorText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? PreprocessorTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Label)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? LabelClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.OpCode)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? OpCodeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ILDirective)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ILDirectiveClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ILModule)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ILModuleClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ExcludedCode)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ExcludedCodeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentAttributeName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlDocCommentAttributeNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentAttributeQuotes)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlDocCommentAttributeQuotesClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentAttributeValue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlDocCommentAttributeValueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentCDataSection)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlDocCommentCDataSectionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentComment)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlDocCommentCommentClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentDelimiter)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlDocCommentDelimiterClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentEntityReference)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlDocCommentEntityReferenceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlDocCommentNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentProcessingInstruction)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlDocCommentProcessingInstructionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlDocCommentTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralAttributeName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlLiteralAttributeNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralAttributeQuotes)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlLiteralAttributeQuotesClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralAttributeValue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlLiteralAttributeValueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralCDataSection)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlLiteralCDataSectionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralComment)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlLiteralCommentClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralDelimiter)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlLiteralDelimiterClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralEmbeddedExpression)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlLiteralEmbeddedExpressionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralEntityReference)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlLiteralEntityReferenceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlLiteralNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralProcessingInstruction)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlLiteralProcessingInstructionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlLiteralTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlAttribute)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlAttributeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlAttributeQuotes)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlAttributeQuotesClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlAttributeValue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlAttributeValueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlCDataSection)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlCDataSectionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlComment)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlCommentClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDelimiter)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlDelimiterClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlKeyword)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlKeywordClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlProcessingInstruction)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlProcessingInstructionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XamlAttribute)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XamlAttributeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XamlAttributeQuotes)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XamlAttributeQuotesClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XamlAttributeValue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XamlAttributeValueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XamlCDataSection)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XamlCDataSectionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XamlComment)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XamlCommentClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XamlDelimiter)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XamlDelimiterClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XamlKeyword)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XamlKeywordClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XamlMarkupExtensionClass)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XamlMarkupExtensionClassClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XamlMarkupExtensionParameterName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XamlMarkupExtensionParameterNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XamlMarkupExtensionParameterValue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XamlMarkupExtensionParameterValueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XamlName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XamlNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XamlProcessingInstruction)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XamlProcessingInstructionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XamlText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XamlTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocToolTipHeader)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? XmlDocToolTipHeaderClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Assembly)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AssemblyClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AssemblyExe)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AssemblyExeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AssemblyModule)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AssemblyModuleClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DirectoryPart)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DirectoryPartClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.FileNameNoExtension)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? FileNameNoExtensionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.FileExtension)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? FileExtensionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Error)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ErrorClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ToStringEval)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ToStringEvalClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ReplPrompt1)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ReplPrompt1ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ReplPrompt2)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ReplPrompt2ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ReplOutputText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ReplOutputTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ReplScriptOutputText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ReplScriptOutputTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Black)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlackClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Blue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Cyan)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? CyanClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DarkBlue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DarkBlueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DarkCyan)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DarkCyanClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DarkGray)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DarkGrayClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DarkGreen)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DarkGreenClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DarkMagenta)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DarkMagentaClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DarkRed)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DarkRedClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DarkYellow)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DarkYellowClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Gray)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? GrayClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Green)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? GreenClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Magenta)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? MagentaClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Red)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? RedClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.White)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? WhiteClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Yellow)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? YellowClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvBlack)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InvBlackClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvBlue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InvBlueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvCyan)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InvCyanClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvDarkBlue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InvDarkBlueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvDarkCyan)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InvDarkCyanClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvDarkGray)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InvDarkGrayClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvDarkGreen)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InvDarkGreenClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvDarkMagenta)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InvDarkMagentaClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvDarkRed)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InvDarkRedClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvDarkYellow)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InvDarkYellowClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvGray)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InvGrayClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvGreen)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InvGreenClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvMagenta)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InvMagentaClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvRed)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InvRedClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvWhite)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InvWhiteClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvYellow)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InvYellowClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogExceptionHandled)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugLogExceptionHandledClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogExceptionUnhandled)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugLogExceptionUnhandledClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogStepFiltering)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugLogStepFilteringClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogLoadModule)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugLogLoadModuleClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogUnloadModule)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugLogUnloadModuleClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogExitProcess)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugLogExitProcessClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogExitThread)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugLogExitThreadClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogProgramOutput)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugLogProgramOutputClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogMDA)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugLogMDAClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogTimestamp)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugLogTimestampClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogTrace)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugLogTraceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogExtensionMessage)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugLogExtensionMessageClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.LineNumber)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? LineNumberClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ReplLineNumberInput1)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ReplLineNumberInput1ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ReplLineNumberInput2)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ReplLineNumberInput2ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ReplLineNumberOutput)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ReplLineNumberOutputClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.VisibleWhitespace)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? VisibleWhitespaceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SelectedText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SelectedTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InactiveSelectedText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? InactiveSelectedTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HighlightedReference)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HighlightedReferenceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HighlightedWrittenReference)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HighlightedWrittenReferenceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HighlightedDefinition)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HighlightedDefinitionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.CurrentStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? CurrentStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.CurrentStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? CurrentStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.CallReturn)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? CallReturnClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.CallReturnMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? CallReturnMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ActiveStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ActiveStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BreakpointStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BreakpointStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BreakpointStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BreakpointStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SelectedBreakpointStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SelectedBreakpointStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DisabledBreakpointStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DisabledBreakpointStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AdvancedBreakpointStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AdvancedBreakpointStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AdvancedBreakpointStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AdvancedBreakpointStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SelectedAdvancedBreakpointStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SelectedAdvancedBreakpointStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DisabledAdvancedBreakpointStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DisabledAdvancedBreakpointStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DisabledAdvancedBreakpointStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DisabledAdvancedBreakpointStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SelectedDisabledAdvancedBreakpointStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SelectedDisabledAdvancedBreakpointStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BreakpointWarningStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BreakpointWarningStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BreakpointWarningStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BreakpointWarningStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SelectedBreakpointWarningStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SelectedBreakpointWarningStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BreakpointErrorStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BreakpointErrorStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BreakpointErrorStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BreakpointErrorStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SelectedBreakpointErrorStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SelectedBreakpointErrorStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AdvancedBreakpointWarningStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AdvancedBreakpointWarningStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AdvancedBreakpointWarningStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AdvancedBreakpointWarningStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SelectedAdvancedBreakpointWarningStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SelectedAdvancedBreakpointWarningStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AdvancedBreakpointErrorStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AdvancedBreakpointErrorStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AdvancedBreakpointErrorStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AdvancedBreakpointErrorStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SelectedAdvancedBreakpointErrorStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SelectedAdvancedBreakpointErrorStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.TracepointStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? TracepointStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.TracepointStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? TracepointStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SelectedTracepointStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SelectedTracepointStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DisabledTracepointStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DisabledTracepointStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DisabledTracepointStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DisabledTracepointStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SelectedDisabledTracepointStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SelectedDisabledTracepointStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AdvancedTracepointStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AdvancedTracepointStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AdvancedTracepointStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AdvancedTracepointStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SelectedAdvancedTracepointStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SelectedAdvancedTracepointStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DisabledAdvancedTracepointStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DisabledAdvancedTracepointStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DisabledAdvancedTracepointStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DisabledAdvancedTracepointStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SelectedDisabledAdvancedTracepointStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SelectedDisabledAdvancedTracepointStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.TracepointWarningStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? TracepointWarningStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.TracepointWarningStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? TracepointWarningStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SelectedTracepointWarningStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SelectedTracepointWarningStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.TracepointErrorStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? TracepointErrorStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.TracepointErrorStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? TracepointErrorStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SelectedTracepointErrorStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SelectedTracepointErrorStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AdvancedTracepointWarningStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AdvancedTracepointWarningStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AdvancedTracepointWarningStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AdvancedTracepointWarningStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SelectedAdvancedTracepointWarningStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SelectedAdvancedTracepointWarningStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AdvancedTracepointErrorStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AdvancedTracepointErrorStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AdvancedTracepointErrorStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AdvancedTracepointErrorStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SelectedAdvancedTracepointErrorStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SelectedAdvancedTracepointErrorStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BookmarkName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BookmarkNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ActiveBookmarkName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ActiveBookmarkNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.CurrentLine)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? CurrentLineClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.CurrentLineNoFocus)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? CurrentLineNoFocusClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexOffset)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexOffsetClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexByte0)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexByte0ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexByte1)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexByte1ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexByteError)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexByteErrorClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexAscii)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexAsciiClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexCaret)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexCaretClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexInactiveCaret)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexInactiveCaretClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexSelection)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexSelectionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.GlyphMargin)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? GlyphMarginClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BraceMatching)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BraceMatchingClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.LineSeparator)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? LineSeparatorClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.FindMatchHighlightMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? FindMatchHighlightMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureNamespace)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureNamespaceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureType)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureTypeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureModule)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureModuleClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureValueType)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureValueTypeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureInterface)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureInterfaceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureMethod)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureMethodClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureAccessor)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureAccessorClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureAnonymousMethod)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureAnonymousMethodClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureConstructor)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureConstructorClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureDestructor)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureDestructorClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureOperator)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureOperatorClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureConditional)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureConditionalClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureLoop)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureLoopClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureProperty)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructurePropertyClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureEvent)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureEventClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureTry)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureTryClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureCatch)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureCatchClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureFilter)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureFilterClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureFinally)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureFinallyClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureFault)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureFaultClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureLock)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureLockClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureUsing)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureUsingClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureFixed)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureFixedClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureSwitch)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureSwitchClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureCase)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureCaseClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureLocalFunction)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureLocalFunctionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureOther)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureOtherClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureXml)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureXmlClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BlockStructureXaml)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? BlockStructureXamlClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.CompletionMatchHighlight)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? CompletionMatchHighlightClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.CompletionSuffix)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? CompletionSuffixClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SignatureHelpDocumentation)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SignatureHelpDocumentationClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SignatureHelpCurrentParameter)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SignatureHelpCurrentParameterClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SignatureHelpParameter)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SignatureHelpParameterClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SignatureHelpParameterDocumentation)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? SignatureHelpParameterDocumentationClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Url)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? UrlClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexPeDosHeader)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexPeDosHeaderClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexPeFileHeader)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexPeFileHeaderClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexPeOptionalHeader32)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexPeOptionalHeader32ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexPeOptionalHeader64)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexPeOptionalHeader64ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexPeSection)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexPeSectionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexPeSectionName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexPeSectionNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexCor20Header)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexCor20HeaderClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexStorageSignature)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexStorageSignatureClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexStorageHeader)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexStorageHeaderClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexStorageStream)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexStorageStreamClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexStorageStreamName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexStorageStreamNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexStorageStreamNameInvalid)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexStorageStreamNameInvalidClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexTablesStream)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexTablesStreamClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexTableName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexTableNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DocumentListMatchHighlight)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DocumentListMatchHighlightClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.GacMatchHighlight)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? GacMatchHighlightClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AppSettingsTreeViewNodeMatchHighlight)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AppSettingsTreeViewNodeMatchHighlightClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AppSettingsTextMatchHighlight)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AppSettingsTextMatchHighlightClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexCurrentLine)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexCurrentLineClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexCurrentLineNoFocus)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexCurrentLineNoFocusClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexInactiveSelectedText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexInactiveSelectedTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexColumnLine0)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexColumnLine0ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexColumnLine1)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexColumnLine1ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexColumnLineGroup0)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexColumnLineGroup0ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexColumnLineGroup1)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexColumnLineGroup1ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexHighlightedValuesColumn)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexHighlightedValuesColumnClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexHighlightedAsciiColumn)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexHighlightedAsciiColumnClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexGlyphMargin)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexGlyphMarginClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexCurrentValueCell)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexCurrentValueCellClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexCurrentAsciiCell)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexCurrentAsciiCellClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.OutputWindowText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? OutputWindowTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexFindMatchHighlightMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexFindMatchHighlightMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexToolTipServiceField0)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexToolTipServiceField0ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexToolTipServiceField1)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexToolTipServiceField1ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexToolTipServiceCurrentField)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? HexToolTipServiceCurrentFieldClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ListFindMatchHighlight)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? ListFindMatchHighlightClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebuggerValueChangedHighlight)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebuggerValueChangedHighlightClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugExceptionName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugExceptionNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugStowedExceptionName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugStowedExceptionNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugReturnValueName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugReturnValueNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugVariableName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugVariableNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugObjectIdName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugObjectIdNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebuggerDisplayAttributeEval)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebuggerDisplayAttributeEvalClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebuggerNoStringQuotesEval)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebuggerNoStringQuotesEvalClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugViewPropertyName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? DebugViewPropertyNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AsmComment)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AsmCommentClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AsmDirective)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AsmDirectiveClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AsmPrefix)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AsmPrefixClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AsmMnemonic)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AsmMnemonicClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AsmKeyword)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AsmKeywordClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AsmOperator)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AsmOperatorClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AsmPunctuation)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AsmPunctuationClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AsmNumber)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AsmNumberClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AsmRegister)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AsmRegisterClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AsmSelectorValue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AsmSelectorValueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AsmLabelAddress)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AsmLabelAddressClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AsmFunctionAddress)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AsmFunctionAddressClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AsmLabel)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AsmLabelClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AsmFunction)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AsmFunctionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AsmData)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AsmDataClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AsmAddress)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AsmAddressClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AsmHexBytes)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition? AsmHexBytesClassificationTypeDefinition;
#pragma warning restore CS0169

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Text)]
		[Name(ThemeClassificationTypeNameKeys.Text)]
		[UserVisible(true)]
		[Order(Before = Priority.Low)]
		sealed class Text : ThemeClassificationFormatDefinition {
			Text() : base(TextColor.Text) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Operator)]
		[Name(ThemeClassificationTypeNameKeys.Operator)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.PreprocessorKeyword)]
		sealed class Operator : ThemeClassificationFormatDefinition {
			Operator() : base(TextColor.Operator) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Punctuation)]
		[Name(ThemeClassificationTypeNameKeys.Punctuation)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class Punctuation : ThemeClassificationFormatDefinition {
			Punctuation() : base(TextColor.Punctuation) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Number)]
		[Name(ThemeClassificationTypeNameKeys.Number)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.Operator)]
		sealed class Number : ThemeClassificationFormatDefinition {
			Number() : base(TextColor.Number) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Comment)]
		[Name(ThemeClassificationTypeNameKeys.Comment)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.ExcludedCode)]
		sealed class Comment : ThemeClassificationFormatDefinition {
			Comment() : base(TextColor.Comment) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Keyword)]
		[Name(ThemeClassificationTypeNameKeys.Keyword)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.Literal)]
		sealed class Keyword : ThemeClassificationFormatDefinition {
			Keyword() : base(TextColor.Keyword) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.String)]
		[Name(ThemeClassificationTypeNameKeys.String)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage)]
		sealed class String : ThemeClassificationFormatDefinition {
			String() : base(TextColor.String) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.VerbatimString)]
		[Name(ThemeClassificationTypeNameKeys.VerbatimString)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class VerbatimString : ThemeClassificationFormatDefinition {
			VerbatimString() : base(TextColor.VerbatimString) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Char)]
		[Name(ThemeClassificationTypeNameKeys.Char)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class Char : ThemeClassificationFormatDefinition {
			Char() : base(TextColor.Char) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Namespace)]
		[Name(ThemeClassificationTypeNameKeys.Namespace)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Namespace : ThemeClassificationFormatDefinition {
			Namespace() : base(TextColor.Namespace) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Type)]
		[Name(ThemeClassificationTypeNameKeys.Type)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Type : ThemeClassificationFormatDefinition {
			Type() : base(TextColor.Type) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SealedType)]
		[Name(ThemeClassificationTypeNameKeys.SealedType)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class SealedType : ThemeClassificationFormatDefinition {
			SealedType() : base(TextColor.SealedType) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.StaticType)]
		[Name(ThemeClassificationTypeNameKeys.StaticType)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class StaticType : ThemeClassificationFormatDefinition {
			StaticType() : base(TextColor.StaticType) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Delegate)]
		[Name(ThemeClassificationTypeNameKeys.Delegate)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Delegate : ThemeClassificationFormatDefinition {
			Delegate() : base(TextColor.Delegate) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Enum)]
		[Name(ThemeClassificationTypeNameKeys.Enum)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Enum : ThemeClassificationFormatDefinition {
			Enum() : base(TextColor.Enum) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Interface)]
		[Name(ThemeClassificationTypeNameKeys.Interface)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Interface : ThemeClassificationFormatDefinition {
			Interface() : base(TextColor.Interface) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ValueType)]
		[Name(ThemeClassificationTypeNameKeys.ValueType)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class ValueType : ThemeClassificationFormatDefinition {
			ValueType() : base(TextColor.ValueType) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Module)]
		[Name(ThemeClassificationTypeNameKeys.Module)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Module : ThemeClassificationFormatDefinition {
			Module() : base(TextColor.Module) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.TypeGenericParameter)]
		[Name(ThemeClassificationTypeNameKeys.TypeGenericParameter)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class TypeGenericParameter : ThemeClassificationFormatDefinition {
			TypeGenericParameter() : base(TextColor.TypeGenericParameter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.MethodGenericParameter)]
		[Name(ThemeClassificationTypeNameKeys.MethodGenericParameter)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class MethodGenericParameter : ThemeClassificationFormatDefinition {
			MethodGenericParameter() : base(TextColor.MethodGenericParameter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InstanceMethod)]
		[Name(ThemeClassificationTypeNameKeys.InstanceMethod)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class InstanceMethod : ThemeClassificationFormatDefinition {
			InstanceMethod() : base(TextColor.InstanceMethod) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.StaticMethod)]
		[Name(ThemeClassificationTypeNameKeys.StaticMethod)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class StaticMethod : ThemeClassificationFormatDefinition {
			StaticMethod() : base(TextColor.StaticMethod) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ExtensionMethod)]
		[Name(ThemeClassificationTypeNameKeys.ExtensionMethod)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class ExtensionMethod : ThemeClassificationFormatDefinition {
			ExtensionMethod() : base(TextColor.ExtensionMethod) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InstanceField)]
		[Name(ThemeClassificationTypeNameKeys.InstanceField)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class InstanceField : ThemeClassificationFormatDefinition {
			InstanceField() : base(TextColor.InstanceField) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.EnumField)]
		[Name(ThemeClassificationTypeNameKeys.EnumField)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class EnumField : ThemeClassificationFormatDefinition {
			EnumField() : base(TextColor.EnumField) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.LiteralField)]
		[Name(ThemeClassificationTypeNameKeys.LiteralField)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class LiteralField : ThemeClassificationFormatDefinition {
			LiteralField() : base(TextColor.LiteralField) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.StaticField)]
		[Name(ThemeClassificationTypeNameKeys.StaticField)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class StaticField : ThemeClassificationFormatDefinition {
			StaticField() : base(TextColor.StaticField) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InstanceEvent)]
		[Name(ThemeClassificationTypeNameKeys.InstanceEvent)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class InstanceEvent : ThemeClassificationFormatDefinition {
			InstanceEvent() : base(TextColor.InstanceEvent) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.StaticEvent)]
		[Name(ThemeClassificationTypeNameKeys.StaticEvent)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class StaticEvent : ThemeClassificationFormatDefinition {
			StaticEvent() : base(TextColor.StaticEvent) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InstanceProperty)]
		[Name(ThemeClassificationTypeNameKeys.InstanceProperty)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class InstanceProperty : ThemeClassificationFormatDefinition {
			InstanceProperty() : base(TextColor.InstanceProperty) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.StaticProperty)]
		[Name(ThemeClassificationTypeNameKeys.StaticProperty)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class StaticProperty : ThemeClassificationFormatDefinition {
			StaticProperty() : base(TextColor.StaticProperty) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Local)]
		[Name(ThemeClassificationTypeNameKeys.Local)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Local : ThemeClassificationFormatDefinition {
			Local() : base(TextColor.Local) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Parameter)]
		[Name(ThemeClassificationTypeNameKeys.Parameter)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Parameter : ThemeClassificationFormatDefinition {
			Parameter() : base(TextColor.Parameter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.PreprocessorKeyword)]
		[Name(ThemeClassificationTypeNameKeys.PreprocessorKeyword)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.String)]
		sealed class PreprocessorKeyword : ThemeClassificationFormatDefinition {
			PreprocessorKeyword() : base(TextColor.PreprocessorKeyword) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.PreprocessorText)]
		[Name(ThemeClassificationTypeNameKeys.PreprocessorText)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class PreprocessorText : ThemeClassificationFormatDefinition {
			PreprocessorText() : base(TextColor.PreprocessorText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Label)]
		[Name(ThemeClassificationTypeNameKeys.Label)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Label : ThemeClassificationFormatDefinition {
			Label() : base(TextColor.Label) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.OpCode)]
		[Name(ThemeClassificationTypeNameKeys.OpCode)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class OpCode : ThemeClassificationFormatDefinition {
			OpCode() : base(TextColor.OpCode) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ILDirective)]
		[Name(ThemeClassificationTypeNameKeys.ILDirective)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class ILDirective : ThemeClassificationFormatDefinition {
			ILDirective() : base(TextColor.ILDirective) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ILModule)]
		[Name(ThemeClassificationTypeNameKeys.ILModule)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class ILModule : ThemeClassificationFormatDefinition {
			ILModule() : base(TextColor.ILModule) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ExcludedCode)]
		[Name(ThemeClassificationTypeNameKeys.ExcludedCode)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.Identifier)]
		sealed class ExcludedCode : ThemeClassificationFormatDefinition {
			ExcludedCode() : base(TextColor.ExcludedCode) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentAttributeName)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentAttributeName)]
		[UserVisible(true)]
		[Order(After = Priority.Default, Before = Priority.High)]
		sealed class XmlDocCommentAttributeName : ThemeClassificationFormatDefinition {
			XmlDocCommentAttributeName() : base(TextColor.XmlDocCommentAttributeName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentAttributeQuotes)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentAttributeQuotes)]
		[UserVisible(true)]
		[Order(After = Priority.Default, Before = Priority.High)]
		sealed class XmlDocCommentAttributeQuotes : ThemeClassificationFormatDefinition {
			XmlDocCommentAttributeQuotes() : base(TextColor.XmlDocCommentAttributeQuotes) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentAttributeValue)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentAttributeValue)]
		[UserVisible(true)]
		[Order(After = Priority.Default, Before = Priority.High)]
		sealed class XmlDocCommentAttributeValue : ThemeClassificationFormatDefinition {
			XmlDocCommentAttributeValue() : base(TextColor.XmlDocCommentAttributeValue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentCDataSection)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentCDataSection)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentCDataSection : ThemeClassificationFormatDefinition {
			XmlDocCommentCDataSection() : base(TextColor.XmlDocCommentCDataSection) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentComment)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentComment)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentComment : ThemeClassificationFormatDefinition {
			XmlDocCommentComment() : base(TextColor.XmlDocCommentComment) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentDelimiter)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentDelimiter)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentDelimiter : ThemeClassificationFormatDefinition {
			XmlDocCommentDelimiter() : base(TextColor.XmlDocCommentDelimiter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentEntityReference)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentEntityReference)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentEntityReference : ThemeClassificationFormatDefinition {
			XmlDocCommentEntityReference() : base(TextColor.XmlDocCommentEntityReference) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentName)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentName)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentName : ThemeClassificationFormatDefinition {
			XmlDocCommentName() : base(TextColor.XmlDocCommentName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentProcessingInstruction)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentProcessingInstruction)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentProcessingInstruction : ThemeClassificationFormatDefinition {
			XmlDocCommentProcessingInstruction() : base(TextColor.XmlDocCommentProcessingInstruction) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentText)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentText)]
		[UserVisible(true)]
		[Order(After = Priority.Default, Before = Priority.High)]
		sealed class XmlDocCommentText : ThemeClassificationFormatDefinition {
			XmlDocCommentText() : base(TextColor.XmlDocCommentText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralAttributeName)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralAttributeName)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralAttributeName : ThemeClassificationFormatDefinition {
			XmlLiteralAttributeName() : base(TextColor.XmlLiteralAttributeName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralAttributeQuotes)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralAttributeQuotes)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralAttributeQuotes : ThemeClassificationFormatDefinition {
			XmlLiteralAttributeQuotes() : base(TextColor.XmlLiteralAttributeQuotes) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralAttributeValue)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralAttributeValue)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralAttributeValue : ThemeClassificationFormatDefinition {
			XmlLiteralAttributeValue() : base(TextColor.XmlLiteralAttributeValue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralCDataSection)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralCDataSection)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralCDataSection : ThemeClassificationFormatDefinition {
			XmlLiteralCDataSection() : base(TextColor.XmlLiteralCDataSection) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralComment)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralComment)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralComment : ThemeClassificationFormatDefinition {
			XmlLiteralComment() : base(TextColor.XmlLiteralComment) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralDelimiter)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralDelimiter)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralDelimiter : ThemeClassificationFormatDefinition {
			XmlLiteralDelimiter() : base(TextColor.XmlLiteralDelimiter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralEmbeddedExpression)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralEmbeddedExpression)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralEmbeddedExpression : ThemeClassificationFormatDefinition {
			XmlLiteralEmbeddedExpression() : base(TextColor.XmlLiteralEmbeddedExpression) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralEntityReference)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralEntityReference)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralEntityReference : ThemeClassificationFormatDefinition {
			XmlLiteralEntityReference() : base(TextColor.XmlLiteralEntityReference) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralName)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralName)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralName : ThemeClassificationFormatDefinition {
			XmlLiteralName() : base(TextColor.XmlLiteralName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralProcessingInstruction)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralProcessingInstruction)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralProcessingInstruction : ThemeClassificationFormatDefinition {
			XmlLiteralProcessingInstruction() : base(TextColor.XmlLiteralProcessingInstruction) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralText)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralText)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralText : ThemeClassificationFormatDefinition {
			XmlLiteralText() : base(TextColor.XmlLiteralText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlAttribute)]
		[Name(ThemeClassificationTypeNameKeys.XmlAttribute)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlAttribute : ThemeClassificationFormatDefinition {
			XmlAttribute() : base(TextColor.XmlAttribute) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlAttributeQuotes)]
		[Name(ThemeClassificationTypeNameKeys.XmlAttributeQuotes)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlAttributeQuotes : ThemeClassificationFormatDefinition {
			XmlAttributeQuotes() : base(TextColor.XmlAttributeQuotes) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlAttributeValue)]
		[Name(ThemeClassificationTypeNameKeys.XmlAttributeValue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlAttributeValue : ThemeClassificationFormatDefinition {
			XmlAttributeValue() : base(TextColor.XmlAttributeValue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlCDataSection)]
		[Name(ThemeClassificationTypeNameKeys.XmlCDataSection)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlCDataSection : ThemeClassificationFormatDefinition {
			XmlCDataSection() : base(TextColor.XmlCDataSection) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlComment)]
		[Name(ThemeClassificationTypeNameKeys.XmlComment)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlComment : ThemeClassificationFormatDefinition {
			XmlComment() : base(TextColor.XmlComment) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDelimiter)]
		[Name(ThemeClassificationTypeNameKeys.XmlDelimiter)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDelimiter : ThemeClassificationFormatDefinition {
			XmlDelimiter() : base(TextColor.XmlDelimiter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlKeyword)]
		[Name(ThemeClassificationTypeNameKeys.XmlKeyword)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlKeyword : ThemeClassificationFormatDefinition {
			XmlKeyword() : base(TextColor.XmlKeyword) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlName)]
		[Name(ThemeClassificationTypeNameKeys.XmlName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlName : ThemeClassificationFormatDefinition {
			XmlName() : base(TextColor.XmlName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlProcessingInstruction)]
		[Name(ThemeClassificationTypeNameKeys.XmlProcessingInstruction)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlProcessingInstruction : ThemeClassificationFormatDefinition {
			XmlProcessingInstruction() : base(TextColor.XmlProcessingInstruction) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlText)]
		[Name(ThemeClassificationTypeNameKeys.XmlText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlText : ThemeClassificationFormatDefinition {
			XmlText() : base(TextColor.XmlText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XamlAttribute)]
		[Name(ThemeClassificationTypeNameKeys.XamlAttribute)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XamlAttribute : ThemeClassificationFormatDefinition {
			XamlAttribute() : base(TextColor.XamlAttribute) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XamlAttributeQuotes)]
		[Name(ThemeClassificationTypeNameKeys.XamlAttributeQuotes)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XamlAttributeQuotes : ThemeClassificationFormatDefinition {
			XamlAttributeQuotes() : base(TextColor.XamlAttributeQuotes) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XamlAttributeValue)]
		[Name(ThemeClassificationTypeNameKeys.XamlAttributeValue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XamlAttributeValue : ThemeClassificationFormatDefinition {
			XamlAttributeValue() : base(TextColor.XamlAttributeValue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XamlCDataSection)]
		[Name(ThemeClassificationTypeNameKeys.XamlCDataSection)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XamlCDataSection : ThemeClassificationFormatDefinition {
			XamlCDataSection() : base(TextColor.XamlCDataSection) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XamlComment)]
		[Name(ThemeClassificationTypeNameKeys.XamlComment)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XamlComment : ThemeClassificationFormatDefinition {
			XamlComment() : base(TextColor.XamlComment) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XamlDelimiter)]
		[Name(ThemeClassificationTypeNameKeys.XamlDelimiter)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XamlDelimiter : ThemeClassificationFormatDefinition {
			XamlDelimiter() : base(TextColor.XamlDelimiter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XamlKeyword)]
		[Name(ThemeClassificationTypeNameKeys.XamlKeyword)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XamlKeyword : ThemeClassificationFormatDefinition {
			XamlKeyword() : base(TextColor.XamlKeyword) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XamlMarkupExtensionClass)]
		[Name(ThemeClassificationTypeNameKeys.XamlMarkupExtensionClass)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XamlMarkupExtensionClass : ThemeClassificationFormatDefinition {
			XamlMarkupExtensionClass() : base(TextColor.XamlMarkupExtensionClass) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XamlMarkupExtensionParameterName)]
		[Name(ThemeClassificationTypeNameKeys.XamlMarkupExtensionParameterName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XamlMarkupExtensionParameterName : ThemeClassificationFormatDefinition {
			XamlMarkupExtensionParameterName() : base(TextColor.XamlMarkupExtensionParameterName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XamlMarkupExtensionParameterValue)]
		[Name(ThemeClassificationTypeNameKeys.XamlMarkupExtensionParameterValue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XamlMarkupExtensionParameterValue : ThemeClassificationFormatDefinition {
			XamlMarkupExtensionParameterValue() : base(TextColor.XamlMarkupExtensionParameterValue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XamlName)]
		[Name(ThemeClassificationTypeNameKeys.XamlName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XamlName : ThemeClassificationFormatDefinition {
			XamlName() : base(TextColor.XamlName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XamlProcessingInstruction)]
		[Name(ThemeClassificationTypeNameKeys.XamlProcessingInstruction)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XamlProcessingInstruction : ThemeClassificationFormatDefinition {
			XamlProcessingInstruction() : base(TextColor.XamlProcessingInstruction) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XamlText)]
		[Name(ThemeClassificationTypeNameKeys.XamlText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XamlText : ThemeClassificationFormatDefinition {
			XamlText() : base(TextColor.XamlText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipHeader)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipHeader)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipHeader : ThemeClassificationFormatDefinition {
			XmlDocToolTipHeader() : base(TextColor.XmlDocToolTipHeader) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Assembly)]
		[Name(ThemeClassificationTypeNameKeys.Assembly)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Assembly : ThemeClassificationFormatDefinition {
			Assembly() : base(TextColor.Assembly) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AssemblyExe)]
		[Name(ThemeClassificationTypeNameKeys.AssemblyExe)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class AssemblyExe : ThemeClassificationFormatDefinition {
			AssemblyExe() : base(TextColor.AssemblyExe) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AssemblyModule)]
		[Name(ThemeClassificationTypeNameKeys.AssemblyModule)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AssemblyModule : ThemeClassificationFormatDefinition {
			AssemblyModule() : base(TextColor.AssemblyModule) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DirectoryPart)]
		[Name(ThemeClassificationTypeNameKeys.DirectoryPart)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class DirectoryPart : ThemeClassificationFormatDefinition {
			DirectoryPart() : base(TextColor.DirectoryPart) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.FileNameNoExtension)]
		[Name(ThemeClassificationTypeNameKeys.FileNameNoExtension)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class FileNameNoExtension : ThemeClassificationFormatDefinition {
			FileNameNoExtension() : base(TextColor.FileNameNoExtension) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.FileExtension)]
		[Name(ThemeClassificationTypeNameKeys.FileExtension)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class FileExtension : ThemeClassificationFormatDefinition {
			FileExtension() : base(TextColor.FileExtension) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Error)]
		[Name(ThemeClassificationTypeNameKeys.Error)]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class Error : ThemeClassificationFormatDefinition {
			Error() : base(TextColor.Error) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ToStringEval)]
		[Name(ThemeClassificationTypeNameKeys.ToStringEval)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ToStringEval : ThemeClassificationFormatDefinition {
			ToStringEval() : base(TextColor.ToStringEval) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplPrompt1)]
		[Name(ThemeClassificationTypeNameKeys.ReplPrompt1)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplPrompt1 : ThemeClassificationFormatDefinition {
			ReplPrompt1() : base(TextColor.ReplPrompt1) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplPrompt2)]
		[Name(ThemeClassificationTypeNameKeys.ReplPrompt2)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplPrompt2 : ThemeClassificationFormatDefinition {
			ReplPrompt2() : base(TextColor.ReplPrompt2) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplOutputText)]
		[Name(ThemeClassificationTypeNameKeys.ReplOutputText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplOutputText : ThemeClassificationFormatDefinition {
			ReplOutputText() : base(TextColor.ReplOutputText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplScriptOutputText)]
		[Name(ThemeClassificationTypeNameKeys.ReplScriptOutputText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplScriptOutputText : ThemeClassificationFormatDefinition {
			ReplScriptOutputText() : base(TextColor.ReplScriptOutputText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Black)]
		[Name(ThemeClassificationTypeNameKeys.Black)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Black : ThemeClassificationFormatDefinition {
			Black() : base(TextColor.Black) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Blue)]
		[Name(ThemeClassificationTypeNameKeys.Blue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Blue : ThemeClassificationFormatDefinition {
			Blue() : base(TextColor.Blue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Cyan)]
		[Name(ThemeClassificationTypeNameKeys.Cyan)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Cyan : ThemeClassificationFormatDefinition {
			Cyan() : base(TextColor.Cyan) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkBlue)]
		[Name(ThemeClassificationTypeNameKeys.DarkBlue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkBlue : ThemeClassificationFormatDefinition {
			DarkBlue() : base(TextColor.DarkBlue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkCyan)]
		[Name(ThemeClassificationTypeNameKeys.DarkCyan)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkCyan : ThemeClassificationFormatDefinition {
			DarkCyan() : base(TextColor.DarkCyan) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkGray)]
		[Name(ThemeClassificationTypeNameKeys.DarkGray)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkGray : ThemeClassificationFormatDefinition {
			DarkGray() : base(TextColor.DarkGray) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkGreen)]
		[Name(ThemeClassificationTypeNameKeys.DarkGreen)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkGreen : ThemeClassificationFormatDefinition {
			DarkGreen() : base(TextColor.DarkGreen) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkMagenta)]
		[Name(ThemeClassificationTypeNameKeys.DarkMagenta)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkMagenta : ThemeClassificationFormatDefinition {
			DarkMagenta() : base(TextColor.DarkMagenta) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkRed)]
		[Name(ThemeClassificationTypeNameKeys.DarkRed)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkRed : ThemeClassificationFormatDefinition {
			DarkRed() : base(TextColor.DarkRed) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkYellow)]
		[Name(ThemeClassificationTypeNameKeys.DarkYellow)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkYellow : ThemeClassificationFormatDefinition {
			DarkYellow() : base(TextColor.DarkYellow) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Gray)]
		[Name(ThemeClassificationTypeNameKeys.Gray)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Gray : ThemeClassificationFormatDefinition {
			Gray() : base(TextColor.Gray) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Green)]
		[Name(ThemeClassificationTypeNameKeys.Green)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Green : ThemeClassificationFormatDefinition {
			Green() : base(TextColor.Green) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Magenta)]
		[Name(ThemeClassificationTypeNameKeys.Magenta)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Magenta : ThemeClassificationFormatDefinition {
			Magenta() : base(TextColor.Magenta) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Red)]
		[Name(ThemeClassificationTypeNameKeys.Red)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Red : ThemeClassificationFormatDefinition {
			Red() : base(TextColor.Red) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.White)]
		[Name(ThemeClassificationTypeNameKeys.White)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class White : ThemeClassificationFormatDefinition {
			White() : base(TextColor.White) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Yellow)]
		[Name(ThemeClassificationTypeNameKeys.Yellow)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Yellow : ThemeClassificationFormatDefinition {
			Yellow() : base(TextColor.Yellow) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvBlack)]
		[Name(ThemeClassificationTypeNameKeys.InvBlack)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvBlack : ThemeClassificationFormatDefinition {
			InvBlack() : base(TextColor.InvBlack) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvBlue)]
		[Name(ThemeClassificationTypeNameKeys.InvBlue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvBlue : ThemeClassificationFormatDefinition {
			InvBlue() : base(TextColor.InvBlue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvCyan)]
		[Name(ThemeClassificationTypeNameKeys.InvCyan)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvCyan : ThemeClassificationFormatDefinition {
			InvCyan() : base(TextColor.InvCyan) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkBlue)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkBlue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkBlue : ThemeClassificationFormatDefinition {
			InvDarkBlue() : base(TextColor.InvDarkBlue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkCyan)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkCyan)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkCyan : ThemeClassificationFormatDefinition {
			InvDarkCyan() : base(TextColor.InvDarkCyan) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkGray)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkGray)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkGray : ThemeClassificationFormatDefinition {
			InvDarkGray() : base(TextColor.InvDarkGray) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkGreen)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkGreen)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkGreen : ThemeClassificationFormatDefinition {
			InvDarkGreen() : base(TextColor.InvDarkGreen) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkMagenta)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkMagenta)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkMagenta : ThemeClassificationFormatDefinition {
			InvDarkMagenta() : base(TextColor.InvDarkMagenta) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkRed)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkRed)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkRed : ThemeClassificationFormatDefinition {
			InvDarkRed() : base(TextColor.InvDarkRed) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkYellow)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkYellow)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkYellow : ThemeClassificationFormatDefinition {
			InvDarkYellow() : base(TextColor.InvDarkYellow) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvGray)]
		[Name(ThemeClassificationTypeNameKeys.InvGray)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvGray : ThemeClassificationFormatDefinition {
			InvGray() : base(TextColor.InvGray) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvGreen)]
		[Name(ThemeClassificationTypeNameKeys.InvGreen)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvGreen : ThemeClassificationFormatDefinition {
			InvGreen() : base(TextColor.InvGreen) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvMagenta)]
		[Name(ThemeClassificationTypeNameKeys.InvMagenta)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvMagenta : ThemeClassificationFormatDefinition {
			InvMagenta() : base(TextColor.InvMagenta) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvRed)]
		[Name(ThemeClassificationTypeNameKeys.InvRed)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvRed : ThemeClassificationFormatDefinition {
			InvRed() : base(TextColor.InvRed) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvWhite)]
		[Name(ThemeClassificationTypeNameKeys.InvWhite)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvWhite : ThemeClassificationFormatDefinition {
			InvWhite() : base(TextColor.InvWhite) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvYellow)]
		[Name(ThemeClassificationTypeNameKeys.InvYellow)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvYellow : ThemeClassificationFormatDefinition {
			InvYellow() : base(TextColor.InvYellow) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogExceptionHandled)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogExceptionHandled)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogExceptionHandled : ThemeClassificationFormatDefinition {
			DebugLogExceptionHandled() : base(TextColor.DebugLogExceptionHandled) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogExceptionUnhandled)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogExceptionUnhandled)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogExceptionUnhandled : ThemeClassificationFormatDefinition {
			DebugLogExceptionUnhandled() : base(TextColor.DebugLogExceptionUnhandled) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogStepFiltering)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogStepFiltering)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogStepFiltering : ThemeClassificationFormatDefinition {
			DebugLogStepFiltering() : base(TextColor.DebugLogStepFiltering) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogLoadModule)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogLoadModule)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogLoadModule : ThemeClassificationFormatDefinition {
			DebugLogLoadModule() : base(TextColor.DebugLogLoadModule) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogUnloadModule)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogUnloadModule)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogUnloadModule : ThemeClassificationFormatDefinition {
			DebugLogUnloadModule() : base(TextColor.DebugLogUnloadModule) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogExitProcess)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogExitProcess)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogExitProcess : ThemeClassificationFormatDefinition {
			DebugLogExitProcess() : base(TextColor.DebugLogExitProcess) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogExitThread)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogExitThread)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogExitThread : ThemeClassificationFormatDefinition {
			DebugLogExitThread() : base(TextColor.DebugLogExitThread) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogProgramOutput)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogProgramOutput)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogProgramOutput : ThemeClassificationFormatDefinition {
			DebugLogProgramOutput() : base(TextColor.DebugLogProgramOutput) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogMDA)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogMDA)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogMDA : ThemeClassificationFormatDefinition {
			DebugLogMDA() : base(TextColor.DebugLogMDA) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogTimestamp)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogTimestamp)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogTimestamp : ThemeClassificationFormatDefinition {
			DebugLogTimestamp() : base(TextColor.DebugLogTimestamp) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogTrace)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogTrace)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogTrace : ThemeClassificationFormatDefinition {
			DebugLogTrace() : base(TextColor.DebugLogTrace) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogExtensionMessage)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogExtensionMessage)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogExtensionMessage : ThemeClassificationFormatDefinition {
			DebugLogExtensionMessage() : base(TextColor.DebugLogExtensionMessage) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.LineNumber)]
		[Name(ThemeClassificationTypeNameKeys.LineNumber)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class LineNumber : ThemeClassificationFormatDefinition {
			LineNumber() : base(TextColor.LineNumber) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplLineNumberInput1)]
		[Name(ThemeClassificationTypeNameKeys.ReplLineNumberInput1)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplLineNumberInput1 : ThemeClassificationFormatDefinition {
			ReplLineNumberInput1() : base(TextColor.ReplLineNumberInput1) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplLineNumberInput2)]
		[Name(ThemeClassificationTypeNameKeys.ReplLineNumberInput2)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplLineNumberInput2 : ThemeClassificationFormatDefinition {
			ReplLineNumberInput2() : base(TextColor.ReplLineNumberInput2) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplLineNumberOutput)]
		[Name(ThemeClassificationTypeNameKeys.ReplLineNumberOutput)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplLineNumberOutput : ThemeClassificationFormatDefinition {
			ReplLineNumberOutput() : base(TextColor.ReplLineNumberOutput) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.VisibleWhitespace)]
		[Name(ThemeClassificationTypeNameKeys.VisibleWhitespace)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class VisibleWhitespace : ThemeClassificationFormatDefinition {
			VisibleWhitespace() : base(TextColor.VisibleWhitespace) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SelectedText)]
		[Name(ThemeClassificationTypeNameKeys.SelectedText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class SelectedText : ThemeClassificationFormatDefinition {
			SelectedText() : base(TextColor.SelectedText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InactiveSelectedText)]
		[Name(ThemeClassificationTypeNameKeys.InactiveSelectedText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InactiveSelectedText : ThemeClassificationFormatDefinition {
			InactiveSelectedText() : base(TextColor.InactiveSelectedText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HighlightedReference)]
		[Name(ThemeClassificationTypeNameKeys.HighlightedReference)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HighlightedReference : ThemeMarkerFormatDefinition {
			HighlightedReference() : base(TextColor.HighlightedReference) => ZOrder = TextMarkerServiceZIndexes.HighlightedReference;
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HighlightedWrittenReference)]
		[Name(ThemeClassificationTypeNameKeys.HighlightedWrittenReference)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HighlightedWrittenReference : ThemeMarkerFormatDefinition {
			HighlightedWrittenReference() : base(TextColor.HighlightedWrittenReference) => ZOrder = TextMarkerServiceZIndexes.HighlightedWrittenReference;
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HighlightedDefinition)]
		[Name(ThemeClassificationTypeNameKeys.HighlightedDefinition)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HighlightedDefinition : ThemeMarkerFormatDefinition {
			HighlightedDefinition() : base(TextColor.HighlightedDefinition) => ZOrder = TextMarkerServiceZIndexes.HighlightedDefinition;
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.CurrentStatement)]
		[Name(ThemeClassificationTypeNameKeys.CurrentStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class CurrentStatement : ThemeClassificationFormatDefinition {
			CurrentStatement() : base(TextColor.CurrentStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.CurrentStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.CurrentStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class CurrentStatementMarker : ThemeMarkerFormatDefinition {
			CurrentStatementMarker() : base(TextColor.CurrentStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.CallReturn)]
		[Name(ThemeClassificationTypeNameKeys.CallReturn)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class CallReturn : ThemeClassificationFormatDefinition {
			CallReturn() : base(TextColor.CallReturn) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.CallReturnMarker)]
		[Name(ThemeClassificationTypeNameKeys.CallReturnMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class CallReturnMarker : ThemeMarkerFormatDefinition {
			CallReturnMarker() : base(TextColor.CallReturnMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ActiveStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.ActiveStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ActiveStatementMarker : ThemeMarkerFormatDefinition {
			ActiveStatementMarker() : base(TextColor.ActiveStatementMarker) => ZOrder = TextMarkerServiceZIndexes.ActiveStatement;
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BreakpointStatement)]
		[Name(ThemeClassificationTypeNameKeys.BreakpointStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class BreakpointStatement : ThemeClassificationFormatDefinition {
			BreakpointStatement() : base(TextColor.BreakpointStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BreakpointStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.BreakpointStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BreakpointStatementMarker : ThemeMarkerFormatDefinition {
			BreakpointStatementMarker() : base(TextColor.BreakpointStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SelectedBreakpointStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.SelectedBreakpointStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class SelectedBreakpointStatementMarker : ThemeMarkerFormatDefinition {
			SelectedBreakpointStatementMarker() : base(TextColor.SelectedBreakpointStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DisabledBreakpointStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.DisabledBreakpointStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DisabledBreakpointStatementMarker : ThemeMarkerFormatDefinition {
			DisabledBreakpointStatementMarker() : base(TextColor.DisabledBreakpointStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AdvancedBreakpointStatement)]
		[Name(ThemeClassificationTypeNameKeys.AdvancedBreakpointStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class AdvancedBreakpointStatement : ThemeClassificationFormatDefinition {
			AdvancedBreakpointStatement() : base(TextColor.AdvancedBreakpointStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AdvancedBreakpointStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.AdvancedBreakpointStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class AdvancedBreakpointStatementMarker : ThemeMarkerFormatDefinition {
			AdvancedBreakpointStatementMarker() : base(TextColor.AdvancedBreakpointStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SelectedAdvancedBreakpointStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.SelectedAdvancedBreakpointStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class SelectedAdvancedBreakpointStatementMarker : ThemeMarkerFormatDefinition {
			SelectedAdvancedBreakpointStatementMarker() : base(TextColor.SelectedAdvancedBreakpointStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DisabledAdvancedBreakpointStatement)]
		[Name(ThemeClassificationTypeNameKeys.DisabledAdvancedBreakpointStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class DisabledAdvancedBreakpointStatement : ThemeClassificationFormatDefinition {
			DisabledAdvancedBreakpointStatement() : base(TextColor.DisabledAdvancedBreakpointStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DisabledAdvancedBreakpointStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.DisabledAdvancedBreakpointStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DisabledAdvancedBreakpointStatementMarker : ThemeMarkerFormatDefinition {
			DisabledAdvancedBreakpointStatementMarker() : base(TextColor.DisabledAdvancedBreakpointStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SelectedDisabledAdvancedBreakpointStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.SelectedDisabledAdvancedBreakpointStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class SelectedDisabledAdvancedBreakpointStatementMarker : ThemeMarkerFormatDefinition {
			SelectedDisabledAdvancedBreakpointStatementMarker() : base(TextColor.SelectedDisabledAdvancedBreakpointStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BreakpointWarningStatement)]
		[Name(ThemeClassificationTypeNameKeys.BreakpointWarningStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class BreakpointWarningStatement : ThemeClassificationFormatDefinition {
			BreakpointWarningStatement() : base(TextColor.BreakpointWarningStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BreakpointWarningStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.BreakpointWarningStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BreakpointWarningStatementMarker : ThemeMarkerFormatDefinition {
			BreakpointWarningStatementMarker() : base(TextColor.BreakpointWarningStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SelectedBreakpointWarningStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.SelectedBreakpointWarningStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class SelectedBreakpointWarningStatementMarker : ThemeMarkerFormatDefinition {
			SelectedBreakpointWarningStatementMarker() : base(TextColor.SelectedBreakpointWarningStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BreakpointErrorStatement)]
		[Name(ThemeClassificationTypeNameKeys.BreakpointErrorStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class BreakpointErrorStatement : ThemeClassificationFormatDefinition {
			BreakpointErrorStatement() : base(TextColor.BreakpointErrorStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BreakpointErrorStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.BreakpointErrorStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BreakpointErrorStatementMarker : ThemeMarkerFormatDefinition {
			BreakpointErrorStatementMarker() : base(TextColor.BreakpointErrorStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SelectedBreakpointErrorStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.SelectedBreakpointErrorStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class SelectedBreakpointErrorStatementMarker : ThemeMarkerFormatDefinition {
			SelectedBreakpointErrorStatementMarker() : base(TextColor.SelectedBreakpointErrorStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AdvancedBreakpointWarningStatement)]
		[Name(ThemeClassificationTypeNameKeys.AdvancedBreakpointWarningStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class AdvancedBreakpointWarningStatement : ThemeClassificationFormatDefinition {
			AdvancedBreakpointWarningStatement() : base(TextColor.AdvancedBreakpointWarningStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AdvancedBreakpointWarningStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.AdvancedBreakpointWarningStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class AdvancedBreakpointWarningStatementMarker : ThemeMarkerFormatDefinition {
			AdvancedBreakpointWarningStatementMarker() : base(TextColor.AdvancedBreakpointWarningStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SelectedAdvancedBreakpointWarningStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.SelectedAdvancedBreakpointWarningStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class SelectedAdvancedBreakpointWarningStatementMarker : ThemeMarkerFormatDefinition {
			SelectedAdvancedBreakpointWarningStatementMarker() : base(TextColor.SelectedAdvancedBreakpointWarningStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AdvancedBreakpointErrorStatement)]
		[Name(ThemeClassificationTypeNameKeys.AdvancedBreakpointErrorStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class AdvancedBreakpointErrorStatement : ThemeClassificationFormatDefinition {
			AdvancedBreakpointErrorStatement() : base(TextColor.AdvancedBreakpointErrorStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AdvancedBreakpointErrorStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.AdvancedBreakpointErrorStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class AdvancedBreakpointErrorStatementMarker : ThemeMarkerFormatDefinition {
			AdvancedBreakpointErrorStatementMarker() : base(TextColor.AdvancedBreakpointErrorStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SelectedAdvancedBreakpointErrorStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.SelectedAdvancedBreakpointErrorStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class SelectedAdvancedBreakpointErrorStatementMarker : ThemeMarkerFormatDefinition {
			SelectedAdvancedBreakpointErrorStatementMarker() : base(TextColor.SelectedAdvancedBreakpointErrorStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.TracepointStatement)]
		[Name(ThemeClassificationTypeNameKeys.TracepointStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class TracepointStatement : ThemeClassificationFormatDefinition {
			TracepointStatement() : base(TextColor.TracepointStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.TracepointStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.TracepointStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class TracepointStatementMarker : ThemeMarkerFormatDefinition {
			TracepointStatementMarker() : base(TextColor.TracepointStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SelectedTracepointStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.SelectedTracepointStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class SelectedTracepointStatementMarker : ThemeMarkerFormatDefinition {
			SelectedTracepointStatementMarker() : base(TextColor.SelectedTracepointStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DisabledTracepointStatement)]
		[Name(ThemeClassificationTypeNameKeys.DisabledTracepointStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class DisabledTracepointStatement : ThemeClassificationFormatDefinition {
			DisabledTracepointStatement() : base(TextColor.DisabledTracepointStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DisabledTracepointStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.DisabledTracepointStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DisabledTracepointStatementMarker : ThemeMarkerFormatDefinition {
			DisabledTracepointStatementMarker() : base(TextColor.DisabledTracepointStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SelectedDisabledTracepointStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.SelectedDisabledTracepointStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class SelectedDisabledTracepointStatementMarker : ThemeMarkerFormatDefinition {
			SelectedDisabledTracepointStatementMarker() : base(TextColor.SelectedDisabledTracepointStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AdvancedTracepointStatement)]
		[Name(ThemeClassificationTypeNameKeys.AdvancedTracepointStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class AdvancedTracepointStatement : ThemeClassificationFormatDefinition {
			AdvancedTracepointStatement() : base(TextColor.AdvancedTracepointStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AdvancedTracepointStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.AdvancedTracepointStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class AdvancedTracepointStatementMarker : ThemeMarkerFormatDefinition {
			AdvancedTracepointStatementMarker() : base(TextColor.AdvancedTracepointStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SelectedAdvancedTracepointStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.SelectedAdvancedTracepointStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class SelectedAdvancedTracepointStatementMarker : ThemeMarkerFormatDefinition {
			SelectedAdvancedTracepointStatementMarker() : base(TextColor.SelectedAdvancedTracepointStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DisabledAdvancedTracepointStatement)]
		[Name(ThemeClassificationTypeNameKeys.DisabledAdvancedTracepointStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class DisabledAdvancedTracepointStatement : ThemeClassificationFormatDefinition {
			DisabledAdvancedTracepointStatement() : base(TextColor.DisabledAdvancedTracepointStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DisabledAdvancedTracepointStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.DisabledAdvancedTracepointStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DisabledAdvancedTracepointStatementMarker : ThemeMarkerFormatDefinition {
			DisabledAdvancedTracepointStatementMarker() : base(TextColor.DisabledAdvancedTracepointStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SelectedDisabledAdvancedTracepointStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.SelectedDisabledAdvancedTracepointStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class SelectedDisabledAdvancedTracepointStatementMarker : ThemeMarkerFormatDefinition {
			SelectedDisabledAdvancedTracepointStatementMarker() : base(TextColor.SelectedDisabledAdvancedTracepointStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.TracepointWarningStatement)]
		[Name(ThemeClassificationTypeNameKeys.TracepointWarningStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class TracepointWarningStatement : ThemeClassificationFormatDefinition {
			TracepointWarningStatement() : base(TextColor.TracepointWarningStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.TracepointWarningStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.TracepointWarningStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class TracepointWarningStatementMarker : ThemeMarkerFormatDefinition {
			TracepointWarningStatementMarker() : base(TextColor.TracepointWarningStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SelectedTracepointWarningStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.SelectedTracepointWarningStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class SelectedTracepointWarningStatementMarker : ThemeMarkerFormatDefinition {
			SelectedTracepointWarningStatementMarker() : base(TextColor.SelectedTracepointWarningStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.TracepointErrorStatement)]
		[Name(ThemeClassificationTypeNameKeys.TracepointErrorStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class TracepointErrorStatement : ThemeClassificationFormatDefinition {
			TracepointErrorStatement() : base(TextColor.TracepointErrorStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.TracepointErrorStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.TracepointErrorStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class TracepointErrorStatementMarker : ThemeMarkerFormatDefinition {
			TracepointErrorStatementMarker() : base(TextColor.TracepointErrorStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SelectedTracepointErrorStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.SelectedTracepointErrorStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class SelectedTracepointErrorStatementMarker : ThemeMarkerFormatDefinition {
			SelectedTracepointErrorStatementMarker() : base(TextColor.SelectedTracepointErrorStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AdvancedTracepointWarningStatement)]
		[Name(ThemeClassificationTypeNameKeys.AdvancedTracepointWarningStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class AdvancedTracepointWarningStatement : ThemeClassificationFormatDefinition {
			AdvancedTracepointWarningStatement() : base(TextColor.AdvancedTracepointWarningStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AdvancedTracepointWarningStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.AdvancedTracepointWarningStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class AdvancedTracepointWarningStatementMarker : ThemeMarkerFormatDefinition {
			AdvancedTracepointWarningStatementMarker() : base(TextColor.AdvancedTracepointWarningStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SelectedAdvancedTracepointWarningStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.SelectedAdvancedTracepointWarningStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class SelectedAdvancedTracepointWarningStatementMarker : ThemeMarkerFormatDefinition {
			SelectedAdvancedTracepointWarningStatementMarker() : base(TextColor.SelectedAdvancedTracepointWarningStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AdvancedTracepointErrorStatement)]
		[Name(ThemeClassificationTypeNameKeys.AdvancedTracepointErrorStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class AdvancedTracepointErrorStatement : ThemeClassificationFormatDefinition {
			AdvancedTracepointErrorStatement() : base(TextColor.AdvancedTracepointErrorStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AdvancedTracepointErrorStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.AdvancedTracepointErrorStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class AdvancedTracepointErrorStatementMarker : ThemeMarkerFormatDefinition {
			AdvancedTracepointErrorStatementMarker() : base(TextColor.AdvancedTracepointErrorStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SelectedAdvancedTracepointErrorStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.SelectedAdvancedTracepointErrorStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class SelectedAdvancedTracepointErrorStatementMarker : ThemeMarkerFormatDefinition {
			SelectedAdvancedTracepointErrorStatementMarker() : base(TextColor.SelectedAdvancedTracepointErrorStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BookmarkName)]
		[Name(ThemeClassificationTypeNameKeys.BookmarkName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BookmarkName : ThemeClassificationFormatDefinition {
			BookmarkName() : base(TextColor.BookmarkName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ActiveBookmarkName)]
		[Name(ThemeClassificationTypeNameKeys.ActiveBookmarkName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ActiveBookmarkName : ThemeClassificationFormatDefinition {
			ActiveBookmarkName() : base(TextColor.ActiveBookmarkName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.CurrentLine)]
		[Name(ThemeClassificationTypeNameKeys.CurrentLine)]
		[UserVisible(true)]
		[Order(Before = Priority.Default)]
		sealed class CurrentLine : ThemeClassificationFormatDefinition {
			CurrentLine() : base(TextColor.CurrentLine) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.CurrentLineNoFocus)]
		[Name(ThemeClassificationTypeNameKeys.CurrentLineNoFocus)]
		[UserVisible(true)]
		[Order(Before = Priority.Default)]
		sealed class CurrentLineNoFocus : ThemeClassificationFormatDefinition {
			CurrentLineNoFocus() : base(TextColor.CurrentLineNoFocus) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexText)]
		[Name(ThemeClassificationTypeNameKeys.HexText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexText : ThemeClassificationFormatDefinition {
			HexText() : base(TextColor.HexText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexOffset)]
		[Name(ThemeClassificationTypeNameKeys.HexOffset)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexOffset : ThemeClassificationFormatDefinition {
			HexOffset() : base(TextColor.HexOffset) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexByte0)]
		[Name(ThemeClassificationTypeNameKeys.HexByte0)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexByte0 : ThemeClassificationFormatDefinition {
			HexByte0() : base(TextColor.HexByte0) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexByte1)]
		[Name(ThemeClassificationTypeNameKeys.HexByte1)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexByte1 : ThemeClassificationFormatDefinition {
			HexByte1() : base(TextColor.HexByte1) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexByteError)]
		[Name(ThemeClassificationTypeNameKeys.HexByteError)]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class HexByteError : ThemeClassificationFormatDefinition {
			HexByteError() : base(TextColor.HexByteError) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexAscii)]
		[Name(ThemeClassificationTypeNameKeys.HexAscii)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexAscii : ThemeClassificationFormatDefinition {
			HexAscii() : base(TextColor.HexAscii) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexCaret)]
		[Name(ThemeClassificationTypeNameKeys.HexCaret)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexCaret : ThemeClassificationFormatDefinition {
			HexCaret() : base(TextColor.HexCaret) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexInactiveCaret)]
		[Name(ThemeClassificationTypeNameKeys.HexInactiveCaret)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexInactiveCaret : ThemeClassificationFormatDefinition {
			HexInactiveCaret() : base(TextColor.HexInactiveCaret) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexSelection)]
		[Name(ThemeClassificationTypeNameKeys.HexSelection)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexSelection : ThemeClassificationFormatDefinition {
			HexSelection() : base(TextColor.HexSelection) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.GlyphMargin)]
		[Name(ThemeClassificationTypeNameKeys.GlyphMargin)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class GlyphMargin : ThemeClassificationFormatDefinition {
			GlyphMargin() : base(TextColor.GlyphMargin) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BraceMatching)]
		[Name(ThemeClassificationTypeNameKeys.BraceMatching)]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class BraceMatching : ThemeClassificationFormatDefinition {
			BraceMatching() : base(TextColor.BraceMatching) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.LineSeparator)]
		[Name(ThemeClassificationTypeNameKeys.LineSeparator)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class LineSeparator : ThemeClassificationFormatDefinition {
			LineSeparator() : base(TextColor.LineSeparator) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.FindMatchHighlightMarker)]
		[Name(ThemeClassificationTypeNameKeys.FindMatchHighlightMarker)]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class FindMatchHighlightMarker : ThemeMarkerFormatDefinition {
			FindMatchHighlightMarker() : base(TextColor.FindMatchHighlightMarker) => ZOrder = TextMarkerServiceZIndexes.FindMatch;
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureNamespace)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureNamespace)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureNamespace : ThemeMarkerFormatDefinition {
			BlockStructureNamespace() : base(TextColor.BlockStructureNamespace) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureType)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureType)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureType : ThemeMarkerFormatDefinition {
			BlockStructureType() : base(TextColor.BlockStructureType) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureModule)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureModule)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureModule : ThemeMarkerFormatDefinition {
			BlockStructureModule() : base(TextColor.BlockStructureModule) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureValueType)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureValueType)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureValueType : ThemeMarkerFormatDefinition {
			BlockStructureValueType() : base(TextColor.BlockStructureValueType) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureInterface)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureInterface)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureInterface : ThemeMarkerFormatDefinition {
			BlockStructureInterface() : base(TextColor.BlockStructureInterface) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureMethod)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureMethod)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureMethod : ThemeMarkerFormatDefinition {
			BlockStructureMethod() : base(TextColor.BlockStructureMethod) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureAccessor)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureAccessor)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureAccessor : ThemeMarkerFormatDefinition {
			BlockStructureAccessor() : base(TextColor.BlockStructureAccessor) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureAnonymousMethod)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureAnonymousMethod)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureAnonymousMethod : ThemeMarkerFormatDefinition {
			BlockStructureAnonymousMethod() : base(TextColor.BlockStructureAnonymousMethod) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureConstructor)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureConstructor)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureConstructor : ThemeMarkerFormatDefinition {
			BlockStructureConstructor() : base(TextColor.BlockStructureConstructor) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureDestructor)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureDestructor)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureDestructor : ThemeMarkerFormatDefinition {
			BlockStructureDestructor() : base(TextColor.BlockStructureDestructor) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureOperator)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureOperator)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureOperator : ThemeMarkerFormatDefinition {
			BlockStructureOperator() : base(TextColor.BlockStructureOperator) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureConditional)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureConditional)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureConditional : ThemeMarkerFormatDefinition {
			BlockStructureConditional() : base(TextColor.BlockStructureConditional) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureLoop)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureLoop)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureLoop : ThemeMarkerFormatDefinition {
			BlockStructureLoop() : base(TextColor.BlockStructureLoop) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureProperty)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureProperty)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureProperty : ThemeMarkerFormatDefinition {
			BlockStructureProperty() : base(TextColor.BlockStructureProperty) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureEvent)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureEvent)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureEvent : ThemeMarkerFormatDefinition {
			BlockStructureEvent() : base(TextColor.BlockStructureEvent) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureTry)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureTry)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureTry : ThemeMarkerFormatDefinition {
			BlockStructureTry() : base(TextColor.BlockStructureTry) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureCatch)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureCatch)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureCatch : ThemeMarkerFormatDefinition {
			BlockStructureCatch() : base(TextColor.BlockStructureCatch) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureFilter)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureFilter)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureFilter : ThemeMarkerFormatDefinition {
			BlockStructureFilter() : base(TextColor.BlockStructureFilter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureFinally)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureFinally)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureFinally : ThemeMarkerFormatDefinition {
			BlockStructureFinally() : base(TextColor.BlockStructureFinally) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureFault)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureFault)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureFault : ThemeMarkerFormatDefinition {
			BlockStructureFault() : base(TextColor.BlockStructureFault) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureLock)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureLock)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureLock : ThemeMarkerFormatDefinition {
			BlockStructureLock() : base(TextColor.BlockStructureLock) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureUsing)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureUsing)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureUsing : ThemeMarkerFormatDefinition {
			BlockStructureUsing() : base(TextColor.BlockStructureUsing) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureFixed)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureFixed)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureFixed : ThemeMarkerFormatDefinition {
			BlockStructureFixed() : base(TextColor.BlockStructureFixed) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureSwitch)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureSwitch)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureSwitch : ThemeMarkerFormatDefinition {
			BlockStructureSwitch() : base(TextColor.BlockStructureSwitch) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureCase)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureCase)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureCase : ThemeMarkerFormatDefinition {
			BlockStructureCase() : base(TextColor.BlockStructureCase) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureLocalFunction)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureLocalFunction)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureLocalFunction : ThemeMarkerFormatDefinition {
			BlockStructureLocalFunction() : base(TextColor.BlockStructureLocalFunction) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureOther)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureOther)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureOther : ThemeMarkerFormatDefinition {
			BlockStructureOther() : base(TextColor.BlockStructureOther) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureXml)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureXml)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureXml : ThemeMarkerFormatDefinition {
			BlockStructureXml() : base(TextColor.BlockStructureXml) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BlockStructureXaml)]
		[Name(ThemeClassificationTypeNameKeys.BlockStructureXaml)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BlockStructureXaml : ThemeMarkerFormatDefinition {
			BlockStructureXaml() : base(TextColor.BlockStructureXaml) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.CompletionMatchHighlight)]
		[Name(ThemeClassificationTypeNameKeys.CompletionMatchHighlight)]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class CompletionMatchHighlight : ThemeClassificationFormatDefinition {
			CompletionMatchHighlight() : base(TextColor.CompletionMatchHighlight) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.CompletionSuffix)]
		[Name(ThemeClassificationTypeNameKeys.CompletionSuffix)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class CompletionSuffix : ThemeClassificationFormatDefinition {
			CompletionSuffix() : base(TextColor.CompletionSuffix) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SignatureHelpDocumentation)]
		[Name(ThemeClassificationTypeNameKeys.SignatureHelpDocumentation)]
		[UserVisible(true)]
		[Order(Before = Priority.Default, After = Priority.Low)]
		sealed class SignatureHelpDocumentation : ThemeClassificationFormatDefinition {
			SignatureHelpDocumentation() : base(TextColor.SignatureHelpDocumentation) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SignatureHelpCurrentParameter)]
		[Name(ThemeClassificationTypeNameKeys.SignatureHelpCurrentParameter)]
		[UserVisible(true)]
		[Order(Before = Priority.Default, After = Priority.Low)]
		sealed class SignatureHelpCurrentParameter : ThemeClassificationFormatDefinition {
			SignatureHelpCurrentParameter() : base(TextColor.SignatureHelpCurrentParameter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SignatureHelpParameter)]
		[Name(ThemeClassificationTypeNameKeys.SignatureHelpParameter)]
		[UserVisible(true)]
		[Order(Before = Priority.Default, After = Priority.Low)]
		sealed class SignatureHelpParameter : ThemeClassificationFormatDefinition {
			SignatureHelpParameter() : base(TextColor.SignatureHelpParameter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SignatureHelpParameterDocumentation)]
		[Name(ThemeClassificationTypeNameKeys.SignatureHelpParameterDocumentation)]
		[UserVisible(true)]
		[Order(Before = Priority.Default, After = Priority.Low)]
		sealed class SignatureHelpParameterDocumentation : ThemeClassificationFormatDefinition {
			SignatureHelpParameterDocumentation() : base(TextColor.SignatureHelpParameterDocumentation) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Url)]
		[Name(ThemeClassificationTypeNameKeys.Url)]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class Url : ThemeClassificationFormatDefinition {
			Url() : base(TextColor.Url) => TextDecorations = System.Windows.TextDecorations.Underline;
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexPeDosHeader)]
		[Name(ThemeClassificationTypeNameKeys.HexPeDosHeader)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexPeDosHeader : ThemeClassificationFormatDefinition {
			HexPeDosHeader() : base(TextColor.HexPeDosHeader) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexPeFileHeader)]
		[Name(ThemeClassificationTypeNameKeys.HexPeFileHeader)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexPeFileHeader : ThemeClassificationFormatDefinition {
			HexPeFileHeader() : base(TextColor.HexPeFileHeader) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexPeOptionalHeader32)]
		[Name(ThemeClassificationTypeNameKeys.HexPeOptionalHeader32)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexPeOptionalHeader32 : ThemeClassificationFormatDefinition {
			HexPeOptionalHeader32() : base(TextColor.HexPeOptionalHeader32) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexPeOptionalHeader64)]
		[Name(ThemeClassificationTypeNameKeys.HexPeOptionalHeader64)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexPeOptionalHeader64 : ThemeClassificationFormatDefinition {
			HexPeOptionalHeader64() : base(TextColor.HexPeOptionalHeader64) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexPeSection)]
		[Name(ThemeClassificationTypeNameKeys.HexPeSection)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexPeSection : ThemeClassificationFormatDefinition {
			HexPeSection() : base(TextColor.HexPeSection) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexPeSectionName)]
		[Name(ThemeClassificationTypeNameKeys.HexPeSectionName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexPeSectionName : ThemeClassificationFormatDefinition {
			HexPeSectionName() : base(TextColor.HexPeSectionName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexCor20Header)]
		[Name(ThemeClassificationTypeNameKeys.HexCor20Header)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexCor20Header : ThemeClassificationFormatDefinition {
			HexCor20Header() : base(TextColor.HexCor20Header) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexStorageSignature)]
		[Name(ThemeClassificationTypeNameKeys.HexStorageSignature)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexStorageSignature : ThemeClassificationFormatDefinition {
			HexStorageSignature() : base(TextColor.HexStorageSignature) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexStorageHeader)]
		[Name(ThemeClassificationTypeNameKeys.HexStorageHeader)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexStorageHeader : ThemeClassificationFormatDefinition {
			HexStorageHeader() : base(TextColor.HexStorageHeader) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexStorageStream)]
		[Name(ThemeClassificationTypeNameKeys.HexStorageStream)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexStorageStream : ThemeClassificationFormatDefinition {
			HexStorageStream() : base(TextColor.HexStorageStream) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexStorageStreamName)]
		[Name(ThemeClassificationTypeNameKeys.HexStorageStreamName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexStorageStreamName : ThemeClassificationFormatDefinition {
			HexStorageStreamName() : base(TextColor.HexStorageStreamName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexStorageStreamNameInvalid)]
		[Name(ThemeClassificationTypeNameKeys.HexStorageStreamNameInvalid)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexStorageStreamNameInvalid : ThemeClassificationFormatDefinition {
			HexStorageStreamNameInvalid() : base(TextColor.HexStorageStreamNameInvalid) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexTablesStream)]
		[Name(ThemeClassificationTypeNameKeys.HexTablesStream)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexTablesStream : ThemeClassificationFormatDefinition {
			HexTablesStream() : base(TextColor.HexTablesStream) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexTableName)]
		[Name(ThemeClassificationTypeNameKeys.HexTableName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexTableName : ThemeClassificationFormatDefinition {
			HexTableName() : base(TextColor.HexTableName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DocumentListMatchHighlight)]
		[Name(ThemeClassificationTypeNameKeys.DocumentListMatchHighlight)]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class DocumentListMatchHighlight : ThemeMarkerFormatDefinition {
			DocumentListMatchHighlight() : base(TextColor.DocumentListMatchHighlight) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.GacMatchHighlight)]
		[Name(ThemeClassificationTypeNameKeys.GacMatchHighlight)]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class GacMatchHighlight : ThemeMarkerFormatDefinition {
			GacMatchHighlight() : base(TextColor.GacMatchHighlight) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AppSettingsTreeViewNodeMatchHighlight)]
		[Name(ThemeClassificationTypeNameKeys.AppSettingsTreeViewNodeMatchHighlight)]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class AppSettingsTreeViewNodeMatchHighlight : ThemeMarkerFormatDefinition {
			AppSettingsTreeViewNodeMatchHighlight() : base(TextColor.AppSettingsTreeViewNodeMatchHighlight) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AppSettingsTextMatchHighlight)]
		[Name(ThemeClassificationTypeNameKeys.AppSettingsTextMatchHighlight)]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class AppSettingsTextMatchHighlight : ThemeMarkerFormatDefinition {
			AppSettingsTextMatchHighlight() : base(TextColor.AppSettingsTextMatchHighlight) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexCurrentLine)]
		[Name(ThemeClassificationTypeNameKeys.HexCurrentLine)]
		[UserVisible(true)]
		[Order(Before = Priority.Default)]
		sealed class HexCurrentLine : ThemeClassificationFormatDefinition {
			HexCurrentLine() : base(TextColor.HexCurrentLine) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexCurrentLineNoFocus)]
		[Name(ThemeClassificationTypeNameKeys.HexCurrentLineNoFocus)]
		[UserVisible(true)]
		[Order(Before = Priority.Default)]
		sealed class HexCurrentLineNoFocus : ThemeClassificationFormatDefinition {
			HexCurrentLineNoFocus() : base(TextColor.HexCurrentLineNoFocus) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexInactiveSelectedText)]
		[Name(ThemeClassificationTypeNameKeys.HexInactiveSelectedText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexInactiveSelectedText : ThemeClassificationFormatDefinition {
			HexInactiveSelectedText() : base(TextColor.HexInactiveSelectedText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexColumnLine0)]
		[Name(ThemeClassificationTypeNameKeys.HexColumnLine0)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexColumnLine0 : ThemeClassificationFormatDefinition {
			HexColumnLine0() : base(TextColor.HexColumnLine0) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexColumnLine1)]
		[Name(ThemeClassificationTypeNameKeys.HexColumnLine1)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexColumnLine1 : ThemeClassificationFormatDefinition {
			HexColumnLine1() : base(TextColor.HexColumnLine1) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexColumnLineGroup0)]
		[Name(ThemeClassificationTypeNameKeys.HexColumnLineGroup0)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexColumnLineGroup0 : ThemeClassificationFormatDefinition {
			HexColumnLineGroup0() : base(TextColor.HexColumnLineGroup0) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexColumnLineGroup1)]
		[Name(ThemeClassificationTypeNameKeys.HexColumnLineGroup1)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexColumnLineGroup1 : ThemeClassificationFormatDefinition {
			HexColumnLineGroup1() : base(TextColor.HexColumnLineGroup1) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexHighlightedValuesColumn)]
		[Name(ThemeClassificationTypeNameKeys.HexHighlightedValuesColumn)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexHighlightedValuesColumn : ThemeClassificationFormatDefinition {
			HexHighlightedValuesColumn() : base(TextColor.HexHighlightedValuesColumn) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexHighlightedAsciiColumn)]
		[Name(ThemeClassificationTypeNameKeys.HexHighlightedAsciiColumn)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexHighlightedAsciiColumn : ThemeClassificationFormatDefinition {
			HexHighlightedAsciiColumn() : base(TextColor.HexHighlightedAsciiColumn) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexGlyphMargin)]
		[Name(ThemeClassificationTypeNameKeys.HexGlyphMargin)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexGlyphMargin : ThemeClassificationFormatDefinition {
			HexGlyphMargin() : base(TextColor.HexGlyphMargin) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexCurrentValueCell)]
		[Name(ThemeClassificationTypeNameKeys.HexCurrentValueCell)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexCurrentValueCell : ThemeMarkerFormatDefinition {
			HexCurrentValueCell() : base(TextColor.HexCurrentValueCell) => ZOrder = HexMarkerServiceZIndexes.CurrentValue;
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexCurrentAsciiCell)]
		[Name(ThemeClassificationTypeNameKeys.HexCurrentAsciiCell)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexCurrentAsciiCell : ThemeMarkerFormatDefinition {
			HexCurrentAsciiCell() : base(TextColor.HexCurrentAsciiCell) => ZOrder = HexMarkerServiceZIndexes.CurrentValue;
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.OutputWindowText)]
		[Name(ThemeClassificationTypeNameKeys.OutputWindowText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class OutputWindowText : ThemeClassificationFormatDefinition {
			OutputWindowText() : base(TextColor.OutputWindowText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexFindMatchHighlightMarker)]
		[Name(ThemeClassificationTypeNameKeys.HexFindMatchHighlightMarker)]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class HexFindMatchHighlightMarker : ThemeMarkerFormatDefinition {
			HexFindMatchHighlightMarker() : base(TextColor.HexFindMatchHighlightMarker) => ZOrder = HexMarkerServiceZIndexes.FindMatch;
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexToolTipServiceField0)]
		[Name(ThemeClassificationTypeNameKeys.HexToolTipServiceField0)]
		[UserVisible(true)]
		[Order(After = Priority.High, Before = ThemeClassificationTypeNameKeys.HexToolTipServiceCurrentField)]
		[Order(After = ThemeClassificationTypeNameKeys.HexFindMatchHighlightMarker, Before = ThemeClassificationTypeNameKeys.HexToolTipServiceField1)]
		sealed class HexToolTipServiceField0 : ThemeMarkerFormatDefinition {
			HexToolTipServiceField0() : base(TextColor.HexToolTipServiceField0) => ZOrder = HexMarkerServiceZIndexes.ToolTipField0;
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexToolTipServiceField1)]
		[Name(ThemeClassificationTypeNameKeys.HexToolTipServiceField1)]
		[UserVisible(true)]
		[Order(After = Priority.High, Before = ThemeClassificationTypeNameKeys.HexToolTipServiceCurrentField)]
		[Order(After = ThemeClassificationTypeNameKeys.HexToolTipServiceField0)]
		sealed class HexToolTipServiceField1 : ThemeMarkerFormatDefinition {
			HexToolTipServiceField1() : base(TextColor.HexToolTipServiceField1) => ZOrder = HexMarkerServiceZIndexes.ToolTipField1;
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexToolTipServiceCurrentField)]
		[Name(ThemeClassificationTypeNameKeys.HexToolTipServiceCurrentField)]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		[Order(After = ThemeClassificationTypeNameKeys.HexToolTipServiceField0)]
		[Order(After = ThemeClassificationTypeNameKeys.HexToolTipServiceField1)]
		sealed class HexToolTipServiceCurrentField : ThemeMarkerFormatDefinition {
			HexToolTipServiceCurrentField() : base(TextColor.HexToolTipServiceCurrentField) => ZOrder = HexMarkerServiceZIndexes.ToolTipCurrentField;
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ListFindMatchHighlight)]
		[Name(ThemeClassificationTypeNameKeys.ListFindMatchHighlight)]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class ListFindMatchHighlight : ThemeClassificationFormatDefinition {
			ListFindMatchHighlight() : base(TextColor.ListFindMatchHighlight) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebuggerValueChangedHighlight)]
		[Name(ThemeClassificationTypeNameKeys.DebuggerValueChangedHighlight)]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class DebuggerValueChangedHighlight : ThemeClassificationFormatDefinition {
			DebuggerValueChangedHighlight() : base(TextColor.DebuggerValueChangedHighlight) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugExceptionName)]
		[Name(ThemeClassificationTypeNameKeys.DebugExceptionName)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class DebugExceptionName : ThemeClassificationFormatDefinition {
			DebugExceptionName() : base(TextColor.DebugExceptionName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugStowedExceptionName)]
		[Name(ThemeClassificationTypeNameKeys.DebugStowedExceptionName)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class DebugStowedExceptionName : ThemeClassificationFormatDefinition {
			DebugStowedExceptionName() : base(TextColor.DebugStowedExceptionName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugReturnValueName)]
		[Name(ThemeClassificationTypeNameKeys.DebugReturnValueName)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class DebugReturnValueName : ThemeClassificationFormatDefinition {
			DebugReturnValueName() : base(TextColor.DebugReturnValueName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugVariableName)]
		[Name(ThemeClassificationTypeNameKeys.DebugVariableName)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class DebugVariableName : ThemeClassificationFormatDefinition {
			DebugVariableName() : base(TextColor.DebugVariableName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugObjectIdName)]
		[Name(ThemeClassificationTypeNameKeys.DebugObjectIdName)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class DebugObjectIdName : ThemeClassificationFormatDefinition {
			DebugObjectIdName() : base(TextColor.DebugObjectIdName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebuggerDisplayAttributeEval)]
		[Name(ThemeClassificationTypeNameKeys.DebuggerDisplayAttributeEval)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class DebuggerDisplayAttributeEval : ThemeClassificationFormatDefinition {
			DebuggerDisplayAttributeEval() : base(TextColor.DebuggerDisplayAttributeEval) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebuggerNoStringQuotesEval)]
		[Name(ThemeClassificationTypeNameKeys.DebuggerNoStringQuotesEval)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class DebuggerNoStringQuotesEval : ThemeClassificationFormatDefinition {
			DebuggerNoStringQuotesEval() : base(TextColor.DebuggerNoStringQuotesEval) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugViewPropertyName)]
		[Name(ThemeClassificationTypeNameKeys.DebugViewPropertyName)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class DebugViewPropertyName : ThemeClassificationFormatDefinition {
			DebugViewPropertyName() : base(TextColor.DebugViewPropertyName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AsmComment)]
		[Name(ThemeClassificationTypeNameKeys.AsmComment)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AsmComment : ThemeClassificationFormatDefinition {
			AsmComment() : base(TextColor.AsmComment) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AsmDirective)]
		[Name(ThemeClassificationTypeNameKeys.AsmDirective)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AsmDirective : ThemeClassificationFormatDefinition {
			AsmDirective() : base(TextColor.AsmDirective) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AsmPrefix)]
		[Name(ThemeClassificationTypeNameKeys.AsmPrefix)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AsmPrefix : ThemeClassificationFormatDefinition {
			AsmPrefix() : base(TextColor.AsmPrefix) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AsmMnemonic)]
		[Name(ThemeClassificationTypeNameKeys.AsmMnemonic)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AsmMnemonic : ThemeClassificationFormatDefinition {
			AsmMnemonic() : base(TextColor.AsmMnemonic) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AsmKeyword)]
		[Name(ThemeClassificationTypeNameKeys.AsmKeyword)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AsmKeyword : ThemeClassificationFormatDefinition {
			AsmKeyword() : base(TextColor.AsmKeyword) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AsmOperator)]
		[Name(ThemeClassificationTypeNameKeys.AsmOperator)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AsmOperator : ThemeClassificationFormatDefinition {
			AsmOperator() : base(TextColor.AsmOperator) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AsmPunctuation)]
		[Name(ThemeClassificationTypeNameKeys.AsmPunctuation)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AsmPunctuation : ThemeClassificationFormatDefinition {
			AsmPunctuation() : base(TextColor.AsmPunctuation) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AsmNumber)]
		[Name(ThemeClassificationTypeNameKeys.AsmNumber)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AsmNumber : ThemeClassificationFormatDefinition {
			AsmNumber() : base(TextColor.AsmNumber) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AsmRegister)]
		[Name(ThemeClassificationTypeNameKeys.AsmRegister)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AsmRegister : ThemeClassificationFormatDefinition {
			AsmRegister() : base(TextColor.AsmRegister) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AsmSelectorValue)]
		[Name(ThemeClassificationTypeNameKeys.AsmSelectorValue)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AsmSelectorValue : ThemeClassificationFormatDefinition {
			AsmSelectorValue() : base(TextColor.AsmSelectorValue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AsmLabelAddress)]
		[Name(ThemeClassificationTypeNameKeys.AsmLabelAddress)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AsmLabelAddress : ThemeClassificationFormatDefinition {
			AsmLabelAddress() : base(TextColor.AsmLabelAddress) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AsmFunctionAddress)]
		[Name(ThemeClassificationTypeNameKeys.AsmFunctionAddress)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AsmFunctionAddress : ThemeClassificationFormatDefinition {
			AsmFunctionAddress() : base(TextColor.AsmFunctionAddress) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AsmLabel)]
		[Name(ThemeClassificationTypeNameKeys.AsmLabel)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AsmLabel : ThemeClassificationFormatDefinition {
			AsmLabel() : base(TextColor.AsmLabel) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AsmFunction)]
		[Name(ThemeClassificationTypeNameKeys.AsmFunction)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AsmFunction : ThemeClassificationFormatDefinition {
			AsmFunction() : base(TextColor.AsmFunction) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AsmData)]
		[Name(ThemeClassificationTypeNameKeys.AsmData)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AsmData : ThemeClassificationFormatDefinition {
			AsmData() : base(TextColor.AsmData) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AsmAddress)]
		[Name(ThemeClassificationTypeNameKeys.AsmAddress)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AsmAddress : ThemeClassificationFormatDefinition {
			AsmAddress() : base(TextColor.AsmAddress) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AsmHexBytes)]
		[Name(ThemeClassificationTypeNameKeys.AsmHexBytes)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class AsmHexBytes : ThemeClassificationFormatDefinition {
			AsmHexBytes() : base(TextColor.AsmHexBytes) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "----------------")]
		[Name(Priority.High)]
		[UserVisible(false)]
		[Order(After = Priority.Default)]
		// Make sure High priority really is HIGH PRIORITY. string happens to be the last one unless I add this.
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class PriorityHigh : ClassificationFormatDefinition {
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "----------------")]
		[Name(Priority.Default)]
		[UserVisible(false)]
		[Order(After = Priority.Low, Before = Priority.High)]
		sealed class PriorityDefault : ClassificationFormatDefinition {
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "----------------")]
		[Name(Priority.Low)]
		[UserVisible(false)]
		[Order(Before = Priority.Default)]
		sealed class PriorityLow : ClassificationFormatDefinition {
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.FormalLanguage)]
		[Name(LanguagePriority.FormalLanguage)]
		[UserVisible(false)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = Priority.High)]
		sealed class FormalLanguage : ClassificationFormatDefinition {
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.NaturalLanguage)]
		[Name(LanguagePriority.NaturalLanguage)]
		[UserVisible(false)]
		[Order(After = Priority.Default, Before = LanguagePriority.FormalLanguage)]
		sealed class NaturalLanguage : ClassificationFormatDefinition {
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Identifier)]
		[Name(ThemeClassificationTypeNameKeys.Identifier)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Identifier : ClassificationFormatDefinition {
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Literal)]
		[Name(ThemeClassificationTypeNameKeys.Literal)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.Number)]
		sealed class Literal : ClassificationFormatDefinition {
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Other)]
		[Name(PredefinedClassificationTypeNames.Other)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Other : ClassificationFormatDefinition {
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.WhiteSpace)]
		[Name(PredefinedClassificationTypeNames.WhiteSpace)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class WhiteSpace : ClassificationFormatDefinition {
		}
	}
}
