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

namespace dndbg.Engine {
	/// <summary>
	/// Identical to <c>COR_DEBUG_STEP_RANGE</c>
	/// </summary>
	struct StepRange {
		/// <summary>
		/// Start offset relative to the beginning of the method
		/// </summary>
		public uint StartOffset;	// must be first

		/// <summary>
		/// End offset (exclusive)
		/// </summary>
		public uint EndOffset;		// must be 2nd and last

		public StepRange(uint start, uint end) {
			StartOffset = start;
			EndOffset = end;
		}
	}
}
