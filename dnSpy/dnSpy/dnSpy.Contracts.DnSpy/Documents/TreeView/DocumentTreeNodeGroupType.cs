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

using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// Default <see cref="ITreeNodeGroup"/> instances
	/// </summary>
	public enum DocumentTreeNodeGroupType {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		AssemblyRefTreeNodeGroupReferences,
		AssemblyRefTreeNodeGroupAssemblyRef,
		ModuleRefTreeNodeGroupReferences,
		ReferencesFolderTreeNodeGroupModule,
		ResourcesFolderTreeNodeGroupModule,
		NamespaceTreeNodeGroupModule,
		TypeTreeNodeGroupNamespace,
		TypeTreeNodeGroupType,
		BaseTypeFolderTreeNodeGroupType,
		BaseTypeTreeNodeGroupBaseType,
		InterfaceBaseTypeTreeNodeGroupBaseType,
		DerivedTypesFolderTreeNodeGroupType,
		MessageTreeNodeGroupDerivedTypes,
		DerivedTypeTreeNodeGroupDerivedTypes,
		MethodTreeNodeGroupType,
		MethodTreeNodeGroupProperty,
		MethodTreeNodeGroupEvent,
		FieldTreeNodeGroupType,
		EventTreeNodeGroupType,
		PropertyTreeNodeGroupType,
		ResourceTreeNodeGroup,
		ResourceElementTreeNodeGroup,
		TypeReferenceTreeNodeGroupTypeReferences,
		TypeSpecsFolderTreeNodeGroupTypeReference,
		MethodReferencesFolderTreeNodeGroupTypeReference,
		PropertyReferencesFolderTreeNodeGroupTypeReference,
		EventReferencesFolderTreeNodeGroupTypeReference,
		FieldReferencesFolderTreeNodeGroupTypeReference,
		TypeSpecTreeNodeGroupTypeSpecsFolder,
		MethodReferenceTreeNodeGroupMethodReferencesFolder,
		PropertyReferenceTreeNodeGroupPropertyReferencesFolder,
		EventReferenceTreeNodeGroupEventReferencesFolder,
		FieldReferenceTreeNodeGroupFieldReferencesFolder,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
