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

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Decompiler;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineStaticFieldsProviderImpl : DbgEngineValueNodeProvider {
		readonly DbgDotNetEngineValueNodeFactory valueNodeFactory;
		readonly DbgDotNetFormatter formatter;

		public DbgEngineStaticFieldsProviderImpl(DbgDotNetEngineValueNodeFactory valueNodeFactory, DbgDotNetFormatter formatter) {
			this.valueNodeFactory = valueNodeFactory ?? throw new ArgumentNullException(nameof(valueNodeFactory));
			this.formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
		}

		public override DbgEngineValueNode[] GetNodes(DbgEvaluationInfo evalInfo, DbgValueNodeEvaluationOptions options) {
			var dispatcher = evalInfo.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return GetNodesCore(evalInfo, options);
			return GetNodes2(dispatcher, evalInfo, options);

			DbgEngineValueNode[] GetNodes2(DbgDotNetDispatcher dispatcher2, DbgEvaluationInfo evalInfo2, DbgValueNodeEvaluationOptions options2) {
				if (!dispatcher2.TryInvokeRethrow(() => GetNodesCore(evalInfo2, options2), out var result))
					result = Array.Empty<DbgEngineValueNode>();
				return result;
			}
		}

		DbgEngineValueNode[] GetNodesCore(DbgEvaluationInfo evalInfo, DbgValueNodeEvaluationOptions options) {
			DbgEngineValueNode[]? valueNodes = null;
			try {
				var runtime = evalInfo.Runtime.GetDotNetRuntime();
				var method = runtime.GetFrameMethod(evalInfo);
				if (method is null)
					return Array.Empty<DbgEngineValueNode>();

				var fields = new HashSet<DmdFieldInfo>(DmdMemberInfoEqualityComparer.DefaultMember);

				var fieldsFromBody = CollectStaticFieldsFromMethodBody(method);
				if (fieldsFromBody is not null) {
					for (int i = 0; i < fieldsFromBody.Count; i++)
						fields.Add(fieldsFromBody[i]);
				}

				if (method.DeclaringType is not null) {
					foreach (var field in method.DeclaringType.Fields.Where(x => x.IsStatic && !x.IsLiteral && x.DeclaringType is not null))
						fields.Add(field);
				}

				valueNodes = new DbgEngineValueNode[fields.Count];
				int j = 0;
				var output = ObjectCache.AllocDotNetTextOutput();
				foreach (var field in fields) {
					var fieldVal = runtime.LoadField(evalInfo, null, field);

					formatter.FormatType(evalInfo, output, field.DeclaringType!, null, DbgValueFormatterTypeOptions.IntrinsicTypeKeywords | DbgValueFormatterTypeOptions.Namespaces, null);
					output.Write(DbgTextColor.Punctuation, ".");
					output.Write(GetFieldColor(field), IdentifierEscaper.Escape(field.Name));

					var fieldExpression = output.CreateAndReset();

					if (fieldVal.HasError)
						valueNodes[j++] = valueNodeFactory.CreateError(evalInfo, fieldExpression, fieldVal.ErrorMessage!, fieldExpression.ToString(), false);
					else
						valueNodes[j++] = valueNodeFactory.Create(evalInfo, fieldExpression, fieldVal.Value!, null, options, fieldExpression.ToString(), GetFieldImageName(field), false, false, field.FieldType);
				}
				ObjectCache.Free(ref output);

				return valueNodes;
			}
			catch (Exception ex) {
				if (valueNodes is not null)
					evalInfo.Runtime.Process.DbgManager.Close(valueNodes.Where(a => a is not null));
				if (!ExceptionUtils.IsInternalDebuggerError(ex))
					throw;
				return valueNodeFactory.CreateInternalErrorResult(evalInfo);
			}
		}

		static IList<DmdFieldInfo>? CollectStaticFieldsFromMethodBody(DmdMethodBase method) {
			var body = method.GetMethodBody();
			if (body is null)
				return null;
			var ilBytes = body.GetILAsByteArray();

			var fields = new List<DmdFieldInfo>();

			uint offset = 0;
			while (offset < ilBytes.Length) {
				var op = ilBytes[offset++];
				var opCode = op == 0xFE ? OpCodes.TwoByteOpCodes[ilBytes[offset++]] : OpCodes.OneByteOpCodes[op];
				switch (opCode.Code) {
				case dnlib.DotNet.Emit.Code.Switch: {
					var numTargets = ilBytes[offset++] | (uint)ilBytes[offset++] << 8 | (uint)ilBytes[offset++] << 16 | (uint)ilBytes[offset++] << 24;
					offset += numTargets * 4;
					break;
				}
				case dnlib.DotNet.Emit.Code.Ldsfld:
				case dnlib.DotNet.Emit.Code.Ldsflda:
				case dnlib.DotNet.Emit.Code.Stsfld: {
					var token = ilBytes[offset++] | ilBytes[offset++] << 8 | ilBytes[offset++] << 16 | ilBytes[offset++] << 24;
					var field = method.Module.ResolveField(token, method.ReflectedType?.GetGenericArguments(), method.GetGenericArguments(), DmdResolveOptions.None);
					if (field is not null)
						fields.Add(field);
					break;
				}
				default:
					offset += GetOperandLength(opCode);
					break;
				}
			}

			return fields;
		}

		static uint GetOperandLength(OpCode opCode) {
			switch (opCode.OperandType) {
			case OperandType.ShortInlineBrTarget:
			case OperandType.ShortInlineI:
			case OperandType.ShortInlineVar:
				return 1;
			case OperandType.InlineVar:
				return 2;
			case OperandType.InlineBrTarget:
			case OperandType.InlineField:
			case OperandType.InlineI:
			case OperandType.InlineMethod:
			case OperandType.InlineSig:
			case OperandType.InlineString:
			case OperandType.InlineTok:
			case OperandType.InlineType:
			case OperandType.ShortInlineR:
				return 4;
			case OperandType.InlineI8:
			case OperandType.InlineR:
				return 8;
			case OperandType.InlineNone:
			case OperandType.InlinePhi:
			default:
				return 0;
			}
		}

		static DbgTextColor GetFieldColor(DmdFieldInfo field) {
			if (field.ReflectedType is not null && field.ReflectedType.IsEnum)
				return DbgTextColor.EnumField;
			if (field.IsLiteral)
				return DbgTextColor.LiteralField;
			if (field.IsStatic)
				return DbgTextColor.StaticField;
			return DbgTextColor.InstanceField;
		}

		static string GetFieldImageName(DmdFieldInfo field) {
			if (field.IsPrivate)
				return PredefinedDbgValueNodeImageNames.FieldPrivate;
			if (field.IsPublic)
				return PredefinedDbgValueNodeImageNames.FieldPublic;
			if (field.IsFamily)
				return PredefinedDbgValueNodeImageNames.FieldFamily;
			if (field.IsAssembly)
				return PredefinedDbgValueNodeImageNames.FieldAssembly;
			if (field.IsFamilyAndAssembly)
				return PredefinedDbgValueNodeImageNames.FieldFamilyAndAssembly;
			if (field.IsFamilyOrAssembly)
				return PredefinedDbgValueNodeImageNames.FieldFamilyOrAssembly;
			if (field.IsPrivateScope)
				return PredefinedDbgValueNodeImageNames.FieldCompilerControlled;
			return PredefinedDbgValueNodeImageNames.Field;
		}
	}
}
