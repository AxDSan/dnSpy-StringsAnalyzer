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
using System.Collections.Generic;
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Decompiles some methods
	/// </summary>
	public sealed class DecompileTypeMethods : DecompileTypeBase {
		/// <summary>
		/// Type to decompile
		/// </summary>
		public TypeDef Type { get; }

		/// <summary>
		/// Methods to decompile. All the other methods will have an empty body, except for possible
		/// <code>return default(XXX);</code> statements and other code to keep the compiler happy.
		/// </summary>
		public HashSet<MethodDef> Methods { get; }

		/// <summary>
		/// All nested types to show, not including their members
		/// </summary>
		public HashSet<TypeDef> Types { get; }

		/// <summary>
		/// true to decompile everything
		/// </summary>
		public bool ShowAll { get; set; }

		/// <summary>
		/// true to only decompile methods and members not stored in <see cref="Methods"/>,
		/// false to only decompile methods and members stored in <see cref="Methods"/>.
		/// </summary>
		public bool DecompileHidden { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="ctx">Context</param>
		/// <param name="type">Type</param>
		public DecompileTypeMethods(IDecompilerOutput output, DecompilationContext ctx, TypeDef type)
			: base(output, ctx) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Methods = new HashSet<MethodDef>();
			Types = new HashSet<TypeDef>();
			DecompileHidden = false;
		}
	}
}
