﻿// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using dnlib.DotNet;

namespace ICSharpCode.Decompiler.ILAst {
	/// <summary>
	/// This exception is thrown when we find something else than we expect from the C# compiler.
	/// This aborts the analysis and makes the whole transform fail.
	/// </summary>
	class SymbolicAnalysisFailedException : Exception {}
	
	enum SymbolicValueType
	{
		/// <summary>
		/// Unknown value
		/// </summary>
		Unknown,
		/// <summary>
		/// int: Constant (result of ldc.i4)
		/// </summary>
		IntegerConstant,
		/// <summary>
		/// int: State + Constant
		/// </summary>
		State,
		/// <summary>
		/// This pointer (result of ldarg.0)
		/// </summary>
		This,
		/// <summary>
		/// bool: State == Constant
		/// </summary>
		StateEquals,
		/// <summary>
		/// bool: State != Constant
		/// </summary>
		StateInEquals,
		/// <summary>
		/// Converted from signed greater than comparisons
		/// </summary>
		StateIsInRange,
		/// <summary>
		/// Converted from unsigned greater than comparisons
		/// </summary>
		StateIsNotInRange,
	}

	struct SymbolicValue
	{
		public readonly int Constant;
		public readonly int Constant2;
		public readonly SymbolicValueType Type;
		
		public SymbolicValue(SymbolicValueType type, int constant = 0)
		{
			this.Type = type;
			this.Constant = constant;
			this.Constant2 = 0;
		}

		public SymbolicValue(SymbolicValueType type, int constant1, int constant2)
		{
			this.Type = type;
			this.Constant = constant1;
			this.Constant2 = constant2;
		}

		public SymbolicValue AsBool()
		{
			if (Type == SymbolicValueType.State) {
				// convert state integer to bool:
				// if (state + c) = if (state + c != 0) = if (state != -c)
				return new SymbolicValue(SymbolicValueType.StateInEquals, unchecked(-Constant));
			}
			return this;
		}
		public override string ToString()
		{
			if (Type == SymbolicValueType.StateIsInRange)
				return $"[SymbolicValue {Type}: [{Constant}..{Constant2}]]";
			if (Type == SymbolicValueType.StateIsNotInRange)
				return $"[SymbolicValue {Type}: ![{Constant}..{Constant2}]]";
			return $"[SymbolicValue {Type}: {Constant}]";
		}
	}
	
	class SymbolicEvaluationContext
	{
		readonly FieldDef stateField;
		readonly List<ILVariable> stateVariables = new List<ILVariable>();

		public List<ILVariable> StateVariables => stateVariables;
		
		public SymbolicEvaluationContext(FieldDef stateField)
		{
			this.stateField = stateField;
		}
		
		public void AddStateVariable(ILVariable v)
		{
			if (!stateVariables.Contains(v))
				stateVariables.Add(v);
		}
		
		SymbolicValue Failed()
		{
			return new SymbolicValue(SymbolicValueType.Unknown);
		}
		
		public SymbolicValue Eval(ILExpression expr)
		{
			SymbolicValue left, right;
			int lo, hi;
			switch (expr.Code) {
				case ILCode.Sub:
				case ILCode.Sub_Ovf:
					left = Eval(expr.Arguments[0]);
					right = Eval(expr.Arguments[1]);
					if (left.Type != SymbolicValueType.State && left.Type != SymbolicValueType.IntegerConstant)
						return Failed();
					if (right.Type != SymbolicValueType.IntegerConstant)
						return Failed();
					return new SymbolicValue(left.Type, unchecked ( left.Constant - right.Constant ));
				case ILCode.Ldfld:
					if (Eval(expr.Arguments[0]).Type != SymbolicValueType.This)
						return Failed();
					if ((expr.Operand as IField).ResolveFieldWithinSameModule() != stateField)
						return Failed();
					return new SymbolicValue(SymbolicValueType.State);
				case ILCode.Ldloc:
					ILVariable loadedVariable = (ILVariable)expr.Operand;
					if (stateVariables.Contains(loadedVariable))
						return new SymbolicValue(SymbolicValueType.State);
					else if (loadedVariable.IsParameter && loadedVariable.OriginalParameter.IsHiddenThisParameter)
						return new SymbolicValue(SymbolicValueType.This);
					else
						return Failed();
				case ILCode.Ldc_I4:
					return new SymbolicValue(SymbolicValueType.IntegerConstant, (int)expr.Operand);
				case ILCode.Ceq:
				case ILCode.Cne:
					left = Eval(expr.Arguments[0]);
					right = Eval(expr.Arguments[1]);
					if (left.Type != SymbolicValueType.State || right.Type != SymbolicValueType.IntegerConstant)
						return Failed();
					// bool: (state + left.Constant == right.Constant)
					// bool: (state == right.Constant - left.Constant)
					return new SymbolicValue(expr.Code == ILCode.Ceq ? SymbolicValueType.StateEquals : SymbolicValueType.StateInEquals, unchecked(right.Constant - left.Constant));
				case ILCode.LogicNot:
					SymbolicValue val = Eval(expr.Arguments[0]).AsBool();
					if (val.Type == SymbolicValueType.StateEquals)
						return new SymbolicValue(SymbolicValueType.StateInEquals, val.Constant);
					else if (val.Type == SymbolicValueType.StateInEquals)
						return new SymbolicValue(SymbolicValueType.StateEquals, val.Constant);
					else
						return Failed();
				case ILCode.Cgt:
					left = Eval(expr.Arguments[0]);
					right = Eval(expr.Arguments[1]);
					if (left.Type != SymbolicValueType.State || right.Type != SymbolicValueType.IntegerConstant)
						return Failed();
					return new SymbolicValue(SymbolicValueType.StateIsInRange, right.Constant - left.Constant + 1, int.MaxValue);
				case ILCode.Cgt_Un:
					left = Eval(expr.Arguments[0]);
					right = Eval(expr.Arguments[1]);
					if (left.Type != SymbolicValueType.State || right.Type != SymbolicValueType.IntegerConstant)
						return Failed();
					lo = -left.Constant;
					hi = lo + right.Constant;
					if (lo > hi)
						return Failed();
					return new SymbolicValue(SymbolicValueType.StateIsNotInRange, lo, hi);
				case ILCode.Cle_Un:
					left = Eval(expr.Arguments[0]);
					right = Eval(expr.Arguments[1]);
					if (left.Type != SymbolicValueType.State || right.Type != SymbolicValueType.IntegerConstant)
						return Failed();
					lo = -left.Constant;
					hi = lo + right.Constant;
					if (lo > hi)
						return Failed();
					return new SymbolicValue(SymbolicValueType.StateIsInRange, lo, hi);
				default:
					return Failed();
			}
		}
	}
}
