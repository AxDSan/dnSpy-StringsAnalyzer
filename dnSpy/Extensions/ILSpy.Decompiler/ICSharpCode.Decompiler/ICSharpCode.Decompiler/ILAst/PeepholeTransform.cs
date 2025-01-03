// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;

namespace ICSharpCode.Decompiler.ILAst {
	public partial class ILAstOptimizer
	{
		#region TypeConversionSimplifications
		bool TypeConversionSimplifications(ILBlockBase block, List<ILNode> body, ILExpression expr, int pos)
		{
			bool modified = false;
			modified |= TransformDecimalCtorToConstant(expr);
			modified |= SimplifyLdcI4ConvI8(expr);
			modified |= RemoveConvIFromArrayCreation(expr);
			for (int i = 0; i < expr.Arguments.Count; i++) {
				modified |= TypeConversionSimplifications(block, null, expr.Arguments[i], -1);
			}
			return modified;
		}

		static readonly UTF8String systemString = new UTF8String("System");
		static readonly UTF8String decimalString = new UTF8String("Decimal");
		bool TransformDecimalCtorToConstant(ILExpression expr)
		{
			IMethod r;
			IField f;
			List<ILExpression> args;
			if (expr.Match(ILCode.Newobj, out r, out args)) {
				if (!r.DeclaringType.Compare(systemString, decimalString))
					return false;
				var sig = r.MethodSig;
				if (sig == null || sig.GetGenParamCount() != 0)
					return false;
				if (args.Count == 1) {
					int val;
					long val64;
					if (args[0].Match(ILCode.Ldc_I4, out val)) {
						if (sig.Params.Count != 1)
							return false;
						var paramType = sig.Params[0].RemovePinnedAndModifiers().GetElementType();
						if (paramType != ElementType.I4 && paramType != ElementType.U4)
							return false;
						expr.Code = ILCode.Ldc_Decimal;
						expr.Operand = paramType == ElementType.I4 ? new decimal(val) : new decimal((uint)val);
						expr.InferredType = r.DeclaringType.ToTypeSig();
						if (context.CalculateILSpans) {
							for (int i = 0; i < expr.Arguments.Count; i++)
								expr.Arguments[i].AddSelfAndChildrenRecursiveILSpans(expr.ILSpans);
						}
						expr.Arguments.Clear();
						return true;
					} else if (MatchLdci8(args[0], out val64)) {
						if (sig.Params.Count != 1)
							return false;
						var paramType = sig.Params[0].RemovePinnedAndModifiers().GetElementType();
						if (paramType != ElementType.I8 && paramType != ElementType.U8)
							return false;
						expr.Code = ILCode.Ldc_Decimal;
						expr.Operand = paramType == ElementType.I8 ? new decimal(val64) : new decimal((ulong)val64);
						expr.InferredType = r.DeclaringType.ToTypeSig();
						if (context.CalculateILSpans) {
							for (int i = 0; i < expr.Arguments.Count; i++)
								expr.Arguments[i].AddSelfAndChildrenRecursiveILSpans(expr.ILSpans);
						}
						expr.Arguments.Clear();
						return true;
					}
				} else if (args.Count == 5) {
					int lo, mid, hi, isNegative, scale;
					if (expr.Arguments[0].Match(ILCode.Ldc_I4, out lo) &&
					    expr.Arguments[1].Match(ILCode.Ldc_I4, out mid) &&
					    expr.Arguments[2].Match(ILCode.Ldc_I4, out hi) &&
					    expr.Arguments[3].Match(ILCode.Ldc_I4, out isNegative) &&
					    expr.Arguments[4].Match(ILCode.Ldc_I4, out scale))
					{
						expr.Code = ILCode.Ldc_Decimal;
						expr.Operand = new decimal(lo, mid, hi, isNegative != 0, (byte)Math.Min(28, (uint)scale));
						expr.InferredType = r.DeclaringType.ToTypeSig();
						if (context.CalculateILSpans) {
							for (int i = 0; i < expr.Arguments.Count; i++)
								expr.Arguments[i].AddSelfAndChildrenRecursiveILSpans(expr.ILSpans);
						}
						expr.Arguments.Clear();
						return true;
					}
				}
			} else if (expr.Match(ILCode.Call, out r, out args)) {
				if (!r.DeclaringType.Compare(systemString, decimalString))
					return false;
				if (r.Name != nameCtor)
					return false;
				if (args.Count == 0)
					return false;
				var sig = r.MethodSig;
				if (sig == null || sig.GetGenParamCount() != 0)
					return false;
				ILVariable v;
				if (!args[0].Match(ILCode.Ldloca, out v))
					return false;
				if (args.Count == 2) {
					int val;
					long val64;
					if (args[1].Match(ILCode.Ldc_I4, out val)) {
						if (sig.Params.Count != 1)
							return false;
						var paramType = sig.Params[0].RemovePinnedAndModifiers().GetElementType();
						if (paramType != ElementType.I4 && paramType != ElementType.U4)
							return false;
						var ldcExpr = new ILExpression(ILCode.Ldc_Decimal, paramType == ElementType.I4 ? new decimal(val) : new decimal((uint)val));
						ldcExpr.InferredType = r.DeclaringType.ToTypeSig();
						if (context.CalculateILSpans) {
							for (int i = 0; i < expr.Arguments.Count; i++)
								expr.Arguments[i].AddSelfAndChildrenRecursiveILSpans(expr.ILSpans);
						}
						expr.Code = ILCode.Stloc;
						expr.Operand = v;
						expr.Arguments.Clear();
						expr.Arguments.Add(ldcExpr);
						expr.InferredType = ldcExpr.InferredType;
						expr.ExpectedType = null;
						return true;
					} else if (MatchLdci8(args[1], out val64)) {
						if (sig.Params.Count != 1)
							return false;
						var paramType = sig.Params[0].RemovePinnedAndModifiers().GetElementType();
						if (paramType != ElementType.I8 && paramType != ElementType.U8)
							return false;
						var ldcExpr = new ILExpression(ILCode.Ldc_Decimal, paramType == ElementType.I8 ? new decimal(val64) : new decimal((ulong)val64));
						ldcExpr.InferredType = r.DeclaringType.ToTypeSig();
						if (context.CalculateILSpans) {
							for (int i = 0; i < expr.Arguments.Count; i++)
								expr.Arguments[i].AddSelfAndChildrenRecursiveILSpans(expr.ILSpans);
						}
						expr.Code = ILCode.Stloc;
						expr.Operand = v;
						expr.Arguments.Clear();
						expr.Arguments.Add(ldcExpr);
						expr.InferredType = ldcExpr.InferredType;
						expr.ExpectedType = null;
						return true;
					}
				} else if (args.Count == 6) {
					int lo, mid, hi, isNegative, scale;
					if (expr.Arguments[1].Match(ILCode.Ldc_I4, out lo) &&
					    expr.Arguments[2].Match(ILCode.Ldc_I4, out mid) &&
					    expr.Arguments[3].Match(ILCode.Ldc_I4, out hi) &&
					    expr.Arguments[4].Match(ILCode.Ldc_I4, out isNegative) &&
					    expr.Arguments[5].Match(ILCode.Ldc_I4, out scale))
					{
						var ldcExpr = new ILExpression(ILCode.Ldc_Decimal, new decimal(lo, mid, hi, isNegative != 0, (byte)Math.Min(28, (uint)scale)));
						ldcExpr.InferredType = r.DeclaringType.ToTypeSig();
						if (context.CalculateILSpans) {
							for (int i = 0; i < expr.Arguments.Count; i++)
								expr.Arguments[i].AddSelfAndChildrenRecursiveILSpans(expr.ILSpans);
						}
						expr.Code = ILCode.Stloc;
						expr.Operand = v;
						expr.Arguments.Clear();
						expr.Arguments.Add(ldcExpr);
						expr.InferredType = ldcExpr.InferredType;
						expr.ExpectedType = null;
						return true;
					}
				}
			} else if (expr.Match(ILCode.Ldsfld, out f)) {
				if (!f.DeclaringType.Compare(systemString, decimalString))
					return false;
				decimal value;
				if (f.Name == "MinValue")
					value = decimal.MinValue;
				else if (f.Name == "MaxValue")
					value = decimal.MaxValue;
				else if (f.Name == "Zero")
					value = decimal.Zero;
				else if (f.Name == "MinusOne")
					value = decimal.MinusOne;
				else if (f.Name == "One")
					value = decimal.One;
				else
					return false;
				expr.Code = ILCode.Ldc_Decimal;
				expr.Operand = value;
				expr.InferredType = f.DeclaringType.ToTypeSig();
				return true;
			}
			return false;
		}

		static bool MatchLdci8(ILExpression expr, out long value) {
			if (expr.Match(ILCode.Ldc_I8, out value))
				return true;
			if (expr.Code == ILCode.Conv_I8 || expr.Code == ILCode.Conv_U8) {
				int value32;
				if (expr.Arguments[0].Match(ILCode.Ldc_I4, out value32)) {
					value = expr.Code == ILCode.Conv_I8 ? (long)value32 : (long)(uint)value32;
					return true;
				}
			}
			return false;
		}

		bool SimplifyLdcI4ConvI8(ILExpression expr)
		{
			ILExpression ldc;
			int val;
			if (expr.Match(ILCode.Conv_I8, out ldc) && ldc.Match(ILCode.Ldc_I4, out val)) {
				expr.Code = ILCode.Ldc_I8;
				expr.Operand = (long)val;
				if (context.CalculateILSpans) {
					for (int i = 0; i < expr.Arguments.Count; i++)
						expr.Arguments[i].AddSelfAndChildrenRecursiveILSpans(expr.ILSpans);
				}
				expr.Arguments.Clear();
				return true;
			}
			return false;
		}

		bool RemoveConvIFromArrayCreation(ILExpression expr)
		{
			ITypeDefOrRef typeRef;
			ILExpression length;
			ILExpression input;
			if (expr.Match(ILCode.Newarr, out typeRef, out length)) {
				if (length.Match(ILCode.Conv_Ovf_I, out input) || length.Match(ILCode.Conv_I, out input)
				    || length.Match(ILCode.Conv_Ovf_I_Un, out input) || length.Match(ILCode.Conv_U, out input))
				{
					expr.Arguments[0] = input;
					if (context.CalculateILSpans)
						input.ILSpans.AddRange(length.ILSpans);	// no recursive add
					return true;
				}
			}
			return false;
		}
		#endregion

		#region SimplifyLdObjAndStObj
		bool SimplifyLdObjAndStObj(ILBlockBase block, List<ILNode> body, ILExpression expr, int pos)
		{
			bool modified = false;
			expr = SimplifyLdObjAndStObj(expr, ref modified);
			if (modified && body != null)
				body[pos] = expr;
			for (int i = 0; i < expr.Arguments.Count; i++) {
				expr.Arguments[i] = SimplifyLdObjAndStObj(expr.Arguments[i], ref modified);
				modified |= SimplifyLdObjAndStObj(block, null, expr.Arguments[i], -1);
			}
			return modified;
		}

		ILExpression SimplifyLdObjAndStObj(ILExpression expr, ref bool modified)
		{
			if (expr.Code == ILCode.Initobj) {
				expr.Code = ILCode.Stobj;
				expr.Arguments.Add(new ILExpression(ILCode.DefaultValue, expr.Operand));
				modified = true;
			} else if (expr.Code == ILCode.Cpobj) {
				expr.Code = ILCode.Stobj;
				expr.Arguments[1] = new ILExpression(ILCode.Ldobj, expr.Operand, expr.Arguments[1]);
				modified = true;
			}
			ILExpression arg, arg2;
			ITypeDefOrRef type;
			ILCode? newCode = null;
			if (expr.Match(ILCode.Stobj, out type, out arg, out arg2)) {
				switch (arg.Code) {
						case ILCode.Ldelema: newCode = ILCode.Stelem; break;
						case ILCode.Ldloca:  newCode = ILCode.Stloc; break;
						case ILCode.Ldflda:  newCode = ILCode.Stfld; break;
						case ILCode.Ldsflda: newCode = ILCode.Stsfld; break;
				}
			} else if (expr.Match(ILCode.Ldobj, out type, out arg)) {
				switch (arg.Code) {
						case ILCode.Ldelema: newCode = ILCode.Ldelem; break;
						case ILCode.Ldloca:  newCode = ILCode.Ldloc; break;
						case ILCode.Ldflda:  newCode = ILCode.Ldfld; break;
						case ILCode.Ldsflda: newCode = ILCode.Ldsfld; break;
				}
			}
			if (newCode != null) {
				arg.Code = newCode.Value;
				if (expr.Code == ILCode.Stobj) {
					arg.InferredType = expr.InferredType;
					arg.ExpectedType = expr.ExpectedType;
					arg.Arguments.Add(arg2);
				}
				if (context.CalculateILSpans)
					arg.ILSpans.AddRange(expr.ILSpans);
				modified = true;
				return arg;
			} else {
				return expr;
			}
		}
		#endregion

		#region CachedDelegateInitialization
		void CachedDelegateInitializationWithField(ILBlock block, ref int i)
		{
			// if (logicnot(ldsfld(field))) {
			//     stsfld(field, newobj(Action::.ctor, ldnull(), ldftn(method)))
			// } else {
			// }
			// ...(..., ldsfld(field), ...)

			ILCondition c = block.Body[i] as ILCondition;
			if (c == null || c.Condition == null && c.TrueBlock == null || c.FalseBlock == null)
				return;
			if (!(c.TrueBlock.Body.Count == 1 && c.FalseBlock.Body.Count == 0))
				return;
			if (!c.Condition.Match(ILCode.LogicNot))
				return;
			ILExpression condition = c.Condition.Arguments.Single() as ILExpression;
			if (condition == null || condition.Code != ILCode.Ldsfld)
				return;
			FieldDef field = condition.Operand is MemberRef ?
				((MemberRef)condition.Operand).ResolveFieldWithinSameModule() :
				(FieldDef)condition.Operand; // field is defined in current assembly
			if (field == null || !field.IsCompilerGeneratedOrIsInCompilerGeneratedClass())
				return;
			ILExpression stsfld = c.TrueBlock.Body[0] as ILExpression;
			if (!(stsfld != null && stsfld.Code == ILCode.Stsfld && ((IField)stsfld.Operand).ResolveFieldWithinSameModule() == field))
				return;
			ILExpression newObj = stsfld.Arguments[0];
			if (!(newObj.Code == ILCode.Newobj && newObj.Arguments.Count == 2))
				return;
			if (newObj.Arguments[0].Code != ILCode.Ldnull)
				return;
			if (newObj.Arguments[1].Code != ILCode.Ldftn)
				return;
			MethodDef anonymousMethod = ((IMethod)newObj.Arguments[1].Operand).ResolveMethodWithinSameModule(); // method is defined in current assembly
			if (!Ast.Transforms.DelegateConstruction.IsAnonymousMethod(context, anonymousMethod) && !Ast.AstBuilder.IsAnonymousMethodCacheField(field))
				return;

			ILNode followingNode = block.Body.ElementAtOrDefault(i + 1);
			if (followingNode != null && followingNode.GetSelfAndChildrenRecursive<ILExpression>(Optimize_List_ILExpression).Count(
				e => e.Code == ILCode.Ldsfld && ((IField)e.Operand).ResolveFieldWithinSameModule() == field) == 1)
			{
				for (int k = 0; k < Optimize_List_ILExpression.Count; k++) {
					var parent = Optimize_List_ILExpression[k];
					for (int j = 0; j < parent.Arguments.Count; j++) {
						if (parent.Arguments[j].Code == ILCode.Ldsfld && ((IField)parent.Arguments[j].Operand).ResolveFieldWithinSameModule() == field) {
							if (context.CalculateILSpans) {
								long index = 0;
								bool done = false;
								for (;;) {
									var b = c.GetAllILSpans(ref index, ref done);
									if (done)
										break;
									newObj.ILSpans.Add(b);
								}
								c.Condition.AddSelfAndChildrenRecursiveILSpans(newObj.ILSpans);
								c.FalseBlock.AddSelfAndChildrenRecursiveILSpans(newObj.ILSpans);

								index = 0;
								done = false;
								for (;;) {
									var b = c.TrueBlock.GetAllILSpans(ref index, ref done);
									if (done)
										break;
									newObj.ILSpans.Add(b);
								}
								foreach (var instr in c.TrueBlock.Body.Skip(1))
									instr.AddSelfAndChildrenRecursiveILSpans(newObj.ILSpans);
								newObj.ILSpans.AddRange(stsfld.ILSpans);
								foreach (var arg in stsfld.Arguments.Skip(1))
									arg.AddSelfAndChildrenRecursiveILSpans(newObj.ILSpans);

								newObj.ILSpans.AddRange(parent.Arguments[j].ILSpans);
							}
							parent.Arguments[j] = newObj;
							block.Body.RemoveAt(i);
							i -= GetILInlining(method).InlineInto(block, block.Body, i, aggressive: false);
							return;
						}
					}
				}
			}
		}

		void CachedDelegateInitializationWithLocal(ILBlock block, ref int i)
		{
			// if (logicnot(ldloc(v))) {
			//     stloc(v, newobj(Action::.ctor, ldloc(displayClass), ldftn(method)))
			// } else {
			// }
			// ...(..., ldloc(v), ...)

			ILCondition c = block.Body[i] as ILCondition;
			if (c == null || c.Condition == null && c.TrueBlock == null || c.FalseBlock == null)
				return;
			if (!(c.TrueBlock.Body.Count == 1 && c.FalseBlock.Body.Count == 0))
				return;
			if (!c.Condition.Match(ILCode.LogicNot))
				return;
			ILExpression condition = c.Condition.Arguments.Single() as ILExpression;
			if (condition == null || condition.Code != ILCode.Ldloc)
				return;
			ILVariable v = (ILVariable)condition.Operand;
			ILExpression stloc = c.TrueBlock.Body[0] as ILExpression;
			if (!(stloc != null && stloc.Code == ILCode.Stloc && (ILVariable)stloc.Operand == v))
				return;
			ILExpression newObj = stloc.Arguments[0];
			if (!(newObj.Code == ILCode.Newobj && newObj.Arguments.Count == 2))
				return;
			if (newObj.Arguments[0].Code != ILCode.Ldloc)
				return;
			if (newObj.Arguments[1].Code != ILCode.Ldftn)
				return;
			MethodDef anonymousMethod = ((IMethod)newObj.Arguments[1].Operand).ResolveMethodWithinSameModule(); // method is defined in current assembly
			if (!Ast.Transforms.DelegateConstruction.IsAnonymousMethod(context, anonymousMethod))
				return;

			ILNode followingNode = block.Body.ElementAtOrDefault(i + 1);
			if (followingNode != null && followingNode.GetSelfAndChildrenRecursive<ILExpression>(Optimize_List_ILExpression).Count(
				e => e.Code == ILCode.Ldloc && (ILVariable)e.Operand == v) == 1)
			{
				ILInlining inlining = GetILInlining(method);
				if (!(inlining.numLdloc.GetOrDefault(v) == 2 && inlining.numStloc.GetOrDefault(v) == 2 && inlining.numLdloca.GetOrDefault(v) == 0))
					return;

				// Find the store instruction that initializes the local to null:
				var blockList = method.GetSelfAndChildrenRecursive<ILBlock>(Optimize_List_ILBlock);
				for (int k = 0; k < blockList.Count; k++) {
					var storeBlock = blockList[k];
					for (int j = 0; j < storeBlock.Body.Count; j++) {
						ILVariable storedVar;
						ILExpression storedExpr;
						if (storeBlock.Body[j].Match(ILCode.Stloc, out storedVar, out storedExpr) && storedVar == v && storedExpr.Match(ILCode.Ldnull)) {
							// Remove the instruction
							if (context.CalculateILSpans)
								Utils.AddILSpans(storeBlock, storeBlock.Body, j);
							storeBlock.Body.RemoveAt(j);
							if (storeBlock == block && j < i)
								i--;
							break;
						}
					}
				}

				if (context.CalculateILSpans) {
					long index = 0;
					bool done = false;
					for (;;) {
						var b = c.GetAllILSpans(ref index, ref done);
						if (done)
							break;
						stloc.ILSpans.Add(b);
					}

					c.Condition.AddSelfAndChildrenRecursiveILSpans(stloc.ILSpans);
					c.FalseBlock.AddSelfAndChildrenRecursiveILSpans(stloc.ILSpans);

					index = 0;
					done = false;
					for (;;) {
						var b = c.TrueBlock.GetAllILSpans(ref index, ref done);
						if (done)
							break;
						stloc.ILSpans.Add(b);
					}

					foreach (var instr in c.TrueBlock.Body.Skip(1))
						instr.AddSelfAndChildrenRecursiveILSpans(stloc.ILSpans);
				}

				block.Body[i] = stloc; // remove the 'if (v==null)'
				inlining = GetILInlining(method);
				inlining.InlineIfPossible(block, block.Body, ref i);
			}
		}
		#endregion

		#region MakeAssignmentExpression
		bool MakeAssignmentExpression(ILBlockBase block, List<ILNode> body, ILExpression expr, int pos)
		{
			// exprVar = ...
			// stloc(v, exprVar)
			// ->
			// exprVar = stloc(v, ...))
			ILVariable exprVar;
			ILExpression initializer;
			if (!(expr.Match(ILCode.Stloc, out exprVar, out initializer) && exprVar.GeneratedByDecompiler))
				return false;
			ILExpression nextExpr = body.ElementAtOrDefault(pos + 1) as ILExpression;
			if (nextExpr == null)
				return false;
			ILVariable v;
			ILExpression stLocArg;
			if (nextExpr.Match(ILCode.Stloc, out v, out stLocArg) && stLocArg.MatchLdloc(exprVar)) {
				ILExpression store2 = body.ElementAtOrDefault(pos + 2) as ILExpression;
				if (StoreCanBeConvertedToAssignment(store2, exprVar)) {
					// expr_44 = ...
					// stloc(v1, expr_44)
					// anystore(v2, expr_44)
					// ->
					// stloc(v1, anystore(v2, ...))
					ILInlining inlining = GetILInlining(method);
					if (inlining.numLdloc.GetOrDefault(exprVar) == 2 && inlining.numStloc.GetOrDefault(exprVar) == 1) {
						body.RemoveAt(pos + 2); // remove store2
						body.RemoveAt(pos); // remove expr = ...
						if (context.CalculateILSpans)
							nextExpr.Arguments[0].AddSelfAndChildrenRecursiveILSpans(nextExpr.ILSpans);
						nextExpr.Arguments[0] = store2;
						if (context.CalculateILSpans) {
							expr.AddSelfAndChildrenRecursiveILSpans(store2.ILSpans);
							store2.Arguments[store2.Arguments.Count - 1].AddSelfAndChildrenRecursiveILSpans(store2.ILSpans);
						}
						store2.Arguments[store2.Arguments.Count - 1] = initializer;

						inlining.InlineIfPossible(block, body, ref pos);

						return true;
					}
				}

				body.RemoveAt(pos + 1); // remove stloc
				if (context.CalculateILSpans)
					nextExpr.Arguments[0].AddSelfAndChildrenRecursiveILSpans(nextExpr.ILSpans);
				nextExpr.Arguments[0] = initializer;
				if (context.CalculateILSpans)
					expr.Arguments[0].AddSelfAndChildrenRecursiveILSpans(nextExpr.ILSpans);
				expr.Arguments[0] = nextExpr;
				return true;
			} else if ((nextExpr.Code == ILCode.Stsfld || nextExpr.Code == ILCode.CallSetter || nextExpr.Code == ILCode.CallvirtSetter) && nextExpr.Arguments.Count == 1) {
				// exprVar = ...
				// stsfld(fld, exprVar)
				// ->
				// exprVar = stsfld(fld, ...))
				if (nextExpr.Arguments[0].MatchLdloc(exprVar)) {
					body.RemoveAt(pos + 1); // remove stsfld
					if (context.CalculateILSpans)
						nextExpr.Arguments[0].AddSelfAndChildrenRecursiveILSpans(nextExpr.ILSpans);
					nextExpr.Arguments[0] = initializer;
					if (context.CalculateILSpans)
						expr.Arguments[0].AddSelfAndChildrenRecursiveILSpans(expr.ILSpans);
					expr.Arguments[0] = nextExpr;
					return true;
				}
			}
			return false;
		}

		bool StoreCanBeConvertedToAssignment(ILExpression store, ILVariable exprVar)
		{
			if (store == null)
				return false;
			switch (store.Code) {
				case ILCode.Stloc:
				case ILCode.Stfld:
				case ILCode.Stsfld:
				case ILCode.Stobj:
				case ILCode.CallSetter:
				case ILCode.CallvirtSetter:
				case ILCode.CallReadOnlySetter:
					break;
				default:
					if (!store.Code.IsStoreToArray())
						return false;
					break;
			}
			return store.Arguments.Last().Code == ILCode.Ldloc && store.Arguments.Last().Operand == exprVar;
		}
		#endregion

		#region MakeCompoundAssignments
		bool MakeCompoundAssignments(ILBlockBase block, List<ILNode> body, ILExpression expr, int pos)
		{
			bool modified = false;
			modified |= MakeCompoundAssignment(expr);
			// Static fields and local variables are not handled here - those are expressions without side effects
			// and get handled by ReplaceMethodCallsWithOperators
			// (which does a reversible transform to the short operator form, as the introduction of checked/unchecked might have to revert to the long form).
			for (int i = 0; i < expr.Arguments.Count; i++)
				modified |= MakeCompoundAssignments(block, null, expr.Arguments[i], -1);

			if (modified && body != null)
				GetILInlining(method).InlineInto(block, body, pos, aggressive: false);
			return modified;
		}

		bool MakeCompoundAssignment(ILExpression expr)
		{
			// stelem.any(T, ldloc(array), ldloc(pos), <OP>(ldelem.any(T, ldloc(array), ldloc(pos)), <RIGHT>))
			// or
			// stobj(T, ldloc(ptr), <OP>(ldobj(T, ldloc(ptr)), <RIGHT>))
			ILCode expectedLdelemCode;
			switch (expr.Code) {
				case ILCode.Stelem:
					expectedLdelemCode = ILCode.Ldelem;
					break;
				case ILCode.Stfld:
					expectedLdelemCode = ILCode.Ldfld;
					break;
				case ILCode.Stobj:
					expectedLdelemCode = ILCode.Ldobj;
					break;
				case ILCode.CallSetter:
					expectedLdelemCode = ILCode.CallGetter;
					break;
				case ILCode.CallvirtSetter:
					expectedLdelemCode = ILCode.CallvirtGetter;
					break;
				default:
					return false;
			}

			// all arguments except the last (so either array+pos, or ptr):
			bool hasGeneratedVar = false;
			for (int i = 0; i < expr.Arguments.Count - 1; i++) {
				ILVariable inputVar;
				if (!expr.Arguments[i].Match(ILCode.Ldloc, out inputVar))
					return false;
				hasGeneratedVar |= inputVar.GeneratedByDecompiler;
			}
			// At least one of the variables must be generated; otherwise we just keep the expanded form.
			// We do this because we want compound assignments to be represented in ILAst only when strictly necessary;
			// other compound assignments will be introduced by ReplaceMethodCallsWithOperator
			// (which uses a reversible transformation, see ReplaceMethodCallsWithOperator.RestoreOriginalAssignOperatorAnnotation)
			if (!hasGeneratedVar)
				return false;

			ILExpression op = expr.Arguments.Last();
			// in case of compound assignments with a lifted operator the result is inside NullableOf and the operand is inside ValueOf
			bool liftedOperator = false;
			if (op.Code == ILCode.NullableOf) {
				op = op.Arguments[0];
				liftedOperator = true;
			}
			if (!CanBeRepresentedAsCompoundAssignment(op))
				return false;

			ILExpression ldelem = op.Arguments[0];
			if (liftedOperator) {
				if (ldelem.Code != ILCode.ValueOf)
					return false;
				ldelem = ldelem.Arguments[0];
			}
			if (ldelem.Code != expectedLdelemCode)
				return false;
			Debug.Assert(ldelem.Arguments.Count == expr.Arguments.Count - 1);
			for (int i = 0; i < ldelem.Arguments.Count; i++) {
				if (!ldelem.Arguments[i].MatchLdloc((ILVariable)expr.Arguments[i].Operand))
					return false;
			}
			expr.Code = ILCode.CompoundAssignment;
			expr.Operand = null;
			if (context.CalculateILSpans) {
				for (int i = 0; i < ldelem.Arguments.Count; i++)
					expr.Arguments[i].AddSelfAndChildrenRecursiveILSpans(expr.ILSpans);
			}
			expr.Arguments.RemoveRange(0, ldelem.Arguments.Count);
			// result is "CompoundAssignment(<OP>(ldelem.any(...), <RIGHT>))"
			return true;
		}

		static bool CanBeRepresentedAsCompoundAssignment(ILExpression expr)
		{
			switch (expr.Code) {
				case ILCode.Add:
				case ILCode.Add_Ovf:
				case ILCode.Add_Ovf_Un:
				case ILCode.Sub:
				case ILCode.Sub_Ovf:
				case ILCode.Sub_Ovf_Un:
				case ILCode.Mul:
				case ILCode.Mul_Ovf:
				case ILCode.Mul_Ovf_Un:
				case ILCode.Div:
				case ILCode.Div_Un:
				case ILCode.Rem:
				case ILCode.Rem_Un:
				case ILCode.And:
				case ILCode.Or:
				case ILCode.Xor:
				case ILCode.Shl:
				case ILCode.Shr:
				case ILCode.Shr_Un:
					return true;
				case ILCode.Call:
					var m = expr.Operand as IMethod;
					if (m == null || m.MethodSig == null || m.MethodSig.HasThis || expr.Arguments.Count != 2) return false;
					switch (m.Name) {
						case "op_Addition":
						case "op_Subtraction":
						case "op_Multiply":
						case "op_Division":
						case "op_Modulus":
						case "op_BitwiseAnd":
						case "op_BitwiseOr":
						case "op_ExclusiveOr":
						case "op_LeftShift":
						case "op_RightShift":
							return true;
						default:
							return false;
					}
				default:
					return false;
			}
		}
		#endregion

		#region IntroducePostIncrement

		bool IntroducePostIncrement(ILBlockBase block, List<ILNode> body, ILExpression expr, int pos)
		{
			bool modified = IntroducePostIncrementForVariables(body, expr, pos);
			Debug.Assert(body[pos] == expr); // IntroducePostIncrementForVariables shouldn't change the expression reference
			ILExpression newExpr = IntroducePostIncrementForInstanceFields(expr);
			if (newExpr != null) {
				modified = true;
				body[pos] = newExpr;
				GetILInlining(method).InlineIfPossible(block, body, ref pos);
			}
			return modified;
		}

		bool IntroducePostIncrementForVariables(List<ILNode> body, ILExpression expr, int pos)
		{
			// Works for variables and static fields/properties

			// expr = ldloc(i)
			// stloc(i, add(expr, ldc.i4(1)))
			// ->
			// expr = postincrement(1, ldloca(i))
			ILVariable exprVar;
			ILExpression exprInit;
			if (!(expr.Match(ILCode.Stloc, out exprVar, out exprInit) && exprVar.GeneratedByDecompiler))
				return false;

			//The next expression
			ILExpression nextExpr = body.ElementAtOrDefault(pos + 1) as ILExpression;
			if (nextExpr == null)
				return false;

			ILCode loadInstruction = exprInit.Code;
			ILCode storeInstruction = nextExpr.Code;
			bool recombineVariable = false;

			// We only recognise local variables, static fields, and static getters with no arguments
			switch (loadInstruction) {
				case ILCode.Ldloc:
					//Must be a matching store type
					if (storeInstruction != ILCode.Stloc)
						return false;
					ILVariable loadVar = (ILVariable)exprInit.Operand;
					ILVariable storeVar = (ILVariable)nextExpr.Operand;
					if (loadVar != storeVar) {
						if (loadVar.OriginalVariable != null && loadVar.OriginalVariable == storeVar.OriginalVariable)
							recombineVariable = true;
						else
							return false;
					}
					break;
				case ILCode.Ldsfld:
					if (storeInstruction != ILCode.Stsfld)
						return false;
					if (exprInit.Operand != nextExpr.Operand)
						return false;
					break;
				case ILCode.CallGetter:
					// non-static getters would have the 'this' argument
					if (exprInit.Arguments.Count != 0)
						return false;
					if (storeInstruction != ILCode.CallSetter && storeInstruction != ILCode.CallReadOnlySetter)
						return false;
					if (!IsGetterSetterPair(exprInit.Operand, nextExpr.Operand))
						return false;
					break;
				default:
					return false;
			}

			ILExpression addExpr = nextExpr.Arguments[0];

			int incrementAmount;
			ILCode incrementCode = GetIncrementCode(addExpr, out incrementAmount);
			if (!(incrementAmount != 0 && addExpr.Arguments[0].MatchLdloc(exprVar)))
				return false;

			if (recombineVariable) {
				// Split local variable, unsplit these two instances
				// replace nextExpr.Operand with exprInit.Operand
				ReplaceVariables(method, oldVar => oldVar == nextExpr.Operand ? (ILVariable)exprInit.Operand : oldVar);
			}

			switch (loadInstruction) {
				case ILCode.Ldloc:
					exprInit.Code = ILCode.Ldloca;
					break;
				case ILCode.Ldsfld:
					exprInit.Code = ILCode.Ldsflda;
					break;
				case ILCode.CallGetter:
					exprInit = new ILExpression(ILCode.AddressOf, null, exprInit);
					break;
			}
			expr.Arguments[0] = new ILExpression(incrementCode, incrementAmount, exprInit);
			if (context.CalculateILSpans)
				nextExpr.AddSelfAndChildrenRecursiveILSpans(expr.ILSpans);
			body.RemoveAt(pos + 1);
			return true;
		}

		static bool IsGetterSetterPair(object getterOperand, object setterOperand)
		{
			IMethod getter = getterOperand as IMethod;
			IMethod setter = setterOperand as IMethod;
			if (getter == null || setter == null || !getter.IsMethod || !setter.IsMethod)
				return false;
			if (!TypeAnalysis.IsSameType(getter.DeclaringType, setter.DeclaringType))
				return false;
			MethodDef getterDef = getter.Resolve();
			MethodDef setterDef = setter.Resolve();
			if (getterDef == null || setterDef == null)
				return false;
			for (int i = 0; i < getterDef.DeclaringType.Properties.Count; i++) {
				var prop = getterDef.DeclaringType.Properties[i];
				if (prop.GetMethod == getterDef)
					return prop.SetMethod == setterDef;
			}
			return false;
		}

		ILExpression IntroducePostIncrementForInstanceFields(ILExpression expr)
		{
			// stfld(field, ldloc(instance), add(stloc(helperVar, ldfld(field, ldloc(instance))), ldc.i4(1)))
			// -> stloc(helperVar, postincrement(1, ldflda(field, ldloc(instance))))

			// Also works for array elements and pointers:

			// stelem.any(T, ldloc(instance), ldloc(pos), add(stloc(helperVar, ldelem.any(T, ldloc(instance), ldloc(pos))), ldc.i4(1)))
			// -> stloc(helperVar, postincrement(1, ldelema(ldloc(instance), ldloc(pos))))

			// stobj(T, ldloc(ptr), add(stloc(helperVar, ldobj(T, ldloc(ptr)), ldc.i4(1))))
			// -> stloc(helperVar, postIncrement(1, ldloc(ptr)))

			// callsetter(set_P, ldloc(instance), add(stloc(helperVar, callgetter(get_P, ldloc(instance))), ldc.i4(1)))
			// -> stloc(helperVar, postIncrement(1, propertyaddress. callgetter(get_P, ldloc(instance))))

			if (!(expr.Code == ILCode.Stfld || expr.Code.IsStoreToArray() || expr.Code == ILCode.Stobj || expr.Code == ILCode.CallSetter || expr.Code == ILCode.CallvirtSetter))
				return null;

			// Test that all arguments except the last are ldloc (1 arg for fields and pointers, 2 args for arrays)
			for (int i = 0; i < expr.Arguments.Count - 1; i++) {
				if (expr.Arguments[i].Code != ILCode.Ldloc)
					return null;
			}

			ILExpression addExpr = expr.Arguments[expr.Arguments.Count - 1];
			int incrementAmount;
			ILCode incrementCode = GetIncrementCode(addExpr, out incrementAmount);
			ILVariable helperVar;
			ILExpression initialValue;
			if (!(incrementAmount != 0 && addExpr.Arguments[0].Match(ILCode.Stloc, out helperVar, out initialValue)))
				return null;

			if (expr.Code == ILCode.Stfld) {
				if (initialValue.Code != ILCode.Ldfld)
					return null;
				// There might be two different FieldReference instances, so we compare the field's signatures:
				IField getField = (IField)initialValue.Operand;
				IField setField = (IField)expr.Operand;
				if (!(TypeAnalysis.IsSameType(getField.DeclaringType, setField.DeclaringType)
				      && getField.Name == setField.Name &&
					  getField.FieldSig != null && setField.FieldSig != null &&
					  TypeAnalysis.IsSameType(getField.FieldSig.Type, setField.FieldSig.Type)))
				{
					return null;
				}
			} else if (expr.Code == ILCode.Stobj) {
				if (!(initialValue.Code == ILCode.Ldobj && initialValue.Operand == expr.Operand))
					return null;
			} else if (expr.Code == ILCode.CallSetter) {
				if (!(initialValue.Code == ILCode.CallGetter && IsGetterSetterPair(initialValue.Operand, expr.Operand)))
					return null;
			} else if (expr.Code == ILCode.CallvirtSetter) {
				if (!(initialValue.Code == ILCode.CallvirtGetter && IsGetterSetterPair(initialValue.Operand, expr.Operand)))
					return null;
			} else {
				if (!initialValue.Code.IsLoadFromArray())
					return null;
			}
			Debug.Assert(expr.Arguments.Count - 1 == initialValue.Arguments.Count);
			for (int i = 0; i < initialValue.Arguments.Count; i++) {
				if (!initialValue.Arguments[i].MatchLdloc((ILVariable)expr.Arguments[i].Operand))
					return null;
			}

			ILExpression stloc = addExpr.Arguments[0];
			if (context.CalculateILSpans) {
				stloc.Arguments[0].AddSelfAndChildrenRecursiveILSpans(stloc.ILSpans);
				stloc.ILSpans.AddRange(expr.ILSpans);     // no recursive add
				stloc.ILSpans.AddRange(addExpr.ILSpans);  // no recursive add
				for (int i = 0; i < expr.Arguments.Count - 1; i++)
					expr.Arguments[i].AddSelfAndChildrenRecursiveILSpans(stloc.ILSpans);
				for (int i = 1; i < addExpr.Arguments.Count; i++)
					addExpr.Arguments[i].AddSelfAndChildrenRecursiveILSpans(stloc.ILSpans);
			}
			if (expr.Code == ILCode.Stobj) {
				stloc.Arguments[0] = new ILExpression(incrementCode, incrementAmount, initialValue.Arguments[0]);
			} else if (expr.Code == ILCode.CallSetter || expr.Code == ILCode.CallvirtSetter) {
				initialValue = new ILExpression(ILCode.AddressOf, null, initialValue);
				stloc.Arguments[0] = new ILExpression(incrementCode, incrementAmount, initialValue);
			} else {
				stloc.Arguments[0] = new ILExpression(incrementCode, incrementAmount, initialValue);
				initialValue.Code = (expr.Code == ILCode.Stfld ? ILCode.Ldflda : ILCode.Ldelema);
			}

			return stloc;
		}

		ILCode GetIncrementCode(ILExpression addExpr, out int incrementAmount)
		{
			ILCode incrementCode;
			bool decrement = false;
			switch (addExpr.Code) {
				case ILCode.Add:
					incrementCode = ILCode.PostIncrement;
					break;
				case ILCode.Add_Ovf:
					incrementCode = ILCode.PostIncrement_Ovf;
					break;
				case ILCode.Add_Ovf_Un:
					incrementCode = ILCode.PostIncrement_Ovf_Un;
					break;
				case ILCode.Sub:
					incrementCode = ILCode.PostIncrement;
					decrement = true;
					break;
				case ILCode.Sub_Ovf:
					incrementCode = ILCode.PostIncrement_Ovf;
					decrement = true;
					break;
				case ILCode.Sub_Ovf_Un:
					incrementCode = ILCode.PostIncrement_Ovf_Un;
					decrement = true;
					break;
				default:
					incrementAmount = 0;
					return ILCode.Nop;
			}
			if (addExpr.Arguments[1].Match(ILCode.Ldc_I4, out incrementAmount)) {
				if (incrementAmount == -1 || incrementAmount == 1) { // TODO pointer increment?
					if (decrement)
						incrementAmount = -incrementAmount;
					return incrementCode;
				}
			}
			incrementAmount = 0;
			return ILCode.Nop;
		}
		#endregion

		#region IntroduceFixedStatements
		bool IntroduceFixedStatements(ILBlockBase block, List<ILNode> body, int i)
		{
			ILExpression initValue;
			ILVariable pinnedVar;
			int initEndPos;
			if (!MatchFixedInitializer(body, i, out pinnedVar, out initValue, out initEndPos))
				return false;

			ILFixedStatement fixedStmt = body.ElementAtOrDefault(initEndPos) as ILFixedStatement;
			if (fixedStmt != null) {
				ILExpression expr = fixedStmt.BodyBlock.Body.LastOrDefault() as ILExpression;
				if (expr != null && expr.Code == ILCode.Stloc && expr.Operand == pinnedVar && IsNullOrZero(expr.Arguments[0])) {
					// we found a second initializer for the existing fixed statement
					fixedStmt.Initializers.Insert(0, initValue);
					if (context.CalculateILSpans) {
						for (int k = i; k < initEndPos; k++)
							initValue.ILSpans.AddRange(body[k].GetSelfAndChildrenRecursiveILSpans().ToArray());
					}
					body.RemoveRange(i, initEndPos - i);
					if (context.CalculateILSpans)
						Utils.AddILSpans(fixedStmt.BodyBlock, fixedStmt.BodyBlock.Body, fixedStmt.BodyBlock.Body.Count - 1);
					fixedStmt.BodyBlock.Body.RemoveAt(fixedStmt.BodyBlock.Body.Count - 1);
					if (pinnedVar.Type is ByRefSig)
						pinnedVar.Type = new PtrSig(((ByRefSig)pinnedVar.Type).Next);
					return true;
				}
			}

			// find where pinnedVar is reset to 0:
			int j;
			for (j = initEndPos; j < body.Count; j++) {
				ILVariable v2;
				ILExpression storedVal;
				// stloc(pinned_Var, conv.u(ldc.i4(0)))
				if (body[j].Match(ILCode.Stloc, out v2, out storedVal) && v2 == pinnedVar) {
					if (IsNullOrZero(storedVal)) {
						break;
					}
				}
			}
			// Create fixed statement from i to j
			fixedStmt = new ILFixedStatement();
			fixedStmt.Initializers.Add(initValue);
			fixedStmt.BodyBlock = new ILBlock(body.GetRange(initEndPos, j - initEndPos), CodeBracesRangeFlags.FixedBraces); // from initEndPos to j-1 (inclusive)
			if (context.CalculateILSpans) {
				for (int k = i; k < initEndPos; k++)
					initValue.ILSpans.AddRange(body[k].GetSelfAndChildrenRecursiveILSpans().ToArray());
			}
			body.RemoveRange(i + 1, Math.Min(j, body.Count - 1) - i); // from i+1 to j (inclusive)
			body[i] = fixedStmt;
			if (pinnedVar.Type is ByRefSig)
				pinnedVar.Type = new PtrSig(((ByRefSig)pinnedVar.Type).Next);

			return true;
		}

		bool IsNullOrZero(ILExpression expr)
		{
			if (expr.Code == ILCode.Conv_U || expr.Code == ILCode.Conv_I)
				expr = expr.Arguments[0];
			return (expr.Code == ILCode.Ldc_I4 && (int)expr.Operand == 0) || expr.Code == ILCode.Ldnull;
		}

		bool MatchFixedInitializer(List<ILNode> body, int i, out ILVariable pinnedVar, out ILExpression initValue, out int nextPos)
		{
			if (body[i].Match(ILCode.Stloc, out pinnedVar, out initValue) && pinnedVar.IsPinned && !IsNullOrZero(initValue)) {
				initValue = (ILExpression)body[i];
				nextPos = i + 1;
				HandleStringFixing(pinnedVar, body, ref nextPos, ref initValue);
				return true;
			}
			ILCondition ifStmt = body[i] as ILCondition;
			ILExpression arrayLoadingExpr;
			if (ifStmt != null && MatchFixedArrayInitializerCondition(ifStmt.Condition, out arrayLoadingExpr)) {
				ILVariable arrayVariable = (ILVariable)arrayLoadingExpr.Operand;
				ILExpression trueValue;
				if (ifStmt.TrueBlock != null && ifStmt.TrueBlock.Body.Count == 1
				    && ifStmt.TrueBlock.Body[0].Match(ILCode.Stloc, out pinnedVar, out trueValue)
				    && pinnedVar.IsPinned && IsNullOrZero(trueValue))
				{
					if (ifStmt.FalseBlock != null && ifStmt.FalseBlock.Body.Count == 1 && ifStmt.FalseBlock.Body[0] is ILFixedStatement) {
						ILFixedStatement fixedStmt = (ILFixedStatement)ifStmt.FalseBlock.Body[0];
						ILVariable stlocVar;
						ILExpression falseValue;
						if (fixedStmt.Initializers.Count == 1 && fixedStmt.BodyBlock.Body.Count == 0
						    && fixedStmt.Initializers[0].Match(ILCode.Stloc, out stlocVar, out falseValue) && stlocVar == pinnedVar)
						{
							ILVariable loadedVariable;
							if (falseValue.Code == ILCode.Ldelema
							    && falseValue.Arguments[0].Match(ILCode.Ldloc, out loadedVariable) && loadedVariable == arrayVariable
							    && IsNullOrZero(falseValue.Arguments[1]))
							{
								// OK, we detected the pattern for fixing an array.
								// Now check whether the loading expression was a store ot a temp. var
								// that can be eliminated.
								if (arrayLoadingExpr.Code == ILCode.Stloc) {
									ILInlining inlining = GetILInlining(method);
									if (inlining.numLdloc.GetOrDefault(arrayVariable) == 2 &&
									    inlining.numStloc.GetOrDefault(arrayVariable) == 1 && inlining.numLdloca.GetOrDefault(arrayVariable) == 0)
									{
										arrayLoadingExpr = arrayLoadingExpr.Arguments[0];
									}
								}
								initValue = new ILExpression(ILCode.Stloc, pinnedVar, arrayLoadingExpr);
								nextPos = i + 1;
								return true;
							}
						}
					}
				}
			}
			initValue = null;
			nextPos = -1;
			return false;
		}

		bool MatchFixedArrayInitializerCondition(ILExpression condition, out ILExpression initValue)
		{
			ILExpression logicAnd;
			ILVariable arrayVar;
			if (condition.Match(ILCode.LogicNot, out logicAnd) && logicAnd.Code == ILCode.LogicAnd) {
				initValue = UnpackDoubleNegation(logicAnd.Arguments[0]);
				ILExpression arrayVarInitializer;
				if (initValue.Match(ILCode.Ldloc, out arrayVar)
				    || initValue.Match(ILCode.Stloc, out arrayVar, out arrayVarInitializer))
				{
					ILExpression arrayLength = logicAnd.Arguments[1];
					if (arrayLength.Code == ILCode.Conv_I4)
						arrayLength = arrayLength.Arguments[0];
					return arrayLength.Code == ILCode.Ldlen && arrayLength.Arguments[0].MatchLdloc(arrayVar);
				}
			}
			initValue = null;
			return false;
		}

		ILExpression UnpackDoubleNegation(ILExpression expr)
		{
			ILExpression negated;
			if (expr.Match(ILCode.LogicNot, out negated) && negated.Match(ILCode.LogicNot, out negated))
				return negated;
			else
				return expr;
		}

		bool HandleStringFixing(ILVariable pinnedVar, List<ILNode> body, ref int pos, ref ILExpression fixedStmtInitializer)
		{
			// fixed (stloc(pinnedVar, ldloc(text))) {
			//   var1 = var2 = conv.i(ldloc(pinnedVar))
			//   if (logicnot(logicnot(var1))) {
			//     var2 = add(var1, call(RuntimeHelpers::get_OffsetToStringData))
			//   }
			//   stloc(ptrVar, var2)
			//   ...

			if (pos >= body.Count)
				return false;

			ILVariable var1, var2;
			ILExpression varAssignment, ptrInitialization;
			if (!(body[pos].Match(ILCode.Stloc, out var1, out varAssignment) && varAssignment.Match(ILCode.Stloc, out var2, out ptrInitialization)))
				return false;
			if (!(var1.GeneratedByDecompiler && var2.GeneratedByDecompiler))
				return false;
			if (ptrInitialization.Code == ILCode.Conv_I || ptrInitialization.Code == ILCode.Conv_U)
				ptrInitialization = ptrInitialization.Arguments[0];
			if (!ptrInitialization.MatchLdloc(pinnedVar))
				return false;

			ILCondition ifStmt = body[pos + 1] as ILCondition;
			if (!(ifStmt != null && ifStmt.TrueBlock != null && ifStmt.TrueBlock.Body.Count == 1 && (ifStmt.FalseBlock == null || ifStmt.FalseBlock.Body.Count == 0)))
				return false;
			if (!UnpackDoubleNegation(ifStmt.Condition).MatchLdloc(var1))
				return false;
			ILVariable assignedVar;
			ILExpression assignedExpr;
			if (!(ifStmt.TrueBlock.Body[0].Match(ILCode.Stloc, out assignedVar, out assignedExpr) && assignedVar == var2 && assignedExpr.Code == ILCode.Add))
				return false;
			IMethod calledMethod;
			if (!(assignedExpr.Arguments[0].MatchLdloc(var1)))
				return false;
			if (!(assignedExpr.Arguments[1].Match(ILCode.Call, out calledMethod) || assignedExpr.Arguments[1].Match(ILCode.CallGetter, out calledMethod)))
				return false;
			if (!(calledMethod.Name == "get_OffsetToStringData" && calledMethod.DeclaringType != null && calledMethod.DeclaringType.FullName == "System.Runtime.CompilerServices.RuntimeHelpers"))
				return false;

			ILVariable pointerVar;
			if (body[pos + 2].Match(ILCode.Stloc, out pointerVar, out assignedExpr) && assignedExpr.MatchLdloc(var2)) {
				pos += 3;
				fixedStmtInitializer.Operand = pointerVar;
				return true;
			}
			return false;
		}
		#endregion

		#region SimplifyLogicNot
		bool SimplifyLogicNot(ILBlockBase block, List<ILNode> body, ILExpression expr, int pos)
		{
			bool modified = false;
			expr = SimplifyLogicNot(expr, ref modified);
			Debug.Assert(expr == null);
			return modified;
		}

		ILExpression SimplifyLogicNot(ILExpression expr, ref bool modified)
		{
			ILExpression a;
			// "ceq(a, ldc.i4.0)" becomes "logicnot(a)" if the inferred type for expression "a" is boolean
			if (expr.Code == ILCode.Ceq && expr.Arguments[0].InferredType.GetElementType() == ElementType.Boolean && (a = expr.Arguments[1]).Code == ILCode.Ldc_I4 && (int)a.Operand == 0) {
				expr.Code = ILCode.LogicNot;
				if (context.CalculateILSpans)
					a.AddSelfAndChildrenRecursiveILSpans(expr.ILSpans);
				expr.Arguments.RemoveAt(1);
				modified = true;
			}

			ILExpression res = null;
			while (expr.Code == ILCode.LogicNot) {
				a = expr.Arguments[0];
				// remove double negation
				if (a.Code == ILCode.LogicNot) {
					res = a.Arguments[0];
					if (context.CalculateILSpans) {
						res.ILSpans.AddRange(expr.ILSpans);
						res.ILSpans.AddRange(a.ILSpans);
					}
					expr = res;
				} else {
					if (SimplifyLogicNotArgument(expr)) res = expr = a;
					break;
				}
			}

			for (int i = 0; i < expr.Arguments.Count; i++) {
				a = SimplifyLogicNot(expr.Arguments[i], ref modified);
				if (a != null) {
					expr.Arguments[i] = a;
					modified = true;
				}
			}

			return res;
		}

		/// <summary>
		/// If the argument is a binary comparison operation then the negation is pushed through it
		/// </summary>
		bool SimplifyLogicNotArgument(ILExpression expr)
		{
			var a = expr.Arguments[0];
			ILCode c;
			switch (a.Code) {
					case ILCode.Cnull: c = ILCode.Cnotnull; break;
					case ILCode.Cnotnull: c = ILCode.Cnull; break;
					case ILCode.Ceq: c = ILCode.Cne; break;
					case ILCode.Cne: c = ILCode.Ceq; break;
					case ILCode.Cgt: c = ILCode.Cle; break;
					case ILCode.Cgt_Un: c = ILCode.Cle_Un; break;
					case ILCode.Cge: c = ILCode.Clt; break;
					case ILCode.Cge_Un: c = ILCode.Clt_Un; break;
					case ILCode.Clt: c = ILCode.Cge; break;
					case ILCode.Clt_Un: c = ILCode.Cge_Un; break;
					case ILCode.Cle: c = ILCode.Cgt; break;
					case ILCode.Cle_Un: c = ILCode.Cgt_Un; break;
					default: return false;
			}
			a.Code = c;
			if (context.CalculateILSpans)
				a.ILSpans.AddRange(expr.ILSpans);
			return true;
		}
		#endregion

		#region SimplifyShiftOperators
		bool SimplifyShiftOperators(ILBlockBase block, List<ILNode> body, ILExpression expr, int pos)
		{
			// C# compiles "a << b" to "a << (b & 31)", so we will remove the "& 31" if possible.
			bool modified = false;
			SimplifyShiftOperators(expr, ref modified);
			return modified;
		}

		void SimplifyShiftOperators(ILExpression expr, ref bool modified)
		{
			for (int i = 0; i < expr.Arguments.Count; i++)
				SimplifyShiftOperators(expr.Arguments[i], ref modified);
			if (expr.Code != ILCode.Shl && expr.Code != ILCode.Shr && expr.Code != ILCode.Shr_Un)
				return;
			var a = expr.Arguments[1];
			if (a.Code != ILCode.And || a.Arguments[1].Code != ILCode.Ldc_I4 || expr.InferredType == null)
				return;
			int mask;
			switch (expr.InferredType.ElementType) {
				case ElementType.I4:
					case ElementType.U4: mask = 31; break;
				case ElementType.I8:
					case ElementType.U8: mask = 63; break;
					default: return;
			}
			if ((int)a.Arguments[1].Operand != mask) return;
			var res = a.Arguments[0];
			if (context.CalculateILSpans) {
				res.ILSpans.AddRange(a.ILSpans);
				res.ILSpans.AddRange(a.Arguments[1].ILSpans);
			}
			expr.Arguments[1] = res;
			modified = true;
		}
		#endregion

		#region InlineExpressionTreeParameterDeclarations
		bool InlineExpressionTreeParameterDeclarations(ILBlockBase block, List<ILNode> body, ILExpression expr, int pos)
		{
			// When there is a Expression.Lambda() call, and the parameters are declared in the
			// IL statement immediately prior to the one containing the Lambda() call,
			// using this code for the3 declaration:
			//   stloc(v, call(Expression::Parameter, call(Type::GetTypeFromHandle, ldtoken(...)), ldstr(...)))
			// and the variables v are assigned only once (in that statements), and read only in a Expression::Lambda
			// call that immediately follows the assignment statements, then we will inline those assignments
			// into the Lambda call using ILCode.ExpressionTreeParameterDeclarations.

			// This is sufficient to allow inlining over the expression tree construction. The remaining translation
			// of expression trees into C# will be performed by a C# AST transformer.

			for (int i = expr.Arguments.Count - 1; i >= 0; i--) {
				if (InlineExpressionTreeParameterDeclarations(block, body, expr.Arguments[i], pos))
					return true;
			}

			IMethod mr;
			ILExpression lambdaBodyExpr, parameterArray;
			if (!(expr.Match(ILCode.Call, out mr, out lambdaBodyExpr, out parameterArray) && mr.Name == "Lambda"))
				return false;
			if (!(parameterArray.Code == ILCode.InitArray && mr.DeclaringType != null && mr.DeclaringType.FullName == "System.Linq.Expressions.Expression"))
				return false;
			int firstParameterPos = pos - parameterArray.Arguments.Count;
			if (firstParameterPos < 0)
				return false;

			ILExpression[] parameterInitExpressions = new ILExpression[parameterArray.Arguments.Count + 1];
			for (int i = 0; i < parameterArray.Arguments.Count; i++) {
				parameterInitExpressions[i] = body[firstParameterPos + i] as ILExpression;
				if (!MatchParameterVariableAssignment(parameterInitExpressions[i]))
					return false;
				ILVariable v = (ILVariable)parameterInitExpressions[i].Operand;
				if (!parameterArray.Arguments[i].MatchLdloc(v))
					return false;
				// TODO: validate that the variable is only used here and within 'body'
			}

			parameterInitExpressions[parameterInitExpressions.Length - 1] = lambdaBodyExpr;
			Debug.Assert(expr.Arguments[0] == lambdaBodyExpr);
			expr.Arguments[0] = new ILExpression(ILCode.ExpressionTreeParameterDeclarations, null, parameterInitExpressions);

			body.RemoveRange(firstParameterPos, parameterArray.Arguments.Count);

			return true;
		}

		bool MatchParameterVariableAssignment(ILExpression expr)
		{
			if (expr == null)
				return false;
			// stloc(v, call(Expression::Parameter, call(Type::GetTypeFromHandle, ldtoken(...)), ldstr(...)))
			ILVariable v;
			ILExpression init;
			if (!expr.Match(ILCode.Stloc, out v, out init))
				return false;
			if (v.GeneratedByDecompiler || v.IsParameter || v.IsPinned)
				return false;
			if (v.Type == null || v.Type.FullName != "System.Linq.Expressions.ParameterExpression")
				return false;
			IMethod parameterMethod;
			ILExpression typeArg, nameArg;
			if (!init.Match(ILCode.Call, out parameterMethod, out typeArg, out nameArg))
				return false;
			if (!(parameterMethod.Name == "Parameter" && parameterMethod.DeclaringType != null && parameterMethod.DeclaringType.FullName == "System.Linq.Expressions.Expression"))
				return false;
			IMethod getTypeFromHandle;
			ILExpression typeToken;
			if (!typeArg.Match(ILCode.Call, out getTypeFromHandle, out typeToken))
				return false;
			if (!(getTypeFromHandle.Name == "GetTypeFromHandle" && getTypeFromHandle.DeclaringType != null && getTypeFromHandle.DeclaringType.FullName == "System.Type"))
				return false;
			return typeToken.Code == ILCode.Ldtoken && nameArg.Code == ILCode.Ldstr;
		}
		#endregion
	}
}
