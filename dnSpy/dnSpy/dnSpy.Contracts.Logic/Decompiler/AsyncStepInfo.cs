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
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Async method step info
	/// </summary>
	public readonly struct AsyncStepInfo {
		/// <summary>
		/// Offset in method where it starts waiting for the result
		/// </summary>
		public uint YieldOffset { get; }

		/// <summary>
		/// Resume method
		/// </summary>
		public MethodDef ResumeMethod { get; }

		/// <summary>
		/// Offset in <see cref="ResumeMethod"/> where it resumes after the result is available
		/// </summary>
		public uint ResumeOffset { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="yieldOffset">Offset in <see cref="System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext"/> where it starts waiting for the result</param>
		/// <param name="resumeMethod">Resume method</param>
		/// <param name="resumeOffset">Offset in <paramref name="resumeMethod"/> where it resumes after the result is available</param>
		public AsyncStepInfo(uint yieldOffset, MethodDef resumeMethod, uint resumeOffset) {
			YieldOffset = yieldOffset;
			ResumeMethod = resumeMethod ?? throw new ArgumentNullException(nameof(resumeMethod));
			ResumeOffset = resumeOffset;
		}
	}
}
