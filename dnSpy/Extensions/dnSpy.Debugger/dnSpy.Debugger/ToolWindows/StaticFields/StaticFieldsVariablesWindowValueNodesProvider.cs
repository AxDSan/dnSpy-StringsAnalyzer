/*
    Copyright (C) 2022 ElektroKill

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

using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.Evaluation.UI;
using dnSpy.Debugger.Evaluation.ViewModel;

namespace dnSpy.Debugger.ToolWindows.StaticFields {
	sealed class StaticFieldsVariablesWindowValueNodesProvider : VariablesWindowValueNodesProvider {
		public override ValueNodesProviderResult GetNodes(DbgEvaluationInfo evalInfo, DbgLanguage language, DbgEvaluationOptions evalOptions, DbgValueNodeEvaluationOptions nodeEvalOptions, DbgValueFormatterOptions nameFormatterOptions) {
			var fields = language.StaticFieldsProvider.GetNodes(evalInfo, nodeEvalOptions);

			var res = new DbgValueNodeInfo[fields.Length];
			int ri = 0;
			for (int i = 0; i < fields.Length; i++, ri++)
				res[ri] = new DbgValueNodeInfo(fields[i], causesSideEffects: false);

			return new ValueNodesProviderResult(res, recreateAllNodes: false);
		}
	}
}
