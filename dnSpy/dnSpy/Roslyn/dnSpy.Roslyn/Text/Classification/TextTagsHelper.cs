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

using System.Diagnostics;
using dnSpy.Contracts.Text;
using Microsoft.CodeAnalysis;

namespace dnSpy.Roslyn.Text.Classification {
	/// <summary>
	/// Converts <see cref="TextTags"/> tags to <see cref="TextColor"/> values
	/// </summary>
	static class TextTagsHelper {
		/// <summary>
		/// Converts <paramref name="textTag"/> to a <see cref="TextColor"/> value
		/// </summary>
		/// <param name="textTag">One of the text tags found in <see cref="TextTags"/></param>
		/// <returns></returns>
		public static TextColor ToTextColor(string? textTag) {
			if (textTag is null)
				return TextColor.Text;
			switch (textTag) {
			case TextTags.Alias:			return TextColor.Namespace;
			case TextTags.AnonymousTypeIndicator:return TextColor.Text; // AnonymousType or Tuple
			case TextTags.Assembly:			return TextColor.Assembly;
			case TextTags.Class:			return TextColor.Type;
			case TextTags.Constant:			return TextColor.LiteralField;
			case TextTags.Delegate:			return TextColor.Delegate;
			case TextTags.Enum:				return TextColor.Enum;
			case TextTags.EnumMember:		return TextColor.EnumField;
			case TextTags.ErrorType:		return TextColor.Error;
			case TextTags.Event:			return TextColor.InstanceEvent;
			case TextTags.ExtensionMethod:	return TextColor.ExtensionMethod;
			case TextTags.Field:			return TextColor.InstanceField;
			case TextTags.Interface:		return TextColor.Interface;
			case TextTags.Keyword:			return TextColor.Keyword;
			case TextTags.Label:			return TextColor.Label;
			case TextTags.LineBreak:		return TextColor.Text;
			case TextTags.Local:			return TextColor.Local;
			case TextTags.Method:			return TextColor.InstanceMethod;
			case TextTags.Module:			return TextColor.Module;
			case TextTags.Namespace:		return TextColor.Namespace;
			case TextTags.NumericLiteral:	return TextColor.Number;
			case TextTags.Operator:			return TextColor.Operator;
			case TextTags.Parameter:		return TextColor.Parameter;
			case TextTags.Property:			return TextColor.InstanceProperty;
			case TextTags.Punctuation:		return TextColor.Punctuation;
			case TextTags.RangeVariable:	return TextColor.Local;
			case TextTags.Record:			return TextColor.Type;
			case TextTags.RecordStruct:		return TextColor.ValueType;
			case TextTags.Space:			return TextColor.Text;
			case TextTags.StringLiteral:	return TextColor.String;
			case TextTags.Struct:			return TextColor.ValueType;
			case TextTags.Text:				return TextColor.Text;
			case TextTags.TypeParameter:	return TextColor.TypeGenericParameter;
			case "ContainerStart":			return TextColor.Text;
			case "ContainerEnd":			return TextColor.Text;
			case "CodeBlockStart":			return TextColor.Text;
			case "CodeBlockEnd":			return TextColor.Text;
			default:
				Debug.Fail($"New {nameof(TextTags)} tag: {textTag}");
				return TextColor.Text;
			}
		}
	}
}
