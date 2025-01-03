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

namespace ICSharpCode.Decompiler.ILAst
{
	/// <summary>
	/// IL AST transformation that introduces array, object and collection initializers.
	/// </summary>
	partial class ILAstOptimizer
	{
		#region Array Initializers
		bool TransformArrayInitializers(ILBlockBase block, List<ILNode> body, ILExpression expr, int pos)
		{
			ILVariable v, v3;
			ILExpression newarrExpr;
			ITypeDefOrRef elementType;
			ILExpression lengthExpr;
			int arrayLength;
			if (expr.Match(ILCode.Stloc, out v, out newarrExpr) &&
			    newarrExpr.Match(ILCode.Newarr, out elementType, out lengthExpr) &&
			    lengthExpr.Match(ILCode.Ldc_I4, out arrayLength) &&
			    arrayLength > 0) {
				ILExpression[] newArr;
				int initArrayPos;
				if (ForwardScanInitializeArrayRuntimeHelper(body, pos + 1, v, new SZArraySig(elementType.ToTypeSig()), arrayLength, out newArr, out initArrayPos)) {
					var arrayType = new ArraySig(elementType.ToTypeSig(), 1, new uint[1], new int[1]);
					arrayLength = newArr.Length;
					arrayType.Sizes[0] = (uint)(arrayLength + 1);
					var newStloc = new ILExpression(ILCode.Stloc, v, new ILExpression(ILCode.InitArray, arrayType.ToTypeDefOrRef(), newArr));
					if (context.CalculateILSpans) {
						body[pos].AddSelfAndChildrenRecursiveILSpans(newStloc.ILSpans);
						body[initArrayPos].AddSelfAndChildrenRecursiveILSpans(newStloc.ILSpans);
					}
					body[pos] = newStloc;
					body.RemoveAt(initArrayPos);
				}
				// Put in a limit so that we don't consume too much memory if the code allocates a huge array
				// and populates it extremely sparsly. However, 255 "null" elements in a row actually occur in the Mono C# compiler!
				const int maxConsecutiveDefaultValueExpressions = 300;
				List<ILExpression> operands = new List<ILExpression>();
				int numberOfInstructionsToRemove = 0;
				for (int j = pos + 1; j < body.Count; j++) {
					ILExpression nextExpr = body[j] as ILExpression;
					int arrayPos;
					if (nextExpr != null &&
					    nextExpr.Code.IsStoreToArray() &&
					    nextExpr.Arguments[0].Match(ILCode.Ldloc, out v3) &&
					    v == v3 &&
					    nextExpr.Arguments[1].Match(ILCode.Ldc_I4, out arrayPos) &&
					    arrayPos >= operands.Count &&
					    arrayPos <= operands.Count + maxConsecutiveDefaultValueExpressions &&
					    !nextExpr.Arguments[2].ContainsReferenceTo(v3))
					{
						while (operands.Count < arrayPos)
							operands.Add(new ILExpression(ILCode.DefaultValue, elementType));
						operands.Add(nextExpr.Arguments[2]);
						numberOfInstructionsToRemove++;
					} else {
						break;
					}
				}
				if (operands.Count == arrayLength) {
					var arrayType = new ArraySig(elementType.ToTypeSig(), 1, new uint[1], new int[1]);
					arrayType.Sizes[0] = (uint)(arrayLength + 1);
					expr.Arguments[0] = new ILExpression(ILCode.InitArray, arrayType.ToTypeDefOrRef(), operands);
					if (context.CalculateILSpans) {
						newarrExpr.AddSelfAndChildrenRecursiveILSpans(expr.ILSpans);
						for (int i = 0; i < numberOfInstructionsToRemove; i++)
							body[pos + 1 + i].AddSelfAndChildrenRecursiveILSpans(expr.ILSpans);
					}
					body.RemoveRange(pos + 1, numberOfInstructionsToRemove);

					GetILInlining(method).InlineIfPossible(block, body, ref pos);
					return true;
				}
			}
			return false;
		}

		bool TransformMultidimensionalArrayInitializers(ILBlockBase block, List<ILNode> body, ILExpression expr, int pos)
		{
			ILVariable v;
			ILExpression newarrExpr;
			IMethod ctor;
			List<ILExpression> ctorArgs;
			TypeSpec arySpec;
			ArraySig arrayType;
			if (expr.Match(ILCode.Stloc, out v, out newarrExpr) &&
			    newarrExpr.Match(ILCode.Newobj, out ctor, out ctorArgs) &&
			    (arySpec = (ctor.DeclaringType as TypeSpec)) != null &&
				(arrayType = arySpec.TypeSig.RemovePinnedAndModifiers() as ArraySig) != null &&
			    arrayType.Rank == ctorArgs.Count) {
				// Clone the type, so we can muck about with the Dimensions
				var multAry = new ArraySig(arrayType.Next, arrayType.Rank, new uint[arrayType.Rank], new int[arrayType.Rank]);
				var arrayLengths = new int[multAry.Rank];
				for (int i = 0; i < multAry.Rank; i++) {
					if (!ctorArgs[i].Match(ILCode.Ldc_I4, out arrayLengths[i])) return false;
					if (arrayLengths[i] <= 0) return false;
					multAry.Sizes[i] = (uint)(arrayLengths[i] + 1);
					multAry.LowerBounds[i] = 0;
				}

				var totalElements = arrayLengths.Aggregate(1, (t, l) => t * l);
				ILExpression[] newArr;
				int initArrayPos;
				if (ForwardScanInitializeArrayRuntimeHelper(body, pos + 1, v, multAry, totalElements, out newArr, out initArrayPos)) {
					var newStloc = new ILExpression(ILCode.Stloc, v, new ILExpression(ILCode.InitArray, multAry.ToTypeDefOrRef(), newArr));
					if (context.CalculateILSpans) {
						body[pos].AddSelfAndChildrenRecursiveILSpans(newStloc.ILSpans);
						body[initArrayPos].AddSelfAndChildrenRecursiveILSpans(newStloc.ILSpans);
					}
					body[pos] = newStloc;
					body.RemoveAt(initArrayPos);
					return true;
				}
			}
			return false;
		}

		bool ForwardScanInitializeArrayRuntimeHelper(List<ILNode> body, int pos, ILVariable array, TypeSig arrayType, int arrayLength, out ILExpression[] values, out int foundPos)
		{
			ILVariable v2;
			IMethod methodRef;
			ILExpression methodArg1;
			ILExpression methodArg2;
			IField fieldRef;
			if (body.ElementAtOrDefault(pos).Match(ILCode.Call, out methodRef, out methodArg1, out methodArg2) &&
				methodRef.Name == nameInitializeArray &&
				methodRef.DeclaringType != null &&
			    methodRef.DeclaringType.FullName == "System.Runtime.CompilerServices.RuntimeHelpers" &&
			    methodArg1.Match(ILCode.Ldloc, out v2) &&
			    array == v2 &&
			    methodArg2.Match(ILCode.Ldtoken, out fieldRef))
			{
				FieldDef fieldDef = fieldRef.ResolveFieldWithinSameModule();
				if (fieldDef != null && fieldDef.InitialValue != null) {
					var newArr = new ILExpression[Math.Min(context.Settings.MaxArrayElements, arrayLength)];
					if (DecodeArrayInitializer(arrayType.Next, fieldDef.InitialValue, newArr))
					{
						if (arrayLength != newArr.Length && newArr.Length > 0)
							newArr[newArr.Length - 1] = new ILExpression(ILCode.Ldstr, string.Format("Not showing all elements because this array is too big ({0} elements)", arrayLength));
						values = newArr;
						foundPos = pos;
						return true;
					}
				}
			}
			values = null;
			foundPos = -1;
			return false;
		}
		static readonly UTF8String nameInitializeArray = new UTF8String("InitializeArray");

		static bool DecodeArrayInitializer(TypeSig elementTypeRef, byte[] initialValue, ILExpression[] output)
		{
			elementTypeRef = elementTypeRef.RemovePinnedAndModifiers();
			TypeCode elementType = TypeAnalysis.GetTypeCode(elementTypeRef);
			switch (elementType) {
				case TypeCode.Boolean:
				case TypeCode.Byte:
					return DecodeArrayInitializer(initialValue, output, elementType, (d, i) => (int)d[i]);
				case TypeCode.SByte:
					return DecodeArrayInitializer(initialValue, output, elementType, (d, i) => (int)unchecked((sbyte)d[i]));
				case TypeCode.Int16:
					return DecodeArrayInitializer(initialValue, output, elementType, (d, i) => (int)BitConverter.ToInt16(d, i));
				case TypeCode.Char:
				case TypeCode.UInt16:
					return DecodeArrayInitializer(initialValue, output, elementType, (d, i) => (int)BitConverter.ToUInt16(d, i));
				case TypeCode.Int32:
				case TypeCode.UInt32:
					return DecodeArrayInitializer(initialValue, output, elementType, BitConverter.ToInt32);
				case TypeCode.Int64:
				case TypeCode.UInt64:
					return DecodeArrayInitializer(initialValue, output, elementType, BitConverter.ToInt64);
				case TypeCode.Single:
					return DecodeArrayInitializer(initialValue, output, elementType, BitConverter.ToSingle);
				case TypeCode.Double:
					return DecodeArrayInitializer(initialValue, output, elementType, BitConverter.ToDouble);
				case TypeCode.Object:
					var typeDef = elementTypeRef.ToTypeDefOrRef().ResolveTypeDef();
					if (typeDef != null && typeDef.IsEnum)
						return DecodeArrayInitializer(typeDef.GetEnumUnderlyingType(), initialValue, output);

					return false;
				default:
					return false;
			}
		}

		static bool DecodeArrayInitializer<T>(byte[] initialValue, ILExpression[] output, TypeCode elementType, Func<byte[], int, T> decoder)
		{
			int elementSize = ElementSizeOf(elementType);
			if (initialValue.Length < (output.Length * elementSize))
				return false;

			ILCode code = LoadCodeFor(elementType);
			for (int i = 0; i < output.Length; i++)
				output[i] = new ILExpression(code, decoder(initialValue, i * elementSize));

			return true;
		}

		private static ILCode LoadCodeFor(TypeCode elementType)
		{
			switch (elementType) {
				case TypeCode.Boolean:
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Char:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
					return ILCode.Ldc_I4;
				case TypeCode.Int64:
				case TypeCode.UInt64:
					return ILCode.Ldc_I8;
				case TypeCode.Single:
					return ILCode.Ldc_R4;
				case TypeCode.Double:
					return ILCode.Ldc_R8;
				default:
					throw new ArgumentOutOfRangeException("elementType");
			}
		}

		private static int ElementSizeOf(TypeCode elementType)
		{
			switch (elementType) {
				case TypeCode.Boolean:
				case TypeCode.Byte:
				case TypeCode.SByte:
					return 1;
				case TypeCode.Char:
				case TypeCode.Int16:
				case TypeCode.UInt16:
					return 2;
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Single:
					return 4;
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Double:
					return 8;
				default:
					throw new ArgumentOutOfRangeException("elementType");
			}
		}
		#endregion

		static readonly UTF8String nameCtor = new UTF8String(".ctor");
		/// <summary>
		/// Handles both object and collection initializers.
		/// </summary>
		bool TransformObjectInitializers(ILBlockBase block, List<ILNode> body, ILExpression expr, int pos)
		{
			if (!context.Settings.ObjectOrCollectionInitializers)
				return false;

			Debug.Assert(body[pos] == expr); // should be called for top-level expressions only
			ILVariable v;
			ILExpression newObjExpr;
			ITypeDefOrRef newObjType;
			bool isValueType;
			IMethod ctor;
			List<ILExpression> ctorArgs;
			if (expr.Match(ILCode.Stloc, out v, out newObjExpr)) {
				if (newObjExpr.Match(ILCode.Newobj, out ctor, out ctorArgs)) {
					// v = newObj(ctor, ctorArgs)
					newObjType = ctor.DeclaringType;
					isValueType = DnlibExtensions.IsValueType(newObjType);
				} else if (newObjExpr.Match(ILCode.DefaultValue, out newObjType)) {
					// v = defaultvalue(type)
					isValueType = true;
				} else {
					return false;
				}
			} else if (expr.Match(ILCode.Call, out ctor, out ctorArgs)) {
				// call(SomeStruct::.ctor, ldloca(v), remainingArgs)
				if (ctor.Name == nameCtor && ctorArgs.Count > 0 && ctorArgs[0].Match(ILCode.Ldloca, out v)) {
					isValueType = true;
					newObjType = ctor.DeclaringType;
					ctorArgs = new List<ILExpression>(ctorArgs);
					var old = ctorArgs[0];
					ctorArgs.RemoveAt(0);
					newObjExpr = new ILExpression(ILCode.Newobj, ctor, ctorArgs);
					if (context.CalculateILSpans)
						old.AddSelfAndChildrenRecursiveILSpans(newObjExpr.ILSpans);
				} else {
					return false;
				}
			} else {
				return false;
			}
			if (DnlibExtensions.IsValueType(newObjType) != isValueType)
				return false;

			int originalPos = pos;

			// don't use object initializer syntax for closures
			if (Ast.Transforms.DelegateConstruction.IsPotentialClosure(context, newObjType.ResolveWithinSameModule()))
				return false;

			ILExpression initializer = ParseObjectInitializer(body, ref pos, v, newObjExpr, IsCollectionType(newObjType.ToTypeSig()), isValueType);

			if (initializer.Arguments.Count == 1) // only newobj argument, no initializer elements
				return false;
			int totalElementCount = pos - originalPos - 1; // totalElementCount: includes elements from nested collections
			Debug.Assert(totalElementCount >= initializer.Arguments.Count - 1);

			// Verify that we can inline 'v' into the next instruction:

			if (pos >= body.Count)
				return false; // reached end of block, but there should be another instruction which consumes the initialized object

			var inlining = isValueType ? GetILInlining(body, originalPos, pos - originalPos + 1) : GetILInlining(method);
			bool recheck = true;
			if (isValueType) {
				recheck =
					// one ldloc for the use of the initialized object
					inlining.numLdloc.GetOrDefault(v) != 1 ||
					// one ldloca for each initializer argument, and also for the ctor call (if it exists)
					(inlining.numLdloca.GetOrDefault(v) != totalElementCount + (expr.Code == ILCode.Call ? 1 : 0)) ||
					// one stloc for the initial store (if no ctor call was used)
					(inlining.numStloc.GetOrDefault(v) != (expr.Code == ILCode.Call ? 0 : 1));
			}
			if (recheck) {
				// one ldloc for each initializer argument, and another ldloc for the use of the initialized object
				if (inlining.numLdloc.GetOrDefault(v) != totalElementCount + 1)
					return false;
				if (!(inlining.numStloc.GetOrDefault(v) == 1 && inlining.numLdloca.GetOrDefault(v) == 0))
					return false;
			}
			ILExpression nextExpr = body[pos] as ILExpression;
			if (!inlining.CanInlineInto(nextExpr, v, initializer))
				return false;

			if (expr.Code == ILCode.Stloc) {
				if (context.CalculateILSpans)
					expr.Arguments[0].AddSelfAndChildrenRecursiveILSpans(initializer.ILSpans);
				expr.Arguments[0] = initializer;
			} else {
				Debug.Assert(expr.Code == ILCode.Call);
				expr.Code = ILCode.Stloc;
				expr.Operand = v;
				if (context.CalculateILSpans) {
					for (int i = 0; i < expr.Arguments.Count; i++)
						expr.Arguments[i].AddSelfAndChildrenRecursiveILSpans(initializer.ILSpans);
				}
				expr.Arguments.Clear();
				expr.Arguments.Add(initializer);
			}
			// remove all the instructions that were pulled into the initializer
			if (context.CalculateILSpans) {
				for (int i = originalPos + 1; i < pos; i++)
					body[i].AddSelfAndChildrenRecursiveILSpans(initializer.ILSpans);
			}
			body.RemoveRange(originalPos + 1, pos - originalPos - 1);

			// now that we know that it's an object initializer, change all the first arguments to 'InitializedObject'
			ChangeFirstArgumentToInitializedObject(initializer);

			inlining = GetILInlining(method);
			inlining.InlineIfPossible(block, body, ref originalPos);

			return true;
		}

		/// <summary>
		/// Gets whether the type supports collection initializers.
		/// </summary>
		static bool IsCollectionType(TypeSig tr)
		{
			if (tr == null)
				return false;
			TypeDef td = tr.Resolve();
			while (td != null) {
				for (int j = 0; j < td.Interfaces.Count; j++) {
					var i = td.Interfaces[j].Interface;
					if (i.Name == nameIEnumerable && i.Namespace == nameSystemCollections)
						return true;
				}
				td = td.BaseType != null ? td.BaseType.ResolveTypeDef() : null;
			}
			return false;
		}
		static readonly UTF8String nameIEnumerable = new UTF8String("IEnumerable");
		static readonly UTF8String nameSystemCollections = new UTF8String("System.Collections");

		/// <summary>
		/// Gets whether 'expr' represents a setter in an object initializer.
		/// ('CallvirtSetter(Property, v, value)')
		/// </summary>
		static bool IsSetterInObjectInitializer(ILExpression expr)
		{
			if (expr == null)
				return false;
			if (expr.Code == ILCode.CallvirtSetter || expr.Code == ILCode.CallSetter || expr.Code == ILCode.Stfld) {
				return expr.Arguments.Count == 2;
			}
			return false;
		}

		/// <summary>
		/// Gets whether 'expr' represents the invocation of an 'Add' method in a collection initializer.
		/// </summary>
		static bool IsAddMethodCall(ILExpression expr)
		{
			IMethod addMethod;
			List<ILExpression> args;
			if (expr.Match(ILCode.Callvirt, out addMethod, out args) || expr.Match(ILCode.Call, out addMethod, out args)) {
				if (addMethod.Name == "Add")
					return args.Count >= 2;
			}
			return false;
		}

		/// <summary>
		/// Parses an object initializer.
		/// </summary>
		/// <param name="body">ILAst block</param>
		/// <param name="pos">
		/// Input: position of the instruction assigning to 'v'.
		/// Output: first position after the object initializer
		/// </param>
		/// <param name="v">The variable that holds the object being initialized</param>
		/// <param name="newObjExpr">The newobj instruction</param>
		/// <returns>InitObject instruction</returns>
		ILExpression ParseObjectInitializer(List<ILNode> body, ref int pos, ILVariable v, ILExpression newObjExpr, bool isCollection, bool isValueType)
		{
			// Take care not to modify any existing ILExpressions in here.
			// We just construct new ones around the old ones, any modifications must wait until the whole
			// object/collection initializer was analyzed.
			ILExpression objectInitializer = new ILExpression(isCollection ? ILCode.InitCollection : ILCode.InitObject, null, newObjExpr);
			Optimize_List_ILExpression.Clear();
			Optimize_List_ILExpression.Add(objectInitializer);
			while (++pos < body.Count) {
				ILExpression nextExpr = body[pos] as ILExpression;
				if (IsSetterInObjectInitializer(nextExpr)) {
					if (!AdjustInitializerStack(Optimize_List_ILExpression, Optimize_List_ILExpression2, nextExpr.Arguments[0], v, false, isValueType)) {
						CleanupInitializerStackAfterFailedAdjustment(Optimize_List_ILExpression);
						break;
					}
					Optimize_List_ILExpression[Optimize_List_ILExpression.Count - 1].Arguments.Add(nextExpr);
				} else if (IsAddMethodCall(nextExpr)) {
					if (!AdjustInitializerStack(Optimize_List_ILExpression, Optimize_List_ILExpression2, nextExpr.Arguments[0], v, true, isValueType)) {
						CleanupInitializerStackAfterFailedAdjustment(Optimize_List_ILExpression);
						break;
					}
					Optimize_List_ILExpression[Optimize_List_ILExpression.Count - 1].Arguments.Add(nextExpr);
				} else {
					// can't match any more initializers: end of object initializer
					break;
				}
			}
			return objectInitializer;
		}

		bool AdjustInitializerStack(List<ILExpression> initializerStack, List<ILExpression> getters, ILExpression argument, ILVariable v, bool isCollection, bool isValueType)
		{
			// Argument is of the form 'getter(getter(...(v)))'
			// Unpack it into a list of getters:
			getters.Clear();
			while (argument.Code == ILCode.CallvirtGetter || argument.Code == ILCode.CallGetter || argument.Code == ILCode.Ldfld) {
				getters.Add(argument);
				if (argument.Arguments.Count != 1)
					return false;
				argument = argument.Arguments[0];
			}
			// Ensure that the final argument is 'v'
			if (isValueType) {
				ILVariable loadedVar;
				if (argument.Match(ILCode.Ldloca, out loadedVar)) {
					if (loadedVar != v)
						return false;
				}
				else if (!argument.MatchLdloc(v))
					return false;
			} else {
				if (!argument.MatchLdloc(v))
					return false;
			}
			// Now compare the getters with those that are currently active on the initializer stack:
			int i;
			for (i = 1; i <= Math.Min(getters.Count, initializerStack.Count - 1); i++) {
				ILExpression g1 = initializerStack[i].Arguments[0]; // getter stored in initializer
				ILExpression g2 = getters[getters.Count - i]; // matching getter from argument
				if (g1.Operand != g2.Operand) {
					// operands differ, so we abort the comparison
					break;
				}
			}
			// Remove all initializers from the stack that were not matched with one from the argument:
			initializerStack.RemoveRange(i, initializerStack.Count - i);
			// Now create new initializers for the remaining arguments:
			for (; i <= getters.Count; i++) {
				ILExpression g = getters[getters.Count - i];
				IMemberRef mr = g.Operand as IMemberRef;
				TypeSig returnType;
				if (mr == null || mr.IsField)
					returnType = TypeAnalysis.GetFieldType((IField)mr);
				else
					returnType = TypeAnalysis.SubstituteTypeArgs(((IMethod)mr).MethodSig.GetRetType(), method: (IMethod)mr);

				ILExpression nestedInitializer = new ILExpression(
					IsCollectionType(returnType) ? ILCode.InitCollection : ILCode.InitObject,
					null, g);
				// add new initializer to its parent:
				ILExpression parentInitializer = initializerStack[initializerStack.Count - 1];
				if (parentInitializer.Code == ILCode.InitCollection) {
					// can't add children to collection initializer
					if (parentInitializer.Arguments.Count == 1) {
						// convert empty collection initializer to object initializer
						parentInitializer.Code = ILCode.InitObject;
					} else {
						return false;
					}
				}
				parentInitializer.Arguments.Add(nestedInitializer);
				initializerStack.Add(nestedInitializer);
			}
			ILExpression lastInitializer = initializerStack[initializerStack.Count - 1];
			if (isCollection) {
				return lastInitializer.Code == ILCode.InitCollection;
			} else {
				if (lastInitializer.Code == ILCode.InitCollection) {
					if (lastInitializer.Arguments.Count == 1) {
						// convert empty collection initializer to object initializer
						lastInitializer.Code = ILCode.InitObject;
						return true;
					} else {
						return false;
					}
				} else {
					return true;
				}
			}
		}

		void CleanupInitializerStackAfterFailedAdjustment(List<ILExpression> initializerStack)
		{
			// There might be empty nested initializers left over; so we'll remove those:
			while (initializerStack.Count > 1 && initializerStack[initializerStack.Count - 1].Arguments.Count == 1) {
				ILExpression parent = initializerStack[initializerStack.Count - 2];
				Debug.Assert(parent.Arguments.Last() == initializerStack[initializerStack.Count - 1]);
				if (context.CalculateILSpans)
					parent.Arguments[parent.Arguments.Count - 1].AddSelfAndChildrenRecursiveILSpans(parent.ILSpans);
				parent.Arguments.RemoveAt(parent.Arguments.Count - 1);
				initializerStack.RemoveAt(initializerStack.Count - 1);
			}
		}

		void ChangeFirstArgumentToInitializedObject(ILExpression initializer)
		{
			// Go through all elements in the initializer (so skip the newobj-instr. at the start)
			for (int i = 1; i < initializer.Arguments.Count; i++) {
				ILExpression element = initializer.Arguments[i];
				if (element.Code == ILCode.InitCollection || element.Code == ILCode.InitObject) {
					// nested collection/object initializer
					ILExpression getCollection = element.Arguments[0];
					var newExpr = new ILExpression(ILCode.InitializedObject, null);
					if (context.CalculateILSpans)
						getCollection.Arguments[0].AddSelfAndChildrenRecursiveILSpans(newExpr.ILSpans);
					getCollection.Arguments[0] = newExpr;
					ChangeFirstArgumentToInitializedObject(element); // handle the collection elements
				} else {
					var newExpr = new ILExpression(ILCode.InitializedObject, null);
					if (context.CalculateILSpans)
						element.Arguments[0].AddSelfAndChildrenRecursiveILSpans(newExpr.ILSpans);
					element.Arguments[0] = newExpr;
				}
			}
		}
	}
}
