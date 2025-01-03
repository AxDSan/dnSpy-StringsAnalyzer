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

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;

namespace dnSpy.Contracts.Search {
	/// <summary>
	/// Filter base class
	/// </summary>
	public abstract class DocumentTreeNodeFilterBase : IDocumentTreeNodeFilter {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public virtual DocumentTreeNodeFilterResult GetResult(ModuleDef mod) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(FieldDef field) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(PropertyDef prop) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(EventDef evt) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(MethodDef method) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(TypeDef type) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(ModuleRef modRef) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(BaseTypeFolderNode? node) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(DerivedTypesFolderNode? node) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(ResourcesFolderNode? node) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(ResourceElementNode? node) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(ResourceNode? node) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(ReferencesFolderNode? node) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(DerivedTypeNode? node) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(BaseTypeNode? node) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(AssemblyRef asmRef) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(AssemblyDef asm) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(MethodDef method, Local local) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(MethodDef method, ParamDef param) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(string ns, IDsDocument owner) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResultBody(MethodDef method) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResultLocals(MethodDef method) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResultOther(DocumentTreeNodeData node) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResult(IDsDocument document) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResultParamDefs(MethodDef method) => new DocumentTreeNodeFilterResult();
		public virtual DocumentTreeNodeFilterResult GetResultAttributes(IHasCustomAttribute hca) => new DocumentTreeNodeFilterResult();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
