//
// CSharpFormattingOptions.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.


using System;

namespace ICSharpCode.NRefactory.CSharp {
	public enum BraceStyle
	{
		EndOfLine,
		EndOfLineWithoutSpace,
		NextLine,
		NextLineShifted,
		NextLineShifted2,
		BannerStyle
	}

	public enum PropertyFormatting
	{
		SingleLine,
		MultipleLines
	}

	public enum Wrapping {
		DoNotWrap,
		WrapAlways,
		WrapIfTooLong
	}

	public enum NewLinePlacement {
		DoNotCare,
		NewLine,
		SameLine
	}

	public enum UsingPlacement {
		TopOfFile,
		InsideNamespace
	}

	public enum EmptyLineFormatting {
		DoNotChange,
		Indent,
		DoNotIndent
	}

	public class CSharpFormattingOptions : IEquatable<CSharpFormattingOptions>
	{
		public string Name {
			get;
			set;
		}

		public bool IsBuiltIn {
			get;
			set;
		}

		public CSharpFormattingOptions Clone ()
		{
			//return (CSharpFormattingOptions)MemberwiseClone ();
			// DON'T use MemberwiseClone() since we want to return a CSharpFormattingOptions, not any
			// derived class.
			return CopyTo(new CSharpFormattingOptions());
		}

		#region Indentation
		public bool IndentNamespaceBody { // tested
			get;
			set;
		}

		public bool IndentClassBody { // tested
			get;
			set;
		}

		public bool IndentInterfaceBody { // tested
			get;
			set;
		}

		public bool IndentStructBody { // tested
			get;
			set;
		}

		public bool IndentEnumBody { // tested
			get;
			set;
		}

		public bool IndentMethodBody { // tested
			get;
			set;
		}

		public bool IndentPropertyBody { // tested
			get;
			set;
		}

		public bool IndentEventBody { // tested
			get;
			set;
		}

		public bool IndentBlocks { // tested
			get;
			set;
		}

		public bool IndentSwitchBody { // tested
			get;
			set;
		}

		public bool IndentCaseBody { // tested
			get;
			set;
		}

		public bool IndentBreakStatements { // tested
			get;
			set;
		}

		public bool AlignEmbeddedStatements { // tested
			get;
			set;
		}

		public bool AlignElseInIfStatements {
			get;
			set;
		}

		public PropertyFormatting AutoPropertyFormatting { // tested
			get;
			set;
		}

		public PropertyFormatting SimplePropertyFormatting { // tested
			get;
			set;
		}

		public EmptyLineFormatting EmptyLineFormatting {
			get;
			set;
		}

		public bool IndentPreprocessorDirectives { // tested
			get;
			set;
		}

		public bool AlignToMemberReferenceDot { // TODO!
			get;
			set;
		}

		public bool IndentBlocksInsideExpressions {
			get;
			set;
		}
		#endregion

		#region Braces
		public BraceStyle NamespaceBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle ClassBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle InterfaceBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle StructBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle EnumBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle MethodBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle AnonymousMethodBraceStyle {
			get;
			set;
		}

		public BraceStyle ConstructorBraceStyle {  // tested
			get;
			set;
		}

		public BraceStyle DestructorBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle PropertyBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle PropertyGetBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle PropertySetBraceStyle { // tested
			get;
			set;
		}

		public PropertyFormatting SimpleGetBlockFormatting { // tested
			get;
			set;
		}

		public PropertyFormatting SimpleSetBlockFormatting { // tested
			get;
			set;
		}

		public BraceStyle EventBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle EventAddBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle EventRemoveBraceStyle { // tested
			get;
			set;
		}

		public bool AllowEventAddBlockInline { // tested
			get;
			set;
		}

		public bool AllowEventRemoveBlockInline { // tested
			get;
			set;
		}

		public BraceStyle StatementBraceStyle { // tested
			get;
			set;
		}

		public bool AllowIfBlockInline {
			get;
			set;
		}

		bool allowOneLinedArrayInitialziers = true;
		public bool AllowOneLinedArrayInitialziers {
			get {
				return allowOneLinedArrayInitialziers;
			}
			set {
				allowOneLinedArrayInitialziers = value;
			}
		}
		#endregion

		#region NewLines
		public NewLinePlacement ElseNewLinePlacement { // tested
			get;
			set;
		}

		public NewLinePlacement ElseIfNewLinePlacement { // tested
			get;
			set;
		}

		public NewLinePlacement CatchNewLinePlacement { // tested
			get;
			set;
		}

		public NewLinePlacement FinallyNewLinePlacement { // tested
			get;
			set;
		}

		public NewLinePlacement WhileNewLinePlacement { // tested
			get;
			set;
		}

		NewLinePlacement embeddedStatementPlacement = NewLinePlacement.NewLine;
		public NewLinePlacement EmbeddedStatementPlacement {
			get {
				return embeddedStatementPlacement;
			}
			set {
				embeddedStatementPlacement = value;
			}
		}
		#endregion

		#region Spaces
		// Methods
		public bool SpaceBeforeMethodDeclarationParentheses { // tested
			get;
			set;
		}

		public bool SpaceBetweenEmptyMethodDeclarationParentheses {
			get;
			set;
		}

		public bool SpaceBeforeMethodDeclarationParameterComma { // tested
			get;
			set;
		}

		public bool SpaceAfterMethodDeclarationParameterComma { // tested
			get;
			set;
		}

		public bool SpaceWithinMethodDeclarationParentheses { // tested
			get;
			set;
		}

		// Method calls
		public bool SpaceBeforeMethodCallParentheses { // tested
			get;
			set;
		}

		public bool SpaceBetweenEmptyMethodCallParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeMethodCallParameterComma { // tested
			get;
			set;
		}

		public bool SpaceAfterMethodCallParameterComma { // tested
			get;
			set;
		}

		public bool SpaceWithinMethodCallParentheses { // tested
			get;
			set;
		}

		// fields

		public bool SpaceBeforeFieldDeclarationComma { // tested
			get;
			set;
		}

		public bool SpaceAfterFieldDeclarationComma { // tested
			get;
			set;
		}

		// local variables

		public bool SpaceBeforeLocalVariableDeclarationComma { // tested
			get;
			set;
		}

		public bool SpaceAfterLocalVariableDeclarationComma { // tested
			get;
			set;
		}

		// constructors

		public bool SpaceBeforeConstructorDeclarationParentheses { // tested
			get;
			set;
		}

		public bool SpaceBetweenEmptyConstructorDeclarationParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeConstructorDeclarationParameterComma { // tested
			get;
			set;
		}

		public bool SpaceAfterConstructorDeclarationParameterComma { // tested
			get;
			set;
		}

		public bool SpaceWithinConstructorDeclarationParentheses { // tested
			get;
			set;
		}

		public NewLinePlacement NewLineBeforeConstructorInitializerColon {
			get;
			set;
		}

		public NewLinePlacement NewLineAfterConstructorInitializerColon {
			get;
			set;
		}

		// indexer
		public bool SpaceBeforeIndexerDeclarationBracket { // tested
			get;
			set;
		}

		public bool SpaceWithinIndexerDeclarationBracket { // tested
			get;
			set;
		}

		public bool SpaceBeforeIndexerDeclarationParameterComma {
			get;
			set;
		}

		public bool SpaceAfterIndexerDeclarationParameterComma {
			get;
			set;
		}

		// delegates

		public bool SpaceBeforeDelegateDeclarationParentheses {
			get;
			set;
		}

		public bool SpaceBetweenEmptyDelegateDeclarationParentheses {
			get;
			set;
		}

		public bool SpaceBeforeDelegateDeclarationParameterComma {
			get;
			set;
		}

		public bool SpaceAfterDelegateDeclarationParameterComma {
			get;
			set;
		}

		public bool SpaceWithinDelegateDeclarationParentheses {
			get;
			set;
		}

		public bool SpaceBeforeNewParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeIfParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeWhileParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeForParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeForeachParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeCatchParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeSwitchParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeLockParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeUsingParentheses { // tested
			get;
			set;
		}

		public bool SpaceAroundAssignment { // tested
			get;
			set;
		}

		public bool SpaceAroundLogicalOperator { // tested
			get;
			set;
		}

		public bool SpaceAroundEqualityOperator { // tested
			get;
			set;
		}

		public bool SpaceAroundRelationalOperator { // tested
			get;
			set;
		}

		public bool SpaceAroundBitwiseOperator { // tested
			get;
			set;
		}

		public bool SpaceAroundAdditiveOperator { // tested
			get;
			set;
		}

		public bool SpaceAroundMultiplicativeOperator { // tested
			get;
			set;
		}

		public bool SpaceAroundShiftOperator { // tested
			get;
			set;
		}

		public bool SpaceAroundNullCoalescingOperator { // Tested
			get;
			set;
		}

		public bool SpaceAfterUnsafeAddressOfOperator { // Tested
			get;
			set;
		}

		public bool SpaceAfterUnsafeAsteriskOfOperator { // Tested
			get;
			set;
		}

		public bool SpaceAroundUnsafeArrowOperator { // Tested
			get;
			set;
		}

		public bool SpacesWithinParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinIfParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinWhileParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinForParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinForeachParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinCatchParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinSwitchParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinLockParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinUsingParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinCastParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinSizeOfParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeSizeOfParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinTypeOfParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinNewParentheses { // tested
			get;
			set;
		}

		public bool SpacesBetweenEmptyNewParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeNewParameterComma { // tested
			get;
			set;
		}

		public bool SpaceAfterNewParameterComma { // tested
			get;
			set;
		}

		public bool SpaceBeforeTypeOfParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinCheckedExpressionParantheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeConditionalOperatorCondition { // tested
			get;
			set;
		}

		public bool SpaceAfterConditionalOperatorCondition { // tested
			get;
			set;
		}

		public bool SpaceBeforeConditionalOperatorSeparator { // tested
			get;
			set;
		}

		public bool SpaceAfterConditionalOperatorSeparator { // tested
			get;
			set;
		}

		// brackets
		public bool SpacesWithinBrackets { // tested
			get;
			set;
		}

		public bool SpacesBeforeBrackets { // tested
			get;
			set;
		}

		public bool SpaceBeforeBracketComma { // tested
			get;
			set;
		}

		public bool SpaceAfterBracketComma { // tested
			get;
			set;
		}

		public bool SpaceBeforeForSemicolon { // tested
			get;
			set;
		}

		public bool SpaceAfterForSemicolon { // tested
			get;
			set;
		}

		public bool SpaceAfterTypecast { // tested
			get;
			set;
		}

		public bool SpaceBeforeArrayDeclarationBrackets { // tested
			get;
			set;
		}

		public bool SpaceInNamedArgumentAfterDoubleColon {
			get;
			set;
		}

		public bool RemoveEndOfLineWhiteSpace {
			get;
			set;
		}

		public bool SpaceBeforeSemicolon {
			get;
			set;
		}
		#endregion

		#region Blank Lines
		public int MinimumBlankLinesBeforeUsings {
			get;
			set;
		}

		public int MinimumBlankLinesAfterUsings {
			get;
			set;
		}

		public int MinimumBlankLinesBeforeFirstDeclaration {
			get;
			set;
		}

		public int MinimumBlankLinesBetweenTypes {
			get;
			set;
		}

		public int MinimumBlankLinesBetweenFields {
			get;
			set;
		}

		public int MinimumBlankLinesBetweenEventFields {
			get;
			set;
		}

		public int MinimumBlankLinesBetweenMembers {
			get;
			set;
		}

		public int MinimumBlankLinesAroundRegion {
			get;
			set;
		}

		public int MinimumBlankLinesInsideRegion {
			get;
			set;
		}

		#endregion

		#region Keep formatting
		public bool KeepCommentsAtFirstColumn {
			get;
			set;
		}
		#endregion

		#region Wrapping

		public Wrapping ArrayInitializerWrapping {
			get;
			set;
		}

		public BraceStyle ArrayInitializerBraceStyle {
			get;
			set;
		}

		public Wrapping ChainedMethodCallWrapping {
			get;
			set;
		}

		public Wrapping MethodCallArgumentWrapping {
			get;
			set;
		}

		public NewLinePlacement NewLineAferMethodCallOpenParentheses {
			get;
			set;
		}

		public NewLinePlacement MethodCallClosingParenthesesOnNewLine {
			get;
			set;
		}

		public Wrapping IndexerArgumentWrapping {
			get;
			set;
		}

		public NewLinePlacement NewLineAferIndexerOpenBracket {
			get;
			set;
		}

		public NewLinePlacement IndexerClosingBracketOnNewLine {
			get;
			set;
		}

		public Wrapping MethodDeclarationParameterWrapping {
			get;
			set;
		}

		public NewLinePlacement NewLineAferMethodDeclarationOpenParentheses {
			get;
			set;
		}

		public NewLinePlacement MethodDeclarationClosingParenthesesOnNewLine {
			get;
			set;
		}

		public Wrapping IndexerDeclarationParameterWrapping {
			get;
			set;
		}

		public NewLinePlacement NewLineAferIndexerDeclarationOpenBracket {
			get;
			set;
		}

		public NewLinePlacement IndexerDeclarationClosingBracketOnNewLine {
			get;
			set;
		}

		public bool AlignToFirstIndexerArgument {
			get;
			set;
		}

		public bool AlignToFirstIndexerDeclarationParameter {
			get;
			set;
		}

		public bool AlignToFirstMethodCallArgument {
			get;
			set;
		}

		public bool AlignToFirstMethodDeclarationParameter {
			get;
			set;
		}

		public NewLinePlacement NewLineBeforeNewQueryClause {
			get;
			set;
		}

		#endregion

		#region Using Declarations
		public UsingPlacement UsingPlacement {
			get;
			set;
		}
		#endregion

		internal CSharpFormattingOptions()
		{
		}

		/*public static CSharpFormattingOptions Load (FilePath selectedFile)
		{
			using (var stream = System.IO.File.OpenRead (selectedFile)) {
				return Load (stream);
			}
		}

		public static CSharpFormattingOptions Load (System.IO.Stream input)
		{
			CSharpFormattingOptions result = FormattingOptionsFactory.CreateMonoOptions ();
			result.Name = "noname";
			using (XmlTextReader reader = new XmlTextReader (input)) {
				while (reader.Read ()) {
					if (reader.NodeType == XmlNodeType.Element) {
						if (reader.LocalName == "Property") {
							var info = typeof(CSharpFormattingOptions).GetProperty (reader.GetAttribute ("name"));
							string valString = reader.GetAttribute ("value");
							object value;
							if (info.PropertyType == typeof(bool)) {
								value = Boolean.Parse (valString);
							} else if (info.PropertyType == typeof(int)) {
								value = Int32.Parse (valString);
							} else {
								value = Enum.Parse (info.PropertyType, valString);
							}
							info.SetValue (result, value, null);
						} else if (reader.LocalName == "FormattingProfile") {
							result.Name = reader.GetAttribute ("name");
						}
					} else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "FormattingProfile") {
						//Console.WriteLine ("result:" + result.Name);
						return result;
					}
				}
			}
			return result;
		}

		public void Save (string fileName)
		{
			using (var writer = new XmlTextWriter (fileName, Encoding.Default)) {
				writer.Formatting = System.Xml.Formatting.Indented;
				writer.Indentation = 1;
				writer.IndentChar = '\t';
				writer.WriteStartElement ("FormattingProfile");
				writer.WriteAttributeString ("name", Name);
				foreach (PropertyInfo info in typeof (CSharpFormattingOptions).GetProperties ()) {
					if (info.GetCustomAttributes (false).Any (o => o.GetType () == typeof(ItemPropertyAttribute))) {
						writer.WriteStartElement ("Property");
						writer.WriteAttributeString ("name", info.Name);
						writer.WriteAttributeString ("value", info.GetValue (this, null).ToString ());
						writer.WriteEndElement ();
					}
				}
				writer.WriteEndElement ();
			}
		}

		public bool Equals (CSharpFormattingOptions other)
		{
			foreach (PropertyInfo info in typeof (CSharpFormattingOptions).GetProperties ()) {
				if (info.GetCustomAttributes (false).Any (o => o.GetType () == typeof(ItemPropertyAttribute))) {
					object val = info.GetValue (this, null);
					object otherVal = info.GetValue (other, null);
					if (val == null) {
						if (otherVal == null)
							continue;
						return false;
					}
					if (!val.Equals (otherVal)) {
						//Console.WriteLine ("!equal");
						return false;
					}
				}
			}
			//Console.WriteLine ("== equal");
			return true;
		}*/

		public CSharpFormattingOptions CopyTo(CSharpFormattingOptions other) {
			other.IndentNamespaceBody = this.IndentNamespaceBody;
			other.IndentClassBody = this.IndentClassBody;
			other.IndentInterfaceBody = this.IndentInterfaceBody;
			other.IndentStructBody = this.IndentStructBody;
			other.IndentEnumBody = this.IndentEnumBody;
			other.IndentMethodBody = this.IndentMethodBody;
			other.IndentPropertyBody = this.IndentPropertyBody;
			other.IndentEventBody = this.IndentEventBody;
			other.IndentBlocks = this.IndentBlocks;
			other.IndentSwitchBody = this.IndentSwitchBody;
			other.IndentCaseBody = this.IndentCaseBody;
			other.IndentBreakStatements = this.IndentBreakStatements;
			other.AlignEmbeddedStatements = this.AlignEmbeddedStatements;
			other.AlignElseInIfStatements = this.AlignElseInIfStatements;
			other.AutoPropertyFormatting = this.AutoPropertyFormatting;
			other.SimplePropertyFormatting = this.SimplePropertyFormatting;
			other.EmptyLineFormatting = this.EmptyLineFormatting;
			other.IndentPreprocessorDirectives = this.IndentPreprocessorDirectives;
			other.AlignToMemberReferenceDot = this.AlignToMemberReferenceDot;
			other.IndentBlocksInsideExpressions = this.IndentBlocksInsideExpressions;
			other.NamespaceBraceStyle = this.NamespaceBraceStyle;
			other.ClassBraceStyle = this.ClassBraceStyle;
			other.InterfaceBraceStyle = this.InterfaceBraceStyle;
			other.StructBraceStyle = this.StructBraceStyle;
			other.EnumBraceStyle = this.EnumBraceStyle;
			other.MethodBraceStyle = this.MethodBraceStyle;
			other.AnonymousMethodBraceStyle = this.AnonymousMethodBraceStyle;
			other.ConstructorBraceStyle = this.ConstructorBraceStyle;
			other.DestructorBraceStyle = this.DestructorBraceStyle;
			other.PropertyBraceStyle = this.PropertyBraceStyle;
			other.PropertyGetBraceStyle = this.PropertyGetBraceStyle;
			other.PropertySetBraceStyle = this.PropertySetBraceStyle;
			other.SimpleGetBlockFormatting = this.SimpleGetBlockFormatting;
			other.SimpleSetBlockFormatting = this.SimpleSetBlockFormatting;
			other.EventBraceStyle = this.EventBraceStyle;
			other.EventAddBraceStyle = this.EventAddBraceStyle;
			other.EventRemoveBraceStyle = this.EventRemoveBraceStyle;
			other.AllowEventAddBlockInline = this.AllowEventAddBlockInline;
			other.AllowEventRemoveBlockInline = this.AllowEventRemoveBlockInline;
			other.StatementBraceStyle = this.StatementBraceStyle;
			other.AllowIfBlockInline = this.AllowIfBlockInline;
			other.AllowOneLinedArrayInitialziers = this.AllowOneLinedArrayInitialziers;
			other.ElseNewLinePlacement = this.ElseNewLinePlacement;
			other.ElseIfNewLinePlacement = this.ElseIfNewLinePlacement;
			other.CatchNewLinePlacement = this.CatchNewLinePlacement;
			other.FinallyNewLinePlacement = this.FinallyNewLinePlacement;
			other.WhileNewLinePlacement = this.WhileNewLinePlacement;
			other.EmbeddedStatementPlacement = this.EmbeddedStatementPlacement;
			other.SpaceBeforeMethodDeclarationParentheses = this.SpaceBeforeMethodDeclarationParentheses;
			other.SpaceBetweenEmptyMethodDeclarationParentheses = this.SpaceBetweenEmptyMethodDeclarationParentheses;
			other.SpaceBeforeMethodDeclarationParameterComma = this.SpaceBeforeMethodDeclarationParameterComma;
			other.SpaceAfterMethodDeclarationParameterComma = this.SpaceAfterMethodDeclarationParameterComma;
			other.SpaceWithinMethodDeclarationParentheses = this.SpaceWithinMethodDeclarationParentheses;
			other.SpaceBeforeMethodCallParentheses = this.SpaceBeforeMethodCallParentheses;
			other.SpaceBetweenEmptyMethodCallParentheses = this.SpaceBetweenEmptyMethodCallParentheses;
			other.SpaceBeforeMethodCallParameterComma = this.SpaceBeforeMethodCallParameterComma;
			other.SpaceAfterMethodCallParameterComma = this.SpaceAfterMethodCallParameterComma;
			other.SpaceWithinMethodCallParentheses = this.SpaceWithinMethodCallParentheses;
			other.SpaceBeforeFieldDeclarationComma = this.SpaceBeforeFieldDeclarationComma;
			other.SpaceAfterFieldDeclarationComma = this.SpaceAfterFieldDeclarationComma;
			other.SpaceBeforeLocalVariableDeclarationComma = this.SpaceBeforeLocalVariableDeclarationComma;
			other.SpaceAfterLocalVariableDeclarationComma = this.SpaceAfterLocalVariableDeclarationComma;
			other.SpaceBeforeConstructorDeclarationParentheses = this.SpaceBeforeConstructorDeclarationParentheses;
			other.SpaceBetweenEmptyConstructorDeclarationParentheses = this.SpaceBetweenEmptyConstructorDeclarationParentheses;
			other.SpaceBeforeConstructorDeclarationParameterComma = this.SpaceBeforeConstructorDeclarationParameterComma;
			other.SpaceAfterConstructorDeclarationParameterComma = this.SpaceAfterConstructorDeclarationParameterComma;
			other.SpaceWithinConstructorDeclarationParentheses = this.SpaceWithinConstructorDeclarationParentheses;
			other.NewLineBeforeConstructorInitializerColon = this.NewLineBeforeConstructorInitializerColon;
			other.NewLineAfterConstructorInitializerColon = this.NewLineAfterConstructorInitializerColon;
			other.SpaceBeforeIndexerDeclarationBracket = this.SpaceBeforeIndexerDeclarationBracket;
			other.SpaceWithinIndexerDeclarationBracket = this.SpaceWithinIndexerDeclarationBracket;
			other.SpaceBeforeIndexerDeclarationParameterComma = this.SpaceBeforeIndexerDeclarationParameterComma;
			other.SpaceAfterIndexerDeclarationParameterComma = this.SpaceAfterIndexerDeclarationParameterComma;
			other.SpaceBeforeDelegateDeclarationParentheses = this.SpaceBeforeDelegateDeclarationParentheses;
			other.SpaceBetweenEmptyDelegateDeclarationParentheses = this.SpaceBetweenEmptyDelegateDeclarationParentheses;
			other.SpaceBeforeDelegateDeclarationParameterComma = this.SpaceBeforeDelegateDeclarationParameterComma;
			other.SpaceAfterDelegateDeclarationParameterComma = this.SpaceAfterDelegateDeclarationParameterComma;
			other.SpaceWithinDelegateDeclarationParentheses = this.SpaceWithinDelegateDeclarationParentheses;
			other.SpaceBeforeNewParentheses = this.SpaceBeforeNewParentheses;
			other.SpaceBeforeIfParentheses = this.SpaceBeforeIfParentheses;
			other.SpaceBeforeWhileParentheses = this.SpaceBeforeWhileParentheses;
			other.SpaceBeforeForParentheses = this.SpaceBeforeForParentheses;
			other.SpaceBeforeForeachParentheses = this.SpaceBeforeForeachParentheses;
			other.SpaceBeforeCatchParentheses = this.SpaceBeforeCatchParentheses;
			other.SpaceBeforeSwitchParentheses = this.SpaceBeforeSwitchParentheses;
			other.SpaceBeforeLockParentheses = this.SpaceBeforeLockParentheses;
			other.SpaceBeforeUsingParentheses = this.SpaceBeforeUsingParentheses;
			other.SpaceAroundAssignment = this.SpaceAroundAssignment;
			other.SpaceAroundLogicalOperator = this.SpaceAroundLogicalOperator;
			other.SpaceAroundEqualityOperator = this.SpaceAroundEqualityOperator;
			other.SpaceAroundRelationalOperator = this.SpaceAroundRelationalOperator;
			other.SpaceAroundBitwiseOperator = this.SpaceAroundBitwiseOperator;
			other.SpaceAroundAdditiveOperator = this.SpaceAroundAdditiveOperator;
			other.SpaceAroundMultiplicativeOperator = this.SpaceAroundMultiplicativeOperator;
			other.SpaceAroundShiftOperator = this.SpaceAroundShiftOperator;
			other.SpaceAroundNullCoalescingOperator = this.SpaceAroundNullCoalescingOperator;
			other.SpaceAfterUnsafeAddressOfOperator = this.SpaceAfterUnsafeAddressOfOperator;
			other.SpaceAfterUnsafeAsteriskOfOperator = this.SpaceAfterUnsafeAsteriskOfOperator;
			other.SpaceAroundUnsafeArrowOperator = this.SpaceAroundUnsafeArrowOperator;
			other.SpacesWithinParentheses = this.SpacesWithinParentheses;
			other.SpacesWithinIfParentheses = this.SpacesWithinIfParentheses;
			other.SpacesWithinWhileParentheses = this.SpacesWithinWhileParentheses;
			other.SpacesWithinForParentheses = this.SpacesWithinForParentheses;
			other.SpacesWithinForeachParentheses = this.SpacesWithinForeachParentheses;
			other.SpacesWithinCatchParentheses = this.SpacesWithinCatchParentheses;
			other.SpacesWithinSwitchParentheses = this.SpacesWithinSwitchParentheses;
			other.SpacesWithinLockParentheses = this.SpacesWithinLockParentheses;
			other.SpacesWithinUsingParentheses = this.SpacesWithinUsingParentheses;
			other.SpacesWithinCastParentheses = this.SpacesWithinCastParentheses;
			other.SpacesWithinSizeOfParentheses = this.SpacesWithinSizeOfParentheses;
			other.SpaceBeforeSizeOfParentheses = this.SpaceBeforeSizeOfParentheses;
			other.SpacesWithinTypeOfParentheses = this.SpacesWithinTypeOfParentheses;
			other.SpacesWithinNewParentheses = this.SpacesWithinNewParentheses;
			other.SpacesBetweenEmptyNewParentheses = this.SpacesBetweenEmptyNewParentheses;
			other.SpaceBeforeNewParameterComma = this.SpaceBeforeNewParameterComma;
			other.SpaceAfterNewParameterComma = this.SpaceAfterNewParameterComma;
			other.SpaceBeforeTypeOfParentheses = this.SpaceBeforeTypeOfParentheses;
			other.SpacesWithinCheckedExpressionParantheses = this.SpacesWithinCheckedExpressionParantheses;
			other.SpaceBeforeConditionalOperatorCondition = this.SpaceBeforeConditionalOperatorCondition;
			other.SpaceAfterConditionalOperatorCondition = this.SpaceAfterConditionalOperatorCondition;
			other.SpaceBeforeConditionalOperatorSeparator = this.SpaceBeforeConditionalOperatorSeparator;
			other.SpaceAfterConditionalOperatorSeparator = this.SpaceAfterConditionalOperatorSeparator;
			other.SpacesWithinBrackets = this.SpacesWithinBrackets;
			other.SpacesBeforeBrackets = this.SpacesBeforeBrackets;
			other.SpaceBeforeBracketComma = this.SpaceBeforeBracketComma;
			other.SpaceAfterBracketComma = this.SpaceAfterBracketComma;
			other.SpaceBeforeForSemicolon = this.SpaceBeforeForSemicolon;
			other.SpaceAfterForSemicolon = this.SpaceAfterForSemicolon;
			other.SpaceAfterTypecast = this.SpaceAfterTypecast;
			other.SpaceBeforeArrayDeclarationBrackets = this.SpaceBeforeArrayDeclarationBrackets;
			other.SpaceInNamedArgumentAfterDoubleColon = this.SpaceInNamedArgumentAfterDoubleColon;
			other.RemoveEndOfLineWhiteSpace = this.RemoveEndOfLineWhiteSpace;
			other.SpaceBeforeSemicolon = this.SpaceBeforeSemicolon;
			other.MinimumBlankLinesBeforeUsings = this.MinimumBlankLinesBeforeUsings;
			other.MinimumBlankLinesAfterUsings = this.MinimumBlankLinesAfterUsings;
			other.MinimumBlankLinesBeforeFirstDeclaration = this.MinimumBlankLinesBeforeFirstDeclaration;
			other.MinimumBlankLinesBetweenTypes = this.MinimumBlankLinesBetweenTypes;
			other.MinimumBlankLinesBetweenFields = this.MinimumBlankLinesBetweenFields;
			other.MinimumBlankLinesBetweenEventFields = this.MinimumBlankLinesBetweenEventFields;
			other.MinimumBlankLinesBetweenMembers = this.MinimumBlankLinesBetweenMembers;
			other.MinimumBlankLinesAroundRegion = this.MinimumBlankLinesAroundRegion;
			other.MinimumBlankLinesInsideRegion = this.MinimumBlankLinesInsideRegion;
			other.KeepCommentsAtFirstColumn = this.KeepCommentsAtFirstColumn;
			other.ArrayInitializerWrapping = this.ArrayInitializerWrapping;
			other.ArrayInitializerBraceStyle = this.ArrayInitializerBraceStyle;
			other.ChainedMethodCallWrapping = this.ChainedMethodCallWrapping;
			other.MethodCallArgumentWrapping = this.MethodCallArgumentWrapping;
			other.NewLineAferMethodCallOpenParentheses = this.NewLineAferMethodCallOpenParentheses;
			other.MethodCallClosingParenthesesOnNewLine = this.MethodCallClosingParenthesesOnNewLine;
			other.IndexerArgumentWrapping = this.IndexerArgumentWrapping;
			other.NewLineAferIndexerOpenBracket = this.NewLineAferIndexerOpenBracket;
			other.IndexerClosingBracketOnNewLine = this.IndexerClosingBracketOnNewLine;
			other.MethodDeclarationParameterWrapping = this.MethodDeclarationParameterWrapping;
			other.NewLineAferMethodDeclarationOpenParentheses = this.NewLineAferMethodDeclarationOpenParentheses;
			other.MethodDeclarationClosingParenthesesOnNewLine = this.MethodDeclarationClosingParenthesesOnNewLine;
			other.IndexerDeclarationParameterWrapping = this.IndexerDeclarationParameterWrapping;
			other.NewLineAferIndexerDeclarationOpenBracket = this.NewLineAferIndexerDeclarationOpenBracket;
			other.IndexerDeclarationClosingBracketOnNewLine = this.IndexerDeclarationClosingBracketOnNewLine;
			other.AlignToFirstIndexerArgument = this.AlignToFirstIndexerArgument;
			other.AlignToFirstIndexerDeclarationParameter = this.AlignToFirstIndexerDeclarationParameter;
			other.AlignToFirstMethodCallArgument = this.AlignToFirstMethodCallArgument;
			other.AlignToFirstMethodDeclarationParameter = this.AlignToFirstMethodDeclarationParameter;
			other.NewLineBeforeNewQueryClause = this.NewLineBeforeNewQueryClause;
			other.UsingPlacement = this.UsingPlacement;
			return other;
		}

		public bool Equals(CSharpFormattingOptions other) {
			if (other == null)
				return false;

			if (IndentNamespaceBody != other.IndentNamespaceBody) return false;
			if (IndentClassBody != other.IndentClassBody) return false;
			if (IndentInterfaceBody != other.IndentInterfaceBody) return false;
			if (IndentStructBody != other.IndentStructBody) return false;
			if (IndentEnumBody != other.IndentEnumBody) return false;
			if (IndentMethodBody != other.IndentMethodBody) return false;
			if (IndentPropertyBody != other.IndentPropertyBody) return false;
			if (IndentEventBody != other.IndentEventBody) return false;
			if (IndentBlocks != other.IndentBlocks) return false;
			if (IndentSwitchBody != other.IndentSwitchBody) return false;
			if (IndentCaseBody != other.IndentCaseBody) return false;
			if (IndentBreakStatements != other.IndentBreakStatements) return false;
			if (AlignEmbeddedStatements != other.AlignEmbeddedStatements) return false;
			if (AlignElseInIfStatements != other.AlignElseInIfStatements) return false;
			if (AutoPropertyFormatting != other.AutoPropertyFormatting) return false;
			if (SimplePropertyFormatting != other.SimplePropertyFormatting) return false;
			if (EmptyLineFormatting != other.EmptyLineFormatting) return false;
			if (IndentPreprocessorDirectives != other.IndentPreprocessorDirectives) return false;
			if (AlignToMemberReferenceDot != other.AlignToMemberReferenceDot) return false;
			if (IndentBlocksInsideExpressions != other.IndentBlocksInsideExpressions) return false;
			if (NamespaceBraceStyle != other.NamespaceBraceStyle) return false;
			if (ClassBraceStyle != other.ClassBraceStyle) return false;
			if (InterfaceBraceStyle != other.InterfaceBraceStyle) return false;
			if (StructBraceStyle != other.StructBraceStyle) return false;
			if (EnumBraceStyle != other.EnumBraceStyle) return false;
			if (MethodBraceStyle != other.MethodBraceStyle) return false;
			if (AnonymousMethodBraceStyle != other.AnonymousMethodBraceStyle) return false;
			if (ConstructorBraceStyle != other.ConstructorBraceStyle) return false;
			if (DestructorBraceStyle != other.DestructorBraceStyle) return false;
			if (PropertyBraceStyle != other.PropertyBraceStyle) return false;
			if (PropertyGetBraceStyle != other.PropertyGetBraceStyle) return false;
			if (PropertySetBraceStyle != other.PropertySetBraceStyle) return false;
			if (SimpleGetBlockFormatting != other.SimpleGetBlockFormatting) return false;
			if (SimpleSetBlockFormatting != other.SimpleSetBlockFormatting) return false;
			if (EventBraceStyle != other.EventBraceStyle) return false;
			if (EventAddBraceStyle != other.EventAddBraceStyle) return false;
			if (EventRemoveBraceStyle != other.EventRemoveBraceStyle) return false;
			if (AllowEventAddBlockInline != other.AllowEventAddBlockInline) return false;
			if (AllowEventRemoveBlockInline != other.AllowEventRemoveBlockInline) return false;
			if (StatementBraceStyle != other.StatementBraceStyle) return false;
			if (AllowIfBlockInline != other.AllowIfBlockInline) return false;
			if (AllowOneLinedArrayInitialziers != other.AllowOneLinedArrayInitialziers) return false;
			if (ElseNewLinePlacement != other.ElseNewLinePlacement) return false;
			if (ElseIfNewLinePlacement != other.ElseIfNewLinePlacement) return false;
			if (CatchNewLinePlacement != other.CatchNewLinePlacement) return false;
			if (FinallyNewLinePlacement != other.FinallyNewLinePlacement) return false;
			if (WhileNewLinePlacement != other.WhileNewLinePlacement) return false;
			if (EmbeddedStatementPlacement != other.EmbeddedStatementPlacement) return false;
			if (SpaceBeforeMethodDeclarationParentheses != other.SpaceBeforeMethodDeclarationParentheses) return false;
			if (SpaceBetweenEmptyMethodDeclarationParentheses != other.SpaceBetweenEmptyMethodDeclarationParentheses) return false;
			if (SpaceBeforeMethodDeclarationParameterComma != other.SpaceBeforeMethodDeclarationParameterComma) return false;
			if (SpaceAfterMethodDeclarationParameterComma != other.SpaceAfterMethodDeclarationParameterComma) return false;
			if (SpaceWithinMethodDeclarationParentheses != other.SpaceWithinMethodDeclarationParentheses) return false;
			if (SpaceBeforeMethodCallParentheses != other.SpaceBeforeMethodCallParentheses) return false;
			if (SpaceBetweenEmptyMethodCallParentheses != other.SpaceBetweenEmptyMethodCallParentheses) return false;
			if (SpaceBeforeMethodCallParameterComma != other.SpaceBeforeMethodCallParameterComma) return false;
			if (SpaceAfterMethodCallParameterComma != other.SpaceAfterMethodCallParameterComma) return false;
			if (SpaceWithinMethodCallParentheses != other.SpaceWithinMethodCallParentheses) return false;
			if (SpaceBeforeFieldDeclarationComma != other.SpaceBeforeFieldDeclarationComma) return false;
			if (SpaceAfterFieldDeclarationComma != other.SpaceAfterFieldDeclarationComma) return false;
			if (SpaceBeforeLocalVariableDeclarationComma != other.SpaceBeforeLocalVariableDeclarationComma) return false;
			if (SpaceAfterLocalVariableDeclarationComma != other.SpaceAfterLocalVariableDeclarationComma) return false;
			if (SpaceBeforeConstructorDeclarationParentheses != other.SpaceBeforeConstructorDeclarationParentheses) return false;
			if (SpaceBetweenEmptyConstructorDeclarationParentheses != other.SpaceBetweenEmptyConstructorDeclarationParentheses) return false;
			if (SpaceBeforeConstructorDeclarationParameterComma != other.SpaceBeforeConstructorDeclarationParameterComma) return false;
			if (SpaceAfterConstructorDeclarationParameterComma != other.SpaceAfterConstructorDeclarationParameterComma) return false;
			if (SpaceWithinConstructorDeclarationParentheses != other.SpaceWithinConstructorDeclarationParentheses) return false;
			if (NewLineBeforeConstructorInitializerColon != other.NewLineBeforeConstructorInitializerColon) return false;
			if (NewLineAfterConstructorInitializerColon != other.NewLineAfterConstructorInitializerColon) return false;
			if (SpaceBeforeIndexerDeclarationBracket != other.SpaceBeforeIndexerDeclarationBracket) return false;
			if (SpaceWithinIndexerDeclarationBracket != other.SpaceWithinIndexerDeclarationBracket) return false;
			if (SpaceBeforeIndexerDeclarationParameterComma != other.SpaceBeforeIndexerDeclarationParameterComma) return false;
			if (SpaceAfterIndexerDeclarationParameterComma != other.SpaceAfterIndexerDeclarationParameterComma) return false;
			if (SpaceBeforeDelegateDeclarationParentheses != other.SpaceBeforeDelegateDeclarationParentheses) return false;
			if (SpaceBetweenEmptyDelegateDeclarationParentheses != other.SpaceBetweenEmptyDelegateDeclarationParentheses) return false;
			if (SpaceBeforeDelegateDeclarationParameterComma != other.SpaceBeforeDelegateDeclarationParameterComma) return false;
			if (SpaceAfterDelegateDeclarationParameterComma != other.SpaceAfterDelegateDeclarationParameterComma) return false;
			if (SpaceWithinDelegateDeclarationParentheses != other.SpaceWithinDelegateDeclarationParentheses) return false;
			if (SpaceBeforeNewParentheses != other.SpaceBeforeNewParentheses) return false;
			if (SpaceBeforeIfParentheses != other.SpaceBeforeIfParentheses) return false;
			if (SpaceBeforeWhileParentheses != other.SpaceBeforeWhileParentheses) return false;
			if (SpaceBeforeForParentheses != other.SpaceBeforeForParentheses) return false;
			if (SpaceBeforeForeachParentheses != other.SpaceBeforeForeachParentheses) return false;
			if (SpaceBeforeCatchParentheses != other.SpaceBeforeCatchParentheses) return false;
			if (SpaceBeforeSwitchParentheses != other.SpaceBeforeSwitchParentheses) return false;
			if (SpaceBeforeLockParentheses != other.SpaceBeforeLockParentheses) return false;
			if (SpaceBeforeUsingParentheses != other.SpaceBeforeUsingParentheses) return false;
			if (SpaceAroundAssignment != other.SpaceAroundAssignment) return false;
			if (SpaceAroundLogicalOperator != other.SpaceAroundLogicalOperator) return false;
			if (SpaceAroundEqualityOperator != other.SpaceAroundEqualityOperator) return false;
			if (SpaceAroundRelationalOperator != other.SpaceAroundRelationalOperator) return false;
			if (SpaceAroundBitwiseOperator != other.SpaceAroundBitwiseOperator) return false;
			if (SpaceAroundAdditiveOperator != other.SpaceAroundAdditiveOperator) return false;
			if (SpaceAroundMultiplicativeOperator != other.SpaceAroundMultiplicativeOperator) return false;
			if (SpaceAroundShiftOperator != other.SpaceAroundShiftOperator) return false;
			if (SpaceAroundNullCoalescingOperator != other.SpaceAroundNullCoalescingOperator) return false;
			if (SpaceAfterUnsafeAddressOfOperator != other.SpaceAfterUnsafeAddressOfOperator) return false;
			if (SpaceAfterUnsafeAsteriskOfOperator != other.SpaceAfterUnsafeAsteriskOfOperator) return false;
			if (SpaceAroundUnsafeArrowOperator != other.SpaceAroundUnsafeArrowOperator) return false;
			if (SpacesWithinParentheses != other.SpacesWithinParentheses) return false;
			if (SpacesWithinIfParentheses != other.SpacesWithinIfParentheses) return false;
			if (SpacesWithinWhileParentheses != other.SpacesWithinWhileParentheses) return false;
			if (SpacesWithinForParentheses != other.SpacesWithinForParentheses) return false;
			if (SpacesWithinForeachParentheses != other.SpacesWithinForeachParentheses) return false;
			if (SpacesWithinCatchParentheses != other.SpacesWithinCatchParentheses) return false;
			if (SpacesWithinSwitchParentheses != other.SpacesWithinSwitchParentheses) return false;
			if (SpacesWithinLockParentheses != other.SpacesWithinLockParentheses) return false;
			if (SpacesWithinUsingParentheses != other.SpacesWithinUsingParentheses) return false;
			if (SpacesWithinCastParentheses != other.SpacesWithinCastParentheses) return false;
			if (SpacesWithinSizeOfParentheses != other.SpacesWithinSizeOfParentheses) return false;
			if (SpaceBeforeSizeOfParentheses != other.SpaceBeforeSizeOfParentheses) return false;
			if (SpacesWithinTypeOfParentheses != other.SpacesWithinTypeOfParentheses) return false;
			if (SpacesWithinNewParentheses != other.SpacesWithinNewParentheses) return false;
			if (SpacesBetweenEmptyNewParentheses != other.SpacesBetweenEmptyNewParentheses) return false;
			if (SpaceBeforeNewParameterComma != other.SpaceBeforeNewParameterComma) return false;
			if (SpaceAfterNewParameterComma != other.SpaceAfterNewParameterComma) return false;
			if (SpaceBeforeTypeOfParentheses != other.SpaceBeforeTypeOfParentheses) return false;
			if (SpacesWithinCheckedExpressionParantheses != other.SpacesWithinCheckedExpressionParantheses) return false;
			if (SpaceBeforeConditionalOperatorCondition != other.SpaceBeforeConditionalOperatorCondition) return false;
			if (SpaceAfterConditionalOperatorCondition != other.SpaceAfterConditionalOperatorCondition) return false;
			if (SpaceBeforeConditionalOperatorSeparator != other.SpaceBeforeConditionalOperatorSeparator) return false;
			if (SpaceAfterConditionalOperatorSeparator != other.SpaceAfterConditionalOperatorSeparator) return false;
			if (SpacesWithinBrackets != other.SpacesWithinBrackets) return false;
			if (SpacesBeforeBrackets != other.SpacesBeforeBrackets) return false;
			if (SpaceBeforeBracketComma != other.SpaceBeforeBracketComma) return false;
			if (SpaceAfterBracketComma != other.SpaceAfterBracketComma) return false;
			if (SpaceBeforeForSemicolon != other.SpaceBeforeForSemicolon) return false;
			if (SpaceAfterForSemicolon != other.SpaceAfterForSemicolon) return false;
			if (SpaceAfterTypecast != other.SpaceAfterTypecast) return false;
			if (SpaceBeforeArrayDeclarationBrackets != other.SpaceBeforeArrayDeclarationBrackets) return false;
			if (SpaceInNamedArgumentAfterDoubleColon != other.SpaceInNamedArgumentAfterDoubleColon) return false;
			if (RemoveEndOfLineWhiteSpace != other.RemoveEndOfLineWhiteSpace) return false;
			if (SpaceBeforeSemicolon != other.SpaceBeforeSemicolon) return false;
			if (MinimumBlankLinesBeforeUsings != other.MinimumBlankLinesBeforeUsings) return false;
			if (MinimumBlankLinesAfterUsings != other.MinimumBlankLinesAfterUsings) return false;
			if (MinimumBlankLinesBeforeFirstDeclaration != other.MinimumBlankLinesBeforeFirstDeclaration) return false;
			if (MinimumBlankLinesBetweenTypes != other.MinimumBlankLinesBetweenTypes) return false;
			if (MinimumBlankLinesBetweenFields != other.MinimumBlankLinesBetweenFields) return false;
			if (MinimumBlankLinesBetweenEventFields != other.MinimumBlankLinesBetweenEventFields) return false;
			if (MinimumBlankLinesBetweenMembers != other.MinimumBlankLinesBetweenMembers) return false;
			if (MinimumBlankLinesAroundRegion != other.MinimumBlankLinesAroundRegion) return false;
			if (MinimumBlankLinesInsideRegion != other.MinimumBlankLinesInsideRegion) return false;
			if (KeepCommentsAtFirstColumn != other.KeepCommentsAtFirstColumn) return false;
			if (ArrayInitializerWrapping != other.ArrayInitializerWrapping) return false;
			if (ArrayInitializerBraceStyle != other.ArrayInitializerBraceStyle) return false;
			if (ChainedMethodCallWrapping != other.ChainedMethodCallWrapping) return false;
			if (MethodCallArgumentWrapping != other.MethodCallArgumentWrapping) return false;
			if (NewLineAferMethodCallOpenParentheses != other.NewLineAferMethodCallOpenParentheses) return false;
			if (MethodCallClosingParenthesesOnNewLine != other.MethodCallClosingParenthesesOnNewLine) return false;
			if (IndexerArgumentWrapping != other.IndexerArgumentWrapping) return false;
			if (NewLineAferIndexerOpenBracket != other.NewLineAferIndexerOpenBracket) return false;
			if (IndexerClosingBracketOnNewLine != other.IndexerClosingBracketOnNewLine) return false;
			if (MethodDeclarationParameterWrapping != other.MethodDeclarationParameterWrapping) return false;
			if (NewLineAferMethodDeclarationOpenParentheses != other.NewLineAferMethodDeclarationOpenParentheses) return false;
			if (MethodDeclarationClosingParenthesesOnNewLine != other.MethodDeclarationClosingParenthesesOnNewLine) return false;
			if (IndexerDeclarationParameterWrapping != other.IndexerDeclarationParameterWrapping) return false;
			if (NewLineAferIndexerDeclarationOpenBracket != other.NewLineAferIndexerDeclarationOpenBracket) return false;
			if (IndexerDeclarationClosingBracketOnNewLine != other.IndexerDeclarationClosingBracketOnNewLine) return false;
			if (AlignToFirstIndexerArgument != other.AlignToFirstIndexerArgument) return false;
			if (AlignToFirstIndexerDeclarationParameter != other.AlignToFirstIndexerDeclarationParameter) return false;
			if (AlignToFirstMethodCallArgument != other.AlignToFirstMethodCallArgument) return false;
			if (AlignToFirstMethodDeclarationParameter != other.AlignToFirstMethodDeclarationParameter) return false;
			if (NewLineBeforeNewQueryClause != other.NewLineBeforeNewQueryClause) return false;
			if (UsingPlacement != other.UsingPlacement) return false;

			return true;
		}
	}
}
