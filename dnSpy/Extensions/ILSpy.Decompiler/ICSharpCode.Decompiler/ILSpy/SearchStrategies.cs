﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.ILSpy
{
	abstract class AbstractSearchStrategy
	{
		protected string[] searchTerm;
		protected Regex regex;

		protected AbstractSearchStrategy(params string[] terms)
		{
			if (terms.Length == 1 && terms[0].Length > 2) {
				var search = terms[0];
				if (search.StartsWith("/", StringComparison.Ordinal) && search.EndsWith("/", StringComparison.Ordinal) && search.Length > 4)
					regex = SafeNewRegex(search.Substring(1, search.Length - 2));

				terms[0] = search;
			}

			searchTerm = terms;
		}

		protected bool IsMatch(string text)
		{
			if (regex != null)
				return regex.IsMatch(text);

			for (int i = 0; i < searchTerm.Length; ++i) {
				// How to handle overlapping matches?
				var term = searchTerm[i];
				if (string.IsNullOrEmpty(term)) continue;
				switch (term[0])
				{
					case '+': // must contain
						term = term.Substring(1);
						goto default;
					case '-': // should not contain
						if (term.Length > 1 && text.IndexOf(term.Substring(1), StringComparison.OrdinalIgnoreCase) >= 0)
							return false;
						break;
					case '=': // exact match
						{
							var equalCompareLength = text.IndexOf('`');
							if (equalCompareLength == -1)
								equalCompareLength = text.Length;

							if (term.Length > 1 && String.Compare(term, 1, text, 0, Math.Max(term.Length, equalCompareLength), StringComparison.OrdinalIgnoreCase) != 0)
								return false;
						}
						break;
					default:
						if (text.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0)
							return false;
						break;
				}
			}
			return true;
		}

		protected virtual bool IsMatch(FieldDefinition field)
		{
			return false;
		}

		protected virtual bool IsMatch(PropertyDefinition property)
		{
			return false;
		}

		protected virtual bool IsMatch(EventDefinition ev)
		{
			return false;
		}

		protected virtual bool IsMatch(MethodDefinition m)
		{
			return false;
		}

		void Add<T>(IEnumerable<T> items, TypeDefinition type, Language language, Action<SearchResult> addResult, Func<T, bool> matcher, Func<T, ImageSource> image) where T : MemberReference
		{
			foreach (var item in items) {
				if (matcher(item)) {
					addResult(new SearchResult
					{
						Member = item,
						Image = image(item),
						Name = item.Name,
						LocationImage = TypeTreeNode.GetIcon(type),
						Location = language.TypeToString(type, includeNamespace: true)
					});
				}
			}
		}

		public virtual void Search(TypeDefinition type, Language language, Action<SearchResult> addResult)
		{
			Add(type.Fields, type, language, addResult, IsMatch, FieldTreeNode.GetIcon);
			Add(type.Properties, type, language, addResult, IsMatch, p => PropertyTreeNode.GetIcon(p));
			Add(type.Events, type, language, addResult, IsMatch, EventTreeNode.GetIcon);
			Add(type.Methods.Where(NotSpecialMethod), type, language, addResult, IsMatch, MethodTreeNode.GetIcon);
		}

		bool NotSpecialMethod(MethodDefinition arg)
		{
			return (arg.SemanticsAttributes & (
				MethodSemanticsAttributes.Setter
				| MethodSemanticsAttributes.Getter
				| MethodSemanticsAttributes.AddOn
				| MethodSemanticsAttributes.RemoveOn
				| MethodSemanticsAttributes.Fire)) == 0;
		}

		Regex SafeNewRegex(string unsafePattern)
		{
			try {
				return new Regex(unsafePattern, RegexOptions.Compiled);
			} catch (ArgumentException) {
				return null;
			}
		}
	}

	class LiteralSearchStrategy : AbstractSearchStrategy
	{
		readonly TypeCode searchTermLiteralType;
		readonly object searchTermLiteralValue;

		public LiteralSearchStrategy(params string[] terms)
			: base(terms)
		{
			if (1 == searchTerm.Length) {
				var parser = new CSharpParser();
				var pe = parser.ParseExpression(searchTerm[0]) as PrimitiveExpression;

				if (pe != null && pe.Value != null) {
					TypeCode peValueType = Type.GetTypeCode(pe.Value.GetType());
					switch (peValueType) {
					case TypeCode.Byte:
					case TypeCode.SByte:
					case TypeCode.Int16:
					case TypeCode.UInt16:
					case TypeCode.Int32:
					case TypeCode.UInt32:
					case TypeCode.Int64:
					case TypeCode.UInt64:
						searchTermLiteralType = TypeCode.Int64;
						searchTermLiteralValue = CSharpPrimitiveCast.Cast(TypeCode.Int64, pe.Value, false);
						break;
					case TypeCode.Single:
					case TypeCode.Double:
					case TypeCode.String:
						searchTermLiteralType = peValueType;
						searchTermLiteralValue = pe.Value;
						break;
					}
				}
			}
		}

		protected override bool IsMatch(FieldDefinition field)
		{
			return IsLiteralMatch(field.Constant);
		}

		protected override bool IsMatch(PropertyDefinition property)
		{
			return MethodIsLiteralMatch(property.GetMethod) || MethodIsLiteralMatch(property.SetMethod);
		}

		protected override bool IsMatch(EventDefinition ev)
		{
			return MethodIsLiteralMatch(ev.AddMethod) || MethodIsLiteralMatch(ev.RemoveMethod) || MethodIsLiteralMatch(ev.InvokeMethod);
		}

		protected override bool IsMatch(MethodDefinition m)
		{
			return MethodIsLiteralMatch(m);
		}

		bool IsLiteralMatch(object val)
		{
			if (val == null)
				return false;
			switch (searchTermLiteralType) {
				case TypeCode.Int64:
					TypeCode tc = Type.GetTypeCode(val.GetType());
					if (tc >= TypeCode.SByte && tc <= TypeCode.UInt64)
						return CSharpPrimitiveCast.Cast(TypeCode.Int64, val, false).Equals(searchTermLiteralValue);
					else
						return false;
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.String:
					return searchTermLiteralValue.Equals(val);
				default:
					// substring search with searchTerm
					return IsMatch(val.ToString());
			}
		}

		bool MethodIsLiteralMatch(MethodDefinition m)
		{
			if (m == null)
				return false;
			var body = m.Body;
			if (body == null)
				return false;
			if (searchTermLiteralType == TypeCode.Int64) {
				long val = (long)searchTermLiteralValue;
				foreach (var inst in body.Instructions) {
					switch (inst.OpCode.Code) {
					case Code.Ldc_I8:
						if (val == (long)inst.Operand)
							return true;
						break;
					case Code.Ldc_I4:
						if (val == (int)inst.Operand)
							return true;
						break;
					case Code.Ldc_I4_S:
						if (val == (sbyte)inst.Operand)
							return true;
						break;
					case Code.Ldc_I4_M1:
						if (val == -1)
							return true;
						break;
					case Code.Ldc_I4_0:
						if (val == 0)
							return true;
						break;
					case Code.Ldc_I4_1:
						if (val == 1)
							return true;
						break;
					case Code.Ldc_I4_2:
						if (val == 2)
							return true;
						break;
					case Code.Ldc_I4_3:
						if (val == 3)
							return true;
						break;
					case Code.Ldc_I4_4:
						if (val == 4)
							return true;
						break;
					case Code.Ldc_I4_5:
						if (val == 5)
							return true;
						break;
					case Code.Ldc_I4_6:
						if (val == 6)
							return true;
						break;
					case Code.Ldc_I4_7:
						if (val == 7)
							return true;
						break;
					case Code.Ldc_I4_8:
						if (val == 8)
							return true;
						break;
					}
				}
			} else if (searchTermLiteralType != TypeCode.Empty) {
				Code expectedCode;
				switch (searchTermLiteralType) {
				case TypeCode.Single:
					expectedCode = Code.Ldc_R4;
					break;
				case TypeCode.Double:
					expectedCode = Code.Ldc_R8;
					break;
				case TypeCode.String:
					expectedCode = Code.Ldstr;
					break;
				default:
					throw new InvalidOperationException();
				}
				foreach (var inst in body.Instructions) {
					if (inst.OpCode.Code == expectedCode && searchTermLiteralValue.Equals(inst.Operand))
						return true;
				}
			} else {
				foreach (var inst in body.Instructions) {
					if (inst.OpCode.Code == Code.Ldstr && IsMatch((string)inst.Operand))
						return true;
				}
			}
			return false;
		}
	}

	enum MemberSearchKind
	{
		All,
		Field,
		Property,
		Event,
		Method
	}

	class MemberSearchStrategy : AbstractSearchStrategy
	{
		MemberSearchKind searchKind;

		public MemberSearchStrategy(string term, MemberSearchKind searchKind = MemberSearchKind.All)
			: this(new[] { term }, searchKind)
		{
		}

		public MemberSearchStrategy(string[] terms, MemberSearchKind searchKind = MemberSearchKind.All)
			: base(terms)
		{
			this.searchKind = searchKind;
		}

		protected override bool IsMatch(FieldDefinition field)
		{
			return (searchKind == MemberSearchKind.All || searchKind == MemberSearchKind.Field) && IsMatch(field.Name);
		}

		protected override bool IsMatch(PropertyDefinition property)
		{
			return (searchKind == MemberSearchKind.All || searchKind == MemberSearchKind.Property) && IsMatch(property.Name);
		}

		protected override bool IsMatch(EventDefinition ev)
		{
			return (searchKind == MemberSearchKind.All || searchKind == MemberSearchKind.Event) && IsMatch(ev.Name);
		}

		protected override bool IsMatch(MethodDefinition m)
		{
			return (searchKind == MemberSearchKind.All || searchKind == MemberSearchKind.Method) && IsMatch(m.Name);
		}
	}

	class TypeSearchStrategy : AbstractSearchStrategy
	{
		public TypeSearchStrategy(params string[] terms)
			: base(terms)
		{
		}

		public override void Search(TypeDefinition type, Language language, Action<SearchResult> addResult)
		{
			if (IsMatch(type.Name) || IsMatch(type.FullName)) {
				addResult(new SearchResult {
					Member = type,
					Image = TypeTreeNode.GetIcon(type),
					Name = language.TypeToString(type, includeNamespace: false),
					LocationImage = type.DeclaringType != null ? TypeTreeNode.GetIcon(type.DeclaringType) : Images.Namespace,
					Location = type.DeclaringType != null ? language.TypeToString(type.DeclaringType, includeNamespace: true) : type.Namespace
				});
			}

			foreach (TypeDefinition nestedType in type.NestedTypes) {
				Search(nestedType, language, addResult);
			}
		}
	}

	class TypeAndMemberSearchStrategy : AbstractSearchStrategy
	{
		public TypeAndMemberSearchStrategy(params string[] terms)
			: base(terms)
		{
		}

		public override void Search(TypeDefinition type, Language language, Action<SearchResult> addResult)
		{
			if (IsMatch(type.Name) || IsMatch(type.FullName))
			{
				addResult(new SearchResult
				{
					Member = type,
					Image = TypeTreeNode.GetIcon(type),
					Name = language.TypeToString(type, includeNamespace: false),
					LocationImage = type.DeclaringType != null ? TypeTreeNode.GetIcon(type.DeclaringType) : Images.Namespace,
					Location = type.DeclaringType != null ? language.TypeToString(type.DeclaringType, includeNamespace: true) : type.Namespace
				});
			}

			foreach (TypeDefinition nestedType in type.NestedTypes)
			{
				Search(nestedType, language, addResult);
			}

			base.Search(type, language, addResult);
		}

		protected override bool IsMatch(FieldDefinition field)
		{
			return IsMatch(field.Name);
		}

		protected override bool IsMatch(PropertyDefinition property)
		{
			return IsMatch(property.Name);
		}

		protected override bool IsMatch(EventDefinition ev)
		{
			return IsMatch(ev.Name);
		}

		protected override bool IsMatch(MethodDefinition m)
		{
			return IsMatch(m.Name);
		}
	}
}
