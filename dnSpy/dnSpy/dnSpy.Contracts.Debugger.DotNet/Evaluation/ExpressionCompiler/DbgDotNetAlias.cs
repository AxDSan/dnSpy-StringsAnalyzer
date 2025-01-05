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

using System;

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler {
	/// <summary>
	/// Alias kind
	/// </summary>
	public enum DbgDotNetAliasKind {
		/// <summary>
		/// An exception, eg. "$exception"
		/// </summary>
		Exception,

		/// <summary>
		/// A stowed exception, eg. "$stowedexception"
		/// </summary>
		StowedException,

		/// <summary>
		/// A return value, eg. "$ReturnValue", "$ReturnValue1"
		/// </summary>
		ReturnValue,

		/// <summary>
		/// A variable created by the user that doesn't exist in code
		/// </summary>
		Variable,

		/// <summary>
		/// An object ID, eg. "$1", "$3"
		/// </summary>
		ObjectId,
	}

	/// <summary>
	/// An alias (eg. return value, object id, etc)
	/// </summary>
	public readonly struct DbgDotNetAlias {
		/// <summary>
		/// Alias kind
		/// </summary>
		public readonly DbgDotNetAliasKind Kind;

		/// <summary>
		///	Custom type info understood by the EE or null
		/// </summary>
		public readonly DbgDotNetCustomTypeInfo? CustomTypeInfo;

		/// <summary>
		/// Name, eg. "$ReturnValue", "$1"
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// Serialized type name, see <see cref="Type.AssemblyQualifiedName"/>
		/// </summary>
		public readonly string Type;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="kind">Alias kind</param>
		/// <param name="type">Serialized type name, see <see cref="Type.AssemblyQualifiedName"/></param>
		/// <param name="name">Name, eg. "$ReturnValue", "$1"</param>
		/// <param name="customTypeInfo">Custom type info understood by the EE or null</param>
		public DbgDotNetAlias(DbgDotNetAliasKind kind, string type, string name, DbgDotNetCustomTypeInfo? customTypeInfo) {
			Kind = kind;
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Name = name ?? throw new ArgumentNullException(nameof(name));
			CustomTypeInfo = customTypeInfo;
		}
	}
}
