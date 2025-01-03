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
using System.Linq;

using ICSharpCode.Decompiler.ILAst;
using dnlib.DotNet;
using System.Text;
using System.Globalization;

namespace ICSharpCode.Decompiler.Ast {
	public class NameVariables
	{
		static readonly Dictionary<string, string> typeNameToVariableNameDict = new Dictionary<string, string> {
			{ "System.Boolean", "flag" },
			{ "System.Byte", "b" },
			{ "System.SByte", "b" },
			{ "System.Int16", "num" },
			{ "System.Int32", "num" },
			{ "System.Int64", "num" },
			{ "System.UInt16", "num" },
			{ "System.UInt32", "num" },
			{ "System.UInt64", "num" },
			{ "System.Single", "num" },
			{ "System.Double", "num" },
			{ "System.Decimal", "num" },
			{ "System.String", "text" },
			{ "System.Object", "obj" },
			{ "System.Char", "c" }
		};

		readonly List<ILWhileLoop> GenerateNameForVariable_Loops;

		public NameVariables(StringBuilder sb) {
			this.stringBuilder = sb;
			this.GenerateNameForVariable_Loops = new List<ILWhileLoop>();
			this.proposedStoreNames = new Dictionary<ILVariable, List<string>>();
			this.proposedLoadNames = new Dictionary<ILVariable, List<string>>();
		}
		readonly StringBuilder stringBuilder;

		public static void AssignNamesToVariables(DecompilerContext context, IList<ILVariable> parameters, HashSet<ILVariable> variables, ILBlock methodBody, StringBuilder stringBuilder)
		{
			NameVariables nv = new NameVariables(stringBuilder);
			nv.context = context;
			nv.fieldNamesInCurrentType = context.CurrentType.Fields.Select(f => f.Name.String).ToList();
			// First mark existing variable names as reserved.
			foreach (string name in context.ReservedVariableNames)
				nv.AddExistingName(name);
			foreach (var p in parameters)
				nv.AddExistingName(p.Name);
			foreach (var l in variables) {
				if (l.Renamed)
					nv.AddExistingName(l.Name);
			}
			foreach (var v in variables) {
				if (v.Renamed)
					continue;
				if (v.OriginalVariable != null && context.Settings.UseDebugSymbols) {
					string varName = v.OriginalVariable.Name;
					v.Name = GetName(nv, varName);
				} else {
					v.Name = GetName(nv, TryGetLocalName(v));
				}
			}

			// Gather possible names for all variables in the body based on stloc and ldloc instructions.
			foreach (var ilExpression in methodBody.GetSelfAndChildrenRecursive<ILExpression>()) {
				if (ilExpression.Code == ILCode.Stloc && ilExpression.Operand is ILVariable storedVar) {
					var name = nv.GetNameFromExpression(ilExpression.Arguments.Single());
					if (nv.fieldNamesInCurrentType.Contains(name))
						continue;
					if (!nv.proposedStoreNames.TryGetValue(storedVar, out var storeNames))
						storeNames = nv.proposedStoreNames[storedVar] = new List<string>();
					storeNames.Add(name);
				}
				for (int i = 0; i < ilExpression.Arguments.Count; i++) {
					var argument = ilExpression.Arguments[i];
					if (argument.Code == ILCode.Ldloc && ilExpression.Operand is ILVariable loadedVar) {
						var name = nv.GetNameForArgument(argument, i);
						if (nv.fieldNamesInCurrentType.Contains(name))
							continue;
						if (!nv.proposedLoadNames.TryGetValue(loadedVar, out var loadNames))
							loadNames = nv.proposedLoadNames[loadedVar] = new List<string>();
						loadNames.Add(name);
					}
				}
			}

			// Now generate names:
			foreach (ILVariable p in parameters) {
				if (p.Renamed)
					continue;
				p.Renamed = true;
				if (string.IsNullOrEmpty(p.Name))
					p.Name = nv.GenerateNameForVariable(p, methodBody);
			}
			foreach (ILVariable varDef in variables) {
				if (varDef.Renamed)
					continue;
				varDef.Renamed = true;
				if (string.IsNullOrEmpty(varDef.Name))
					varDef.Name = nv.GenerateNameForVariable(varDef, methodBody);
			}
		}

		readonly Dictionary<ILVariable, List<string>> proposedStoreNames;
		readonly Dictionary<ILVariable, List<string>> proposedLoadNames;

		static string GetName(NameVariables nv, string name) {
			if (string.IsNullOrEmpty(name) || name.StartsWith("V_", StringComparison.Ordinal) || !IsValidName(name)) {
				// don't use the name from the debug symbols if it looks like a generated name
				return null;
			}

			// use the name from the debug symbols
			// (but ensure we don't use the same name for two variables)
			return nv.GetAlternativeName(name);
		}

		static string TryGetLocalName(ILVariable v) {
			if (v.GeneratedByDecompiler)
				return null;
			if (v.OriginalParameter != null)
				return null;
			if (v.OriginalVariable != null)
				return null;
			var name = v.Name;
			if (name.Length == 0)
				return null;
			char c = name[0];

			if (c == '<') {
				int index = name.IndexOf('>', 1);
				if (index < 0)
					return null;
				if (index + 1 >= name.Length)
					return null;
				var type = name[index + 1];
				// Microsoft.CodeAnalysis.CSharp.Symbols.GeneratedNameKind.HoistedLocalField = '5'
				// mcs doesn't use such a char, eg.: "<newString>__1"
				if (type != '5' && type != '_')
					return null;
				return name.Substring(1, index - 1);
			}

			if (c == '$') {
				// Microsoft.CodeAnalysis.VisualBasic.StringConstants
				const string HoistedUserVariablePrefix = "$VB$Local_";
				const string StateMachineHoistedUserVariablePrefix = "$VB$ResumableLocal_";
				if (name.StartsWith(HoistedUserVariablePrefix))
					return name.Substring(HoistedUserVariablePrefix.Length);
				if (name.StartsWith(StateMachineHoistedUserVariablePrefix)) {
					int index = name.IndexOf('$', StateMachineHoistedUserVariablePrefix.Length);
					if (index >= 0) {
						var newName = name.Substring(StateMachineHoistedUserVariablePrefix.Length, index - StateMachineHoistedUserVariablePrefix.Length);
						// Compiler generated: "$VB$ResumableLocal_VB$t_ref$L0$1"
						if (newName != "VB")
							return newName;
					}
					return null;
				}
			}

			return null;
		}

		static bool IsValidName(string varName)
		{
			if (string.IsNullOrEmpty(varName))
				return false;
			if (!(char.IsLetter(varName[0]) || varName[0] == '_'))
				return false;
			for (int i = 1; i < varName.Length; i++) {
				if (!(char.IsLetterOrDigit(varName[i]) || varName[i] == '_'))
					return false;
			}
			return true;
		}

		DecompilerContext context;
		List<string> fieldNamesInCurrentType;
		readonly Dictionary<string, int> typeNames = new Dictionary<string, int>();

		public void AddExistingName(string name)
		{
			if (string.IsNullOrEmpty(name))
				return;
			int number;
			string nameWithoutDigits = SplitName(name, out number);
			int existingNumber;
			if (typeNames.TryGetValue(nameWithoutDigits, out existingNumber)) {
				typeNames[nameWithoutDigits] = Math.Max(number, existingNumber);
			} else {
				typeNames.Add(nameWithoutDigits, number);
			}
		}

		string SplitName(string name, out int number)
		{
			// First, identify whether the name already ends with a number:
			int pos = name.Length;
			while (pos > 0 && name[pos-1] >= '0' && name[pos-1] <= '9')
				pos--;
			if (pos < name.Length) {
				if (int.TryParse(name.Substring(pos), out number)) {
					return name.Substring(0, pos);
				}
			}
			number = 1;
			return name;
		}

		const char maxLoopVariableName = 'n';

		public string GetAlternativeName(string oldVariableName)
		{
			if (oldVariableName.Length == 1 && oldVariableName[0] >= 'i' && oldVariableName[0] <= maxLoopVariableName) {
				for (char c = 'i'; c <= maxLoopVariableName; c++) {
					if (!typeNames.ContainsKey(c.ToString())) {
						typeNames.Add(c.ToString(), 1);
						return c.ToString();
					}
				}
			}

			int number;
			string nameWithoutDigits = SplitName(oldVariableName, out number);

			if (!typeNames.ContainsKey(nameWithoutDigits)) {
				typeNames.Add(nameWithoutDigits, number - 1);
			}
			int count = ++typeNames[nameWithoutDigits];
			if (count != 1) {
				return nameWithoutDigits + count.ToString();
			} else {
				return nameWithoutDigits;
			}
		}

		string TryGetDisplayClassVariableName(ILVariable variable)
		{
			var td = variable.Type.RemovePinnedAndModifiers().ToTypeDefOrRef().ResolveTypeDef();
			if (td == null)
				return null;
			if (!td.IsNested)
				return null;
			if (!td.CustomAttributes.IsDefined("System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
				return null;
			var typeName = td.Name.String;

			//TODO: Should be true if we're decompiling VB code
			bool isVisualBasic = false;

			const string DisplayClassPrefix = "_Closure$__";
			const string ClosureVariablePrefix = "$VB$Closure_";
			const string CSharpDisplayClassPrefix = "<>c__DisplayClass";// See Roslyn: MakeLambdaDisplayClassName
			const string LambdaDisplayLocalName = "CS$<>8__locals";// See Roslyn: MakeLambdaDisplayLocalName
			if (typeName.StartsWith(CSharpDisplayClassPrefix) || typeName.StartsWith(DisplayClassPrefix)) {
				if (isVisualBasic)
					return ClosureVariablePrefix;
				return LambdaDisplayLocalName;
			}

			return null;
		}

		string GenerateNameForVariable(ILVariable variable, ILBlock methodBody)
		{
			string proposedName = null;
			bool useCounter = false;
			if (string.IsNullOrEmpty(proposedName)) {
				proposedName = TryGetDisplayClassVariableName(variable);
				useCounter = proposedName != null;
			}
			if (string.IsNullOrEmpty(proposedName) && new SigComparer().Equals(variable.GetVariableType(), context.CurrentType.Module.CorLibTypes.Int32)) {
				// test whether the variable might be a loop counter
				bool isLoopCounter = false;
				foreach (ILWhileLoop loop in methodBody.GetSelfAndChildrenRecursive(GenerateNameForVariable_Loops)) {
					ILExpression expr = loop.Condition;
					while (expr != null && expr.Code == ILCode.LogicNot)
						expr = expr.Arguments[0];
					if (expr != null) {
						switch (expr.Code) {
							case ILCode.Clt:
							case ILCode.Clt_Un:
							case ILCode.Cgt:
							case ILCode.Cgt_Un:
							case ILCode.Cle:
							case ILCode.Cle_Un:
							case ILCode.Cge:
							case ILCode.Cge_Un:
								ILVariable loadVar;
								if (expr.Arguments[0].Match(ILCode.Ldloc, out loadVar) && loadVar == variable) {
									isLoopCounter = true;
								}
								break;
						}
					}
				}
				if (isLoopCounter) {
					// For loop variables, use i,j,k,l,m,n
					for (char c = 'i'; c <= maxLoopVariableName; c++) {
						if (!typeNames.ContainsKey(c.ToString())) {
							proposedName = c.ToString();
							break;
						}
					}
				}
			}
			if (string.IsNullOrEmpty(proposedName) && proposedStoreNames.TryGetValue(variable, out var proposedNameForStores)) {
				if (proposedNameForStores.Count == 1) {
					proposedName = proposedNameForStores[0];
				}
			}
			if (string.IsNullOrEmpty(proposedName) && proposedLoadNames.TryGetValue(variable, out var proposedNameForLoads)) {
				if (proposedNameForLoads.Count == 1) {
					proposedName = proposedNameForLoads[0];
				}
			}
			if (string.IsNullOrEmpty(proposedName)) {
				proposedName = GetNameByType(variable.GetVariableType());
			}

			// remove any numbers from the proposed name
			int number;
			proposedName = SplitName(proposedName, out number);

			if (!typeNames.ContainsKey(proposedName)) {
				typeNames.Add(proposedName, 0);
			}
			int count = ++typeNames[proposedName];
			if (count > 1 || useCounter) {
				return proposedName + count.ToString();
			} else {
				return proposedName;
			}
		}

		string GetNameFromExpression(ILExpression expr)
		{
			switch (expr.Code) {
				case ILCode.Ldfld:
				case ILCode.Ldsfld:
					return CleanUpVariableName(((IField)expr.Operand).Name);
				case ILCode.Call:
				case ILCode.Callvirt:
				case ILCode.CallGetter:
				case ILCode.CallvirtGetter:
					IMethod mr = (IMethod)expr.Operand;
					if (mr.MethodSig.GetParameters().Count == 0 && mr.Name.StartsWith("get_", StringComparison.OrdinalIgnoreCase) && mr.Name != nameGetCurrent) {
						// use name from properties, but not from indexers
						return CleanUpVariableName(mr.Name.Substring(4));
					} else if (mr.Name.StartsWith("Get", StringComparison.OrdinalIgnoreCase) && mr.Name.String.Length >= 4 && char.IsUpper(mr.Name.String[3])) {
						// use name from Get-methods
						return CleanUpVariableName(mr.Name.Substring(3));
					}
					break;
			}
			return null;
		}
		static readonly UTF8String nameGetCurrent = new UTF8String("get_Current");

		string GetNameForArgument(ILExpression parent, int i)
		{
			switch (parent.Code) {
				case ILCode.Stfld:
				case ILCode.Stsfld:
					if (i == parent.Arguments.Count - 1) // last argument is stored value
						return CleanUpVariableName(((IField)parent.Operand).Name);
					else
						break;
				case ILCode.Call:
				case ILCode.Callvirt:
				case ILCode.Newobj:
				case ILCode.CallGetter:
				case ILCode.CallvirtGetter:
				case ILCode.CallSetter:
				case ILCode.CallvirtSetter:
				case ILCode.CallReadOnlySetter:
					IMethod methodRef = (IMethod)parent.Operand;
					if (methodRef.MethodSig.GetParameters().Count == 1 && i == parent.Arguments.Count - 1) {
						// argument might be value of a setter
						if (methodRef.Name.StartsWith("set_", StringComparison.OrdinalIgnoreCase) ||
							(parent.Code == ILCode.CallReadOnlySetter && methodRef.Name.StartsWith("get_", StringComparison.OrdinalIgnoreCase))) {
							var name = methodRef.Name.Substring(4);
							if (name != "Current")
								return CleanUpVariableName(name);
						} else if (methodRef.Name.StartsWith("Set", StringComparison.OrdinalIgnoreCase) && methodRef.Name.String.Length >= 4 && char.IsUpper(methodRef.Name.String[3])) {
							return CleanUpVariableName(methodRef.Name.Substring(3));
						}
					}
					MethodDef methodDef = methodRef.Resolve();
					if (methodDef != null) {
						var p = methodDef.Parameters.ElementAtOrDefault(i + (parent.Code == ILCode.Newobj ? 1 : 0));
						if (p != null && !string.IsNullOrEmpty(p.Name))
							return CleanUpVariableName(p.Name);
					}
					break;
				case ILCode.Ret:
					return "result";
			}
			return null;
		}

		static readonly UTF8String systemString = new UTF8String("System");
		static readonly UTF8String nullableString = new UTF8String("Nullable`1");
		string GetNameByType(TypeSig type)
		{
			type = type.RemoveModifiers();

			GenericInstSig git = type as GenericInstSig;
			if (git != null && git.GenericType != null && git.GenericArguments.Count == 1 && git.GenericType.TypeDefOrRef.Compare(systemString, nullableString)) {
				type = ((GenericInstSig)type).GenericArguments[0];
			}

			string name;
			if (type == null)
				return string.Empty;
			if (type.IsSingleOrMultiDimensionalArray) {
				name = "array";
			} else if (type.IsPointer || type.IsByRef) {
				name = "ptr";
			} else {
				stringBuilder.Clear();
				if (FullNameFactory.NameSB(type, false, stringBuilder).EndsWith("Exception")) {
					name = "ex";
				}
				else {
					stringBuilder.Clear();
					if (!typeNameToVariableNameDict.TryGetValue(FullNameFactory.FullName(type, false, null, null, null, stringBuilder), out name)) {
						stringBuilder.Clear();
						var builder = FullNameFactory.NameSB(type, false, stringBuilder);
						// remove the 'I' for interfaces
						if (builder.Length >= 3 && builder[0] == 'I' && char.IsUpper(builder[1]) && char.IsLower(builder[2]))
							builder.Remove(0, 1);
						name = CleanUpVariableName(builder.ToString());
					}
				}
			}
			return name;
		}

		string CleanUpVariableName(string name)
		{
			var sb = stringBuilder;
			sb.Clear();
			// remove the backtick (generics)
			int pos = name.LastIndexOf('`');
			if (pos < 0)
				pos = name.Length;
			for (int i = 0; i < pos; i++) {
				var c = name[i];
				if (IsValidChar(c))
					sb.Append(c);
				else {
					sb.Append("_u");
					sb.Append(((ushort)c).ToString("X4"));
				}
			}

			// remove field prefix:
			if (sb.Length > 2 && sb[0] == 'm' && sb[1] == '_')
				sb.Remove(0, 2);
			else if (sb.Length > 1 && sb[0] == '_' && (char.IsLetter(sb[1]) || sb[1] == '_'))
				sb.Remove(0, 1);

			if (sb.Length == 0)
				return "obj";

			for (int i = 0; i < sb.Length; i++) {
				var origChar = sb[i];
				var newChar = char.ToLowerInvariant(origChar);
				if (origChar == newChar)
					break;
				sb[i] = newChar;
			}
			return sb.ToString();
		}

		static bool IsValidChar(char c) {
			if (0x21 <= c && c <= 0x7E)
				return true;
			if (c <= 0x20)
				return false;

			switch (char.GetUnicodeCategory(c)) {
			case UnicodeCategory.UppercaseLetter:
			case UnicodeCategory.LowercaseLetter:
			case UnicodeCategory.OtherLetter:
			case UnicodeCategory.DecimalDigitNumber:
				return true;

			case UnicodeCategory.TitlecaseLetter:
			case UnicodeCategory.ModifierLetter:
			case UnicodeCategory.NonSpacingMark:
			case UnicodeCategory.SpacingCombiningMark:
			case UnicodeCategory.EnclosingMark:
			case UnicodeCategory.LetterNumber:
			case UnicodeCategory.OtherNumber:
			case UnicodeCategory.SpaceSeparator:
			case UnicodeCategory.LineSeparator:
			case UnicodeCategory.ParagraphSeparator:
			case UnicodeCategory.Control:
			case UnicodeCategory.Format:
			case UnicodeCategory.Surrogate:
			case UnicodeCategory.PrivateUse:
			case UnicodeCategory.ConnectorPunctuation:
			case UnicodeCategory.DashPunctuation:
			case UnicodeCategory.OpenPunctuation:
			case UnicodeCategory.ClosePunctuation:
			case UnicodeCategory.InitialQuotePunctuation:
			case UnicodeCategory.FinalQuotePunctuation:
			case UnicodeCategory.OtherPunctuation:
			case UnicodeCategory.MathSymbol:
			case UnicodeCategory.CurrencySymbol:
			case UnicodeCategory.ModifierSymbol:
			case UnicodeCategory.OtherSymbol:
			case UnicodeCategory.OtherNotAssigned:
			default:
				return false;
			}
		}
	}
}
