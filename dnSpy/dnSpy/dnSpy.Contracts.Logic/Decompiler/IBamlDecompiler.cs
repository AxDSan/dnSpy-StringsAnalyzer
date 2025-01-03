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

using System.Collections.Generic;
using System.IO;
using System.Threading;
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Baml to xaml decompiler
	/// </summary>
	public interface IBamlDecompiler {
		/// <summary>
		/// Decompiles baml to xaml. Returns all assembly references.
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="data">Baml data</param>
		/// <param name="token">Cancellation token</param>
		/// <param name="bamlDecompilerOptions">Options</param>
		/// <param name="output">Output stream</param>
		/// <param name="outputOptions">Output options</param>
		/// <returns></returns>
		IList<string> Decompile(ModuleDef module, byte[] data, CancellationToken token, BamlDecompilerOptions bamlDecompilerOptions, Stream output, XamlOutputOptions outputOptions);
	}

	/// <summary>
	/// Provides <see cref="XamlOutputOptions"/>
	/// </summary>
	public interface IXamlOutputOptionsProvider {
		/// <summary>
		/// Gets the default options that gets changed by the user
		/// </summary>
		XamlOutputOptions Default { get; }
	}

	/// <summary>
	/// XAML output options
	/// </summary>
	public sealed class XamlOutputOptions {
		/// <summary>
		/// Indent characters
		/// </summary>
		public string? IndentChars { get; set; }

		/// <summary>
		/// Newline characters
		/// </summary>
		public string? NewLineChars { get; set; }

		/// <summary>
		/// Add a newline after each attribute
		/// </summary>
		public bool NewLineOnAttributes { get; set; }
	}
}
