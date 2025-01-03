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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.Decompiler {
	public enum DecompilationObject {
		NestedTypes,
		Fields,
		Events,
		Properties,
		Methods,
	}

	/// <summary>
	/// Settings for the decompiler.
	/// </summary>
	public class DecompilerSettings : INotifyPropertyChanged, IEquatable<DecompilerSettings> {
		protected virtual void OnModified() {
		}

		DecompilationObject[] decompilationObjects = new DecompilationObject[5] {
			DecompilationObject.Methods,
			DecompilationObject.Properties,
			DecompilationObject.Events,
			DecompilationObject.Fields,
			DecompilationObject.NestedTypes,
		};

		public IEnumerable<DecompilationObject> DecompilationObjects {
			get { return decompilationObjects.AsEnumerable(); }
		}

		public DecompilationObject DecompilationObject0 {
			get { return decompilationObjects[0]; }
			set { SetDecompilationObject(0, value); }
		}

		public DecompilationObject DecompilationObject1 {
			get { return decompilationObjects[1]; }
			set { SetDecompilationObject(1, value); }
		}

		public DecompilationObject DecompilationObject2 {
			get { return decompilationObjects[2]; }
			set { SetDecompilationObject(2, value); }
		}

		public DecompilationObject DecompilationObject3 {
			get { return decompilationObjects[3]; }
			set { SetDecompilationObject(3, value); }
		}

		public DecompilationObject DecompilationObject4 {
			get { return decompilationObjects[4]; }
			set { SetDecompilationObject(4, value); }
		}

		void SetDecompilationObject(int index, DecompilationObject newValue) {
			if (decompilationObjects[index] == newValue)
				return;

			int otherIndex = Array.IndexOf(decompilationObjects, newValue);
			Debug.Assert(otherIndex >= 0);
			if (otherIndex >= 0) {
				decompilationObjects[otherIndex] = decompilationObjects[index];
				decompilationObjects[index] = newValue;

				OnPropertyChanged(string.Format(DecompilationObject_format, otherIndex));
			}
			OnPropertyChanged(string.Format(DecompilationObject_format, index));
		}
		static string DecompilationObject_format = nameof(DecompilationObject0).Substring(0, nameof(DecompilationObject0).Length - 1) + "{0}";

		bool anonymousMethods = true;

		/// <summary>
		/// Decompile anonymous methods/lambdas.
		/// </summary>
		public bool AnonymousMethods {
			get { return anonymousMethods; }
			set {
				if (anonymousMethods != value) {
					anonymousMethods = value;
					OnPropertyChanged(nameof(AnonymousMethods));
				}
			}
		}

		bool expressionTrees = true;

		/// <summary>
		/// Decompile expression trees.
		/// </summary>
		public bool ExpressionTrees {
			get { return expressionTrees; }
			set {
				if (expressionTrees != value) {
					expressionTrees = value;
					OnPropertyChanged(nameof(ExpressionTrees));
				}
			}
		}

		bool yieldReturn = true;

		/// <summary>
		/// Decompile enumerators.
		/// </summary>
		public bool YieldReturn {
			get { return yieldReturn; }
			set {
				if (yieldReturn != value) {
					yieldReturn = value;
					OnPropertyChanged(nameof(YieldReturn));
				}
			}
		}

		bool asyncAwait = true;

		/// <summary>
		/// Decompile async methods.
		/// </summary>
		public bool AsyncAwait {
			get { return asyncAwait; }
			set {
				if (asyncAwait != value) {
					asyncAwait = value;
					OnPropertyChanged(nameof(AsyncAwait));
				}
			}
		}

		bool automaticProperties = true;

		/// <summary>
		/// Decompile automatic properties
		/// </summary>
		public bool AutomaticProperties {
			get { return automaticProperties; }
			set {
				if (automaticProperties != value) {
					automaticProperties = value;
					OnPropertyChanged(nameof(AutomaticProperties));
				}
			}
		}

		bool automaticEvents = true;

		/// <summary>
		/// Decompile automatic events
		/// </summary>
		public bool AutomaticEvents {
			get { return automaticEvents; }
			set {
				if (automaticEvents != value) {
					automaticEvents = value;
					OnPropertyChanged(nameof(AutomaticEvents));
				}
			}
		}

		bool usingStatement = true;

		/// <summary>
		/// Decompile using statements.
		/// </summary>
		public bool UsingStatement {
			get { return usingStatement; }
			set {
				if (usingStatement != value) {
					usingStatement = value;
					OnPropertyChanged(nameof(UsingStatement));
				}
			}
		}

		bool forEachStatement = true;

		/// <summary>
		/// Decompile foreach statements.
		/// </summary>
		public bool ForEachStatement {
			get { return forEachStatement; }
			set {
				if (forEachStatement != value) {
					forEachStatement = value;
					OnPropertyChanged(nameof(ForEachStatement));
				}
			}
		}

		bool lockStatement = true;

		/// <summary>
		/// Decompile lock statements.
		/// </summary>
		public bool LockStatement {
			get { return lockStatement; }
			set {
				if (lockStatement != value) {
					lockStatement = value;
					OnPropertyChanged(nameof(LockStatement));
				}
			}
		}

		bool switchStatementOnString = true;

		public bool SwitchStatementOnString {
			get { return switchStatementOnString; }
			set {
				if (switchStatementOnString != value) {
					switchStatementOnString = value;
					OnPropertyChanged(nameof(SwitchStatementOnString));
				}
			}
		}

		bool usingDeclarations = true;

		public bool UsingDeclarations {
			get { return usingDeclarations; }
			set {
				if (usingDeclarations != value) {
					usingDeclarations = value;
					OnPropertyChanged(nameof(UsingDeclarations));
				}
			}
		}

		bool queryExpressions = true;

		public bool QueryExpressions {
			get { return queryExpressions; }
			set {
				if (queryExpressions != value) {
					queryExpressions = value;
					OnPropertyChanged(nameof(QueryExpressions));
				}
			}
		}

		bool fullyQualifyAmbiguousTypeNames = true;

		public bool FullyQualifyAmbiguousTypeNames {
			get { return fullyQualifyAmbiguousTypeNames; }
			set {
				if (fullyQualifyAmbiguousTypeNames != value) {
					fullyQualifyAmbiguousTypeNames = value;
					OnPropertyChanged(nameof(FullyQualifyAmbiguousTypeNames));
				}
			}
		}

		bool fullyQualifyAllTypes = false;

		public bool FullyQualifyAllTypes {
			get { return fullyQualifyAllTypes; }
			set {
				if (fullyQualifyAllTypes != value) {
					fullyQualifyAllTypes = value;
					OnPropertyChanged(nameof(FullyQualifyAllTypes));
				}
			}
		}

		bool useDebugSymbols = true;

		/// <summary>
		/// Gets/Sets whether to use variable names from debug symbols, if available.
		/// </summary>
		public bool UseDebugSymbols {
			get { return useDebugSymbols; }
			set {
				if (useDebugSymbols != value) {
					useDebugSymbols = value;
					OnPropertyChanged(nameof(UseDebugSymbols));
				}
			}
		}

		bool objectCollectionInitializers = true;

		/// <summary>
		/// Gets/Sets whether to use C# 3.0 object/collection initializers
		/// </summary>
		public bool ObjectOrCollectionInitializers {
			get { return objectCollectionInitializers; }
			set {
				if (objectCollectionInitializers != value) {
					objectCollectionInitializers = value;
					OnPropertyChanged(nameof(ObjectOrCollectionInitializers));
				}
			}
		}

		bool showXmlDocumentation = true;

		/// <summary>
		/// Gets/Sets whether to include XML documentation comments in the decompiled code
		/// </summary>
		public bool ShowXmlDocumentation {
			get { return showXmlDocumentation; }
			set {
				if (showXmlDocumentation != value) {
					showXmlDocumentation = value;
					OnPropertyChanged(nameof(ShowXmlDocumentation));
				}
			}
		}

		bool removeEmptyDefaultConstructors = true;

		public bool RemoveEmptyDefaultConstructors {
			get { return removeEmptyDefaultConstructors; }
			set {
				if (removeEmptyDefaultConstructors != value) {
					removeEmptyDefaultConstructors = value;
					OnPropertyChanged(nameof(RemoveEmptyDefaultConstructors));
				}
			}
		}

		#region Options to aid VB decompilation
		bool introduceIncrementAndDecrement = true;

		/// <summary>
		/// Gets/Sets whether to use increment and decrement operators
		/// </summary>
		public bool IntroduceIncrementAndDecrement {
			get { return introduceIncrementAndDecrement; }
			set {
				if (introduceIncrementAndDecrement != value) {
					introduceIncrementAndDecrement = value;
					OnPropertyChanged(nameof(IntroduceIncrementAndDecrement));
				}
			}
		}

		bool makeAssignmentExpressions = true;

		/// <summary>
		/// Gets/Sets whether to use assignment expressions such as in while ((count = Do()) != 0) ;
		/// </summary>
		public bool MakeAssignmentExpressions {
			get { return makeAssignmentExpressions; }
			set {
				if (makeAssignmentExpressions != value) {
					makeAssignmentExpressions = value;
					OnPropertyChanged(nameof(MakeAssignmentExpressions));
				}
			}
		}

		bool alwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject = false;

		/// <summary>
		/// Gets/Sets whether to always generate exception variables in catch blocks
		/// </summary>
		public bool AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject {
			get { return alwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject; }
			set {
				if (alwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject != value) {
					alwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject = value;
					OnPropertyChanged(nameof(AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject));
				}
			}
		}
		#endregion

		bool showTokenAndRvaComments = true;

		/// <summary>
		/// Gets/sets whether to show tokens of types/methods/etc and the RVA / file offset in comments
		/// </summary>
		public bool ShowTokenAndRvaComments {
			get { return showTokenAndRvaComments; }
			set {
				if (showTokenAndRvaComments != value) {
					showTokenAndRvaComments = value;
					OnPropertyChanged(nameof(ShowTokenAndRvaComments));
				}
			}
		}

		bool sortMembers = false;

		/// <summary>
		/// Gets/sets whether to sort members
		/// </summary>
		public bool SortMembers {
			get { return sortMembers; }
			set {
				if (sortMembers != value) {
					sortMembers = value;
					OnPropertyChanged(nameof(SortMembers));
				}
			}
		}

		public bool ForceShowAllMembers {
			get { return forceShowAllMembers; }
			set {
				if (forceShowAllMembers != value) {
					forceShowAllMembers = value;
					OnPropertyChanged(nameof(ForceShowAllMembers));
				}
			}
		}
		bool forceShowAllMembers = false;

		public bool SortSystemUsingStatementsFirst {
			get { return sortSystemUsingStatementsFirst; }
			set {
				if (sortSystemUsingStatementsFirst != value) {
					sortSystemUsingStatementsFirst = value;
					OnPropertyChanged(nameof(SortSystemUsingStatementsFirst));
				}
			}
		}
		bool sortSystemUsingStatementsFirst = true;

		public int MaxArrayElements {
			get { return maxArrayElements; }
			set {
				if (maxArrayElements != value) {
					maxArrayElements = value;
					OnPropertyChanged(nameof(MaxArrayElements));
				}
			}
		}
		// Don't show too big arrays, no-one will read every single element, and too big
		// arrays could cause OOM exceptions.
		int maxArrayElements = 10000;

		public int MaxStringLength {
			get { return maxStringLength; }
			set {
				if (maxStringLength != value) {
					maxStringLength = value;
					OnPropertyChanged(nameof(MaxStringLength));
				}
			}
		}
		int maxStringLength = ConstMaxStringLength;
		public const int ConstMaxStringLength = 20000;

		public bool SortCustomAttributes {
			get { return sortCustomAttributes; }
			set {
				if (sortCustomAttributes != value) {
					sortCustomAttributes = value;
					OnPropertyChanged(nameof(SortCustomAttributes));
				}
			}
		}
		bool sortCustomAttributes = false;

		public bool UseSourceCodeOrder {
			get { return useSourceCodeOrder; }
			set {
				if (useSourceCodeOrder != value) {
					useSourceCodeOrder = value;
					OnPropertyChanged(nameof(UseSourceCodeOrder));
				}
			}
		}
		bool useSourceCodeOrder = true;

		public bool AllowFieldInitializers {
			get { return allowFieldInitializers; }
			set {
				if (allowFieldInitializers != value) {
					allowFieldInitializers = value;
					OnPropertyChanged(nameof(AllowFieldInitializers));
				}
			}
		}
		bool allowFieldInitializers = true;

		public bool OneCustomAttributePerLine {
			get { return oneCustomAttributePerLine; }
			set {
				if (oneCustomAttributePerLine != value) {
					oneCustomAttributePerLine = value;
					OnPropertyChanged(nameof(OneCustomAttributePerLine));
				}
			}
		}
		bool oneCustomAttributePerLine = true;

		public bool TypeAddInternalModifier {
			get { return typeAddInternalModifier; }
			set {
				if (typeAddInternalModifier != value) {
					typeAddInternalModifier = value;
					OnPropertyChanged(nameof(TypeAddInternalModifier));
				}
			}
		}
		bool typeAddInternalModifier = true;

		public bool MemberAddPrivateModifier {
			get { return memberAddPrivateModifier; }
			set {
				if (memberAddPrivateModifier != value) {
					memberAddPrivateModifier = value;
					OnPropertyChanged(nameof(MemberAddPrivateModifier));
				}
			}
		}
		bool memberAddPrivateModifier = true;

		public bool RemoveNewDelegateClass {
			get { return removeNewDelegateClass; }
			set {
				if (removeNewDelegateClass != value) {
					removeNewDelegateClass = value;
					OnPropertyChanged(nameof(RemoveNewDelegateClass));
				}
			}
		}
		bool removeNewDelegateClass = true;

		public bool HexadecimalNumbers {
			get { return hexadecimalNumbers; }
			set {
				if (hexadecimalNumbers != value) {
					hexadecimalNumbers = value;
					OnPropertyChanged(nameof(HexadecimalNumbers));
				}
			}
		}
		bool hexadecimalNumbers = false;

		public bool EmitCalliAsInvocationExpression {
			get { return emitCalliAsInvocationExpression; }
			set {
				if (emitCalliAsInvocationExpression != value) {
					emitCalliAsInvocationExpression = value;
					OnPropertyChanged(nameof(EmitCalliAsInvocationExpression));
				}
			}
		}
		bool emitCalliAsInvocationExpression = false;

		CSharpFormattingOptions csharpFormattingOptions;

		public CSharpFormattingOptions CSharpFormattingOptions {
			get {
				if (csharpFormattingOptions == null) {
					csharpFormattingOptions = FormattingOptionsFactory.CreateAllman();
					csharpFormattingOptions.IndentSwitchBody = false;
				}
				return csharpFormattingOptions;
			}
			set {
				if (value == null)
					throw new ArgumentNullException();
				if (!csharpFormattingOptions.Equals(value)) {
					csharpFormattingOptions = value;
					OnPropertyChanged(nameof(CSharpFormattingOptions));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public event EventHandler SettingsVersionChanged;

		protected virtual void OnPropertyChanged(string propertyName) {
			Interlocked.Increment(ref settingsVersion);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			OnModified();
			SettingsVersionChanged?.Invoke(this, EventArgs.Empty);
		}

		public int SettingsVersion => settingsVersion;
		volatile int settingsVersion;

		public DecompilerSettings Clone() {
			// DON'T use MemberwiseClone() since we want to return a DecompilerSettings, not any
			// derived class.
			return CopyTo(new DecompilerSettings());
		}

		public bool Equals(DecompilerSettings other) {
			if (other == null)
				return false;

			if (AnonymousMethods != other.AnonymousMethods) return false;
			if (ExpressionTrees != other.ExpressionTrees) return false;
			if (YieldReturn != other.YieldReturn) return false;
			if (AsyncAwait != other.AsyncAwait) return false;
			if (AutomaticProperties != other.AutomaticProperties) return false;
			if (AutomaticEvents != other.AutomaticEvents) return false;
			if (UsingStatement != other.UsingStatement) return false;
			if (ForEachStatement != other.ForEachStatement) return false;
			if (LockStatement != other.LockStatement) return false;
			if (SwitchStatementOnString != other.SwitchStatementOnString) return false;
			if (UsingDeclarations != other.UsingDeclarations) return false;
			if (QueryExpressions != other.QueryExpressions) return false;
			if (FullyQualifyAmbiguousTypeNames != other.FullyQualifyAmbiguousTypeNames) return false;
			if (FullyQualifyAllTypes != other.FullyQualifyAllTypes) return false;
			if (UseDebugSymbols != other.UseDebugSymbols) return false;
			if (ObjectOrCollectionInitializers != other.ObjectOrCollectionInitializers) return false;
			if (ShowXmlDocumentation != other.ShowXmlDocumentation) return false;
			if (RemoveEmptyDefaultConstructors != other.RemoveEmptyDefaultConstructors) return false;
			if (IntroduceIncrementAndDecrement != other.IntroduceIncrementAndDecrement) return false;
			if (MakeAssignmentExpressions != other.MakeAssignmentExpressions) return false;
			if (AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject != other.AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject) return false;
			if (ShowTokenAndRvaComments != other.ShowTokenAndRvaComments) return false;
			if (DecompilationObject0 != other.DecompilationObject0) return false;
			if (DecompilationObject1 != other.DecompilationObject1) return false;
			if (DecompilationObject2 != other.DecompilationObject2) return false;
			if (DecompilationObject3 != other.DecompilationObject3) return false;
			if (DecompilationObject4 != other.DecompilationObject4) return false;
			if (SortMembers != other.SortMembers) return false;
			if (ForceShowAllMembers != other.ForceShowAllMembers) return false;
			if (SortSystemUsingStatementsFirst != other.SortSystemUsingStatementsFirst) return false;
			if (MaxArrayElements != other.MaxArrayElements) return false;
			if (MaxStringLength != other.MaxStringLength) return false;
			if (SortCustomAttributes != other.SortCustomAttributes) return false;
			if (UseSourceCodeOrder != other.UseSourceCodeOrder) return false;
			if (AllowFieldInitializers != other.AllowFieldInitializers) return false;
			if (OneCustomAttributePerLine != other.OneCustomAttributePerLine) return false;
			if (TypeAddInternalModifier != other.TypeAddInternalModifier) return false;
			if (MemberAddPrivateModifier != other.MemberAddPrivateModifier) return false;
			if (RemoveNewDelegateClass != other.RemoveNewDelegateClass) return false;
			if (HexadecimalNumbers != other.HexadecimalNumbers) return false;
			if (EmitCalliAsInvocationExpression != other.EmitCalliAsInvocationExpression) return false;
			if (!CSharpFormattingOptions.Equals(other.CSharpFormattingOptions)) return false;

			return true;
		}

		public override bool Equals(object obj) {
			return Equals(obj as DecompilerSettings);
		}

		public override int GetHashCode() {
			unchecked {
				uint h = 0;

				h ^= AnonymousMethods				? 0 : 0x80000000U;
				h ^= ExpressionTrees				? 0 : 0x40000000U;
				h ^= YieldReturn					? 0 : 0x20000000U;
				h ^= AsyncAwait						? 0 : 0x10000000U;
				h ^= AutomaticProperties			? 0 : 0x08000000U;
				h ^= AutomaticEvents				? 0 : 0x04000000U;
				h ^= UsingStatement					? 0 : 0x02000000U;
				h ^= ForEachStatement				? 0 : 0x01000000U;
				h ^= LockStatement					? 0 : 0x00800000U;
				h ^= SwitchStatementOnString		? 0 : 0x00400000U;
				h ^= UsingDeclarations				? 0 : 0x00200000U;
				h ^= QueryExpressions				? 0 : 0x00100000U;
				h ^= FullyQualifyAmbiguousTypeNames	? 0 : 0x00080000U;
				h ^= UseDebugSymbols				? 0 : 0x00040000U;
				h ^= ObjectOrCollectionInitializers	? 0 : 0x00020000U;
				h ^= ShowXmlDocumentation			? 0 : 0x00010000U;
				h ^= IntroduceIncrementAndDecrement	? 0 : 0x00008000U;
				h ^= MakeAssignmentExpressions		? 0 : 0x00004000U;
				h ^= AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject ? 0 : 0x00002000U;
				h ^= RemoveEmptyDefaultConstructors	? 0 : 0x00001000U;
				h ^= ShowTokenAndRvaComments		? 0 : 0x00000800U;
				h ^= SortMembers					? 0 : 0x00000400U;
				h ^= ForceShowAllMembers			? 0 : 0x00000200U;
				h ^= SortSystemUsingStatementsFirst	? 0 : 0x00000100U;
				h ^= FullyQualifyAllTypes			? 0 : 0x00000080U;
				h ^= SortCustomAttributes			? 0 : 0x00000040U;
				h ^= UseSourceCodeOrder				? 0 : 0x00000020U;
				h ^= AllowFieldInitializers			? 0 : 0x00000010U;
				h ^= OneCustomAttributePerLine		? 0 : 0x00000008U;
				h ^= TypeAddInternalModifier		? 0 : 0x00000004U;
				h ^= MemberAddPrivateModifier		? 0 : 0x00000002U;
				h ^= RemoveNewDelegateClass			? 0 : 0x00000001U;
				h ^= HexadecimalNumbers				? 0 : 0x00000002U;
				h ^= EmitCalliAsInvocationExpression ? 0 : 0x00000004U;

				for (int i = 0; i < decompilationObjects.Length; i++)
					h ^= (uint)decompilationObjects[i] << (i * 8);

				h ^= (uint)MaxArrayElements;
				h ^= (uint)MaxStringLength;

				//TODO: CSharpFormattingOptions. This isn't currently used but it has a ton of properties

				return (int)h;
			}
		}

		public DecompilerSettings CopyTo(DecompilerSettings other) {
			other.DecompilationObject0 = this.DecompilationObject0;
			other.DecompilationObject1 = this.DecompilationObject1;
			other.DecompilationObject2 = this.DecompilationObject2;
			other.DecompilationObject3 = this.DecompilationObject3;
			other.DecompilationObject4 = this.DecompilationObject4;
			other.AnonymousMethods = this.AnonymousMethods;
			other.ExpressionTrees = this.ExpressionTrees;
			other.YieldReturn = this.YieldReturn;
			other.AsyncAwait = this.AsyncAwait;
			other.AutomaticProperties = this.AutomaticProperties;
			other.AutomaticEvents = this.AutomaticEvents;
			other.UsingStatement = this.UsingStatement;
			other.ForEachStatement = this.ForEachStatement;
			other.LockStatement = this.LockStatement;
			other.SwitchStatementOnString = this.SwitchStatementOnString;
			other.UsingDeclarations = this.UsingDeclarations;
			other.QueryExpressions = this.QueryExpressions;
			other.FullyQualifyAmbiguousTypeNames = this.FullyQualifyAmbiguousTypeNames;
			other.FullyQualifyAllTypes = this.FullyQualifyAllTypes;
			other.UseDebugSymbols = this.UseDebugSymbols;
			other.ObjectOrCollectionInitializers = this.ObjectOrCollectionInitializers;
			other.ShowXmlDocumentation = this.ShowXmlDocumentation;
			other.RemoveEmptyDefaultConstructors = this.RemoveEmptyDefaultConstructors;
			other.IntroduceIncrementAndDecrement = this.IntroduceIncrementAndDecrement;
			other.MakeAssignmentExpressions = this.MakeAssignmentExpressions;
			other.AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject = this.AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject;
			other.ShowTokenAndRvaComments = this.ShowTokenAndRvaComments;
			other.SortMembers = this.SortMembers;
			other.ForceShowAllMembers = this.ForceShowAllMembers;
			other.SortSystemUsingStatementsFirst = this.SortSystemUsingStatementsFirst;
			other.MaxArrayElements = this.MaxArrayElements;
			other.MaxStringLength = this.MaxStringLength;
			other.SortCustomAttributes = this.SortCustomAttributes;
			other.UseSourceCodeOrder = this.UseSourceCodeOrder;
			other.AllowFieldInitializers = this.AllowFieldInitializers;
			other.OneCustomAttributePerLine = this.OneCustomAttributePerLine;
			other.TypeAddInternalModifier = this.TypeAddInternalModifier;
			other.MemberAddPrivateModifier = this.MemberAddPrivateModifier;
			other.RemoveNewDelegateClass = this.RemoveNewDelegateClass;
			other.HexadecimalNumbers = this.HexadecimalNumbers;
			other.EmitCalliAsInvocationExpression = this.EmitCalliAsInvocationExpression;
			if (!this.CSharpFormattingOptions.Equals(other.CSharpFormattingOptions)) {
				this.CSharpFormattingOptions.CopyTo(other.CSharpFormattingOptions);
				other.OnPropertyChanged(nameof(other.CSharpFormattingOptions));
			}
			else {
				this.CSharpFormattingOptions.CopyTo(other.CSharpFormattingOptions);
			}
			return other;
		}
	}
}
