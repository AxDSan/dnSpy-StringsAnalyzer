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

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// Creates <see cref="DocumentTreeNodeData"/>
	/// </summary>
	public interface IDocumentTreeNodeProvider {
		/// <summary>
		/// Creates a <see cref="AssemblyDocumentNode"/>
		/// </summary>
		/// <param name="asmDocument">Assembly</param>
		/// <returns></returns>
		AssemblyDocumentNode CreateAssembly(IDsDotNetDocument asmDocument);

		/// <summary>
		/// Creates a <see cref="ModuleDocumentNode"/>
		/// </summary>
		/// <param name="modDocument">Module</param>
		/// <returns></returns>
		ModuleDocumentNode CreateModule(IDsDotNetDocument modDocument);

		/// <summary>
		/// Creates a <see cref="AssemblyReferenceNode"/>
		/// </summary>
		/// <param name="asmRef">Assembly reference</param>
		/// <param name="ownerModule">Owner module</param>
		/// <returns></returns>
		AssemblyReferenceNode Create(AssemblyRef asmRef, ModuleDef ownerModule);

		/// <summary>
		/// Creates a <see cref="ModuleReferenceNode"/>
		/// </summary>
		/// <param name="modRef">Module reference</param>
		/// <returns></returns>
		ModuleReferenceNode Create(ModuleRef modRef);

		/// <summary>
		/// Creates an event <see cref="MethodNode"/>
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		MethodNode CreateEvent(MethodDef method);

		/// <summary>
		/// Creates a property <see cref="MethodNode"/>
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		MethodNode CreateProperty(MethodDef method);

		/// <summary>
		/// Creates a <see cref="NamespaceNode"/>
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns></returns>
		NamespaceNode Create(string name);

		/// <summary>
		/// Creates a non-nested <see cref="TypeNode"/>
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		TypeNode Create(TypeDef type);

		/// <summary>
		/// Creates a nested <see cref="TypeNode"/>
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		TypeNode CreateNested(TypeDef type);

		/// <summary>
		/// Creates a <see cref="MethodNode"/>
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		MethodNode Create(MethodDef method);

		/// <summary>
		/// Creates a <see cref="PropertyNode"/>
		/// </summary>
		/// <param name="property">Property</param>
		/// <returns></returns>
		PropertyNode Create(PropertyDef property);

		/// <summary>
		/// Creates a <see cref="EventNode"/>
		/// </summary>
		/// <param name="event">Event</param>
		/// <returns></returns>
		EventNode Create(EventDef @event);

		/// <summary>
		/// Creates a <see cref="FieldNode"/>
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		FieldNode Create(FieldDef field);
	}
}
