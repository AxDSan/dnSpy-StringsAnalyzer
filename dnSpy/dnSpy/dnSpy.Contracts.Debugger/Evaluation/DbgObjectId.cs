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

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// References a value in the debugged process
	/// </summary>
	public abstract class DbgObjectId : DbgObject {
		/// <summary>
		/// Gets the process
		/// </summary>
		public DbgProcess Process => Runtime.Process;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public abstract DbgRuntime Runtime { get; }

		/// <summary>
		/// Gets the unique id in the runtime
		/// </summary>
		public abstract uint Id { get; }

		/// <summary>
		/// Creates a new value. The returned <see cref="DbgValue"/> is automatically closed when its runtime continues.
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <returns></returns>
		public abstract DbgValue GetValue(DbgEvaluationInfo evalInfo);

		/// <summary>
		/// Removes and closes the object id
		/// </summary>
		public abstract void Remove();
	}
}
