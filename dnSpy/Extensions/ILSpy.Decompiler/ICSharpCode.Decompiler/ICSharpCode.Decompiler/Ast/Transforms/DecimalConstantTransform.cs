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

using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using dnlib.DotNet;
using System.Linq;
using System;

namespace ICSharpCode.Decompiler.Ast.Transforms {
	/// <summary>
	/// Transforms decimal constant fields.
	/// </summary>
	public class DecimalConstantTransform : DepthFirstAstVisitor<object, object>, IAstTransformPoolObject
	{
		static readonly PrimitiveType decimalType = new PrimitiveType("decimal");

		public void Reset(DecompilerContext context)
		{
		}

		static readonly UTF8String systemRuntimeCompilerServicesString = new UTF8String("System.Runtime.CompilerServices");
		static readonly UTF8String decimalConstantAttributeString = new UTF8String("DecimalConstantAttribute");
		public override object VisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
		{
			const Modifiers staticReadOnly = Modifiers.Static | Modifiers.Readonly;
			if ((fieldDeclaration.Modifiers & staticReadOnly) == staticReadOnly && decimalType.IsMatch(fieldDeclaration.ReturnType)) {
				foreach (var attributeSection in fieldDeclaration.Attributes) {
					foreach (var attribute in attributeSection.Attributes) {
						ITypeDefOrRef tr = attribute.Type.Annotation<ITypeDefOrRef>();
						if (tr != null && tr.Compare(systemRuntimeCompilerServicesString, decimalConstantAttributeString)) {
							attribute.Remove();
							if (attributeSection.Attributes.Count == 0)
								attributeSection.Remove();
							fieldDeclaration.Modifiers = (fieldDeclaration.Modifiers & ~staticReadOnly) | Modifiers.Const;
							var comments = fieldDeclaration.GetChildrenByRole(Roles.Comment).ToArray();
							Array.Reverse(comments);
							foreach (var c in comments) {
								c.Remove();
								fieldDeclaration.InsertChildAfter(null, c, Roles.Comment);
							}
							return null;
						}
					}
				}
			}
			return null;
		}

		public override object VisitParameterDeclaration(ParameterDeclaration parameterDeclaration, object data)
		{
			foreach (var attributeSection in parameterDeclaration.Attributes) {
				foreach (var attribute in attributeSection.Attributes) {
					ITypeDefOrRef tr = attribute.Type.Annotation<ITypeDefOrRef>();
					if (tr != null && tr.Compare(systemRuntimeCompilerServicesString, decimalConstantAttributeString)) {
						attribute.Remove();
						if (attributeSection.Attributes.Count == 0)
							attributeSection.Remove();
						return null;
					}
				}
			}
			return null;
		}

		public void Run(AstNode compilationUnit)
		{
			compilationUnit.AcceptVisitor(this, null);
		}
	}
}
