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

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// Provides <see cref="DotNetEmbeddedResource"/> instances
	/// </summary>
	public abstract class DotNetResourceProvider {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="file">File</param>
		protected DotNetResourceProvider(HexBufferFile file) => File = file ?? throw new ArgumentNullException(nameof(file));

		/// <summary>
		/// Gets the file
		/// </summary>
		public HexBufferFile File { get; }

		/// <summary>
		/// Gets the span of the .NET resources or an empty span if there are no .NET resources
		/// </summary>
		public abstract HexSpan ResourcesSpan { get; }

		/// <summary>
		/// Returns true if <paramref name="position"/> is probably within a resource
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract bool IsResourcePosition(HexPosition position);

		/// <summary>
		/// Gets a resource or null if <paramref name="position"/> isn't within a resource
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract DotNetEmbeddedResource? GetResource(HexPosition position);
	}
}
