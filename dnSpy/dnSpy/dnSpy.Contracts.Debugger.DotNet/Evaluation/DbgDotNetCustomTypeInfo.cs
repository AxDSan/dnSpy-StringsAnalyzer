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
using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation {
	/// <summary>
	/// Extra custom type info provided by the expression compiler and used by language formatters
	/// </summary>
	public sealed class DbgDotNetCustomTypeInfo : IEquatable<DbgDotNetCustomTypeInfo> {
		/// <summary>
		/// Gets the custom type info ID
		/// </summary>
		public Guid CustomTypeInfoId { get; }

		/// <summary>
		/// Gets the custom type info
		/// </summary>
		public ReadOnlyCollection<byte> CustomTypeInfo { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="customTypeInfoId">Custom type info ID</param>
		/// <param name="customTypeInfo">Custom type info</param>
		public DbgDotNetCustomTypeInfo(Guid customTypeInfoId, ReadOnlyCollection<byte> customTypeInfo) {
			CustomTypeInfoId = customTypeInfoId;
			CustomTypeInfo = customTypeInfo ?? throw new ArgumentNullException(nameof(customTypeInfo));
		}

		/// <inheritdoc />
		public override bool Equals(object? obj) {
			if (obj is not DbgDotNetCustomTypeInfo other)
				return false;
			return Equals(other);
		}

		/// <inheritdoc />
		public bool Equals(DbgDotNetCustomTypeInfo? other) {
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			if (CustomTypeInfoId != other.CustomTypeInfoId)
				return false;
			if (CustomTypeInfo == other.CustomTypeInfo)
				return true;
			if (CustomTypeInfo.Count != other.CustomTypeInfo.Count)
				return false;
			for (int i = 0; i < CustomTypeInfo.Count; i++) {
				if (CustomTypeInfo[i] != other.CustomTypeInfo[i])
					return false;
			}
			return true;
		}

		/// <inheritdoc />
		public override int GetHashCode() => CustomTypeInfoId.GetHashCode() * 397 ^ CustomTypeInfo.GetHashCode();
	}
}
