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

using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter {
	sealed class DefaultArgumentsProviderImpl : VariablesProvider {
		readonly IDbgDotNetRuntime runtime;
		public DefaultArgumentsProviderImpl(IDbgDotNetRuntime runtime) {
			evalInfo = null!;
			this.runtime = runtime;
		}

		DbgEvaluationInfo evalInfo;

		public override void Initialize(DbgEvaluationInfo evalInfo, DmdMethodBase method, DmdMethodBody body) => this.evalInfo = evalInfo;

		public override DbgDotNetValue? GetValueAddress(int index, DmdType targetType) =>
			runtime.GetParameterValueAddress(evalInfo, (uint)index, targetType);

		public override DbgDotNetValueResult GetVariable(int index) =>
			runtime.GetParameterValue(evalInfo, (uint)index);

		public override string? SetVariable(int index, DmdType targetType, object? value) =>
			runtime.SetParameterValue(evalInfo, (uint)index, targetType, value);

		public override bool CanDispose(DbgDotNetValue value) => true;

		public override void Clear() => evalInfo = null!;
	}
}
