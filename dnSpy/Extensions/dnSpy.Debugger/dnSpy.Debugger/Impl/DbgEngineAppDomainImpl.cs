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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;

namespace dnSpy.Debugger.Impl {
	sealed class DbgEngineAppDomainImpl : DbgEngineAppDomain {
		public override DbgAppDomain AppDomain => appDomain;
		readonly DbgAppDomainImpl appDomain;

		public DbgEngineAppDomainImpl(DbgAppDomainImpl appDomain) => this.appDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));

		public override void Remove(DbgEngineMessageFlags messageFlags) => appDomain.Remove(messageFlags);

		public override void Update(UpdateOptions options, string? name, int id) => appDomain.Process.DbgManager.Dispatcher.BeginInvoke(() => {
			if (appDomain.IsClosed)
				return;
			if ((options & UpdateOptions.Name) != 0)
				appDomain.UpdateName_DbgThread(name);
			if ((options & UpdateOptions.Id) != 0)
				appDomain.UpdateId_DbgThread(id);
		});
	}
}
