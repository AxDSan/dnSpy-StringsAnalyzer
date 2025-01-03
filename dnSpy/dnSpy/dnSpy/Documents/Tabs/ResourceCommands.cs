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

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Documents.Tabs {
	sealed class ResourceRef {
		public ModuleDef Module { get; }
		public string Filename { get; }
		public string ResourceName { get; }

		ResourceRef(ModuleDef module, string resourcesFilename, string resourceName) {
			Module = module;
			Filename = resourcesFilename;
			ResourceName = resourceName;
		}

		public static ResourceRef? TryCreate(object? o) {
			if (o is PropertyDef pd) {
				if (pd.SetMethod is not null)
					return null;
				o = pd.GetMethod;
			}
			var md = o as MethodDef;
			if (!(md?.DeclaringType is TypeDef type))
				return null;
			var resourceName = GetResourceName(md);
			if (resourceName is null)
				return null;
			var resourcesFilename = GetResourcesFilename(type);
			if (resourcesFilename is null)
				return null;
			var module = type.Module;
			if (module is null)
				return null;

			return new ResourceRef(module, resourcesFilename, resourceName);
		}

		static string? GetResourcesFilename(TypeDef type) {
			foreach (var m in type.Methods) {
				if (!m.IsStatic)
					continue;
				if (m.MethodSig.GetParamCount() != 0)
					continue;
				var ret = m.MethodSig.GetRetType();
				if (ret is null || ret.FullName != "System.Resources.ResourceManager")
					continue;
				var body = m.Body;
				if (body is null)
					continue;

				ITypeDefOrRef? resourceType = null;
				string? resourceName = null;
				foreach (var instr in body.Instructions) {
					if (instr.OpCode.Code == Code.Ldstr) {
						resourceName = instr.Operand as string;
						continue;
					}
					if (instr.OpCode.Code == Code.Newobj) {
						var ctor = instr.Operand as IMethod;
						if (ctor is null || ctor.DeclaringType is null || ctor.DeclaringType.FullName != "System.Resources.ResourceManager")
							continue;
						var ctorFullName = ctor.FullName;
						if (ctorFullName == "System.Void System.Resources.ResourceManager::.ctor(System.Type)")
							return resourceType is null ? null : resourceType.ReflectionFullName + ".resources";
						if (ctorFullName == "System.Void System.Resources.ResourceManager::.ctor(System.String,System.Reflection.Assembly)" ||
							ctorFullName == "System.Void System.Resources.ResourceManager::.ctor(System.String,System.Reflection.Assembly,System.Type)")
							return resourceName is null ? null : resourceName + ".resources";
					}
				}
			}

			return null;
		}

		static string? GetResourceName(MethodDef method) {
			if (!IsResourcesClass(method.DeclaringType))
				return null;

			var body = method.Body;
			if (body is null)
				return null;

			bool foundGetMethod = false;
			string? resourceName = null;
			foreach (var instr in body.Instructions) {
				if (instr.OpCode.Code == Code.Ldstr) {
					resourceName = instr.Operand as string;
					continue;
				}
				if (instr.OpCode.Code == Code.Callvirt) {
					var getStringMethod = instr.Operand as IMethod;
					if (getStringMethod is null)
						continue;
					if (getStringMethod.Name != "GetObject" && getStringMethod.Name != "GetStream" && getStringMethod.Name != "GetString")
						continue;
					var getStringDeclType = getStringMethod.DeclaringType;
					if (getStringDeclType is null || getStringDeclType.FullName != "System.Resources.ResourceManager")
						continue;
					foundGetMethod = true;
					break;
				}
			}
			return foundGetMethod ? resourceName : null;
		}

		static bool IsResourcesClass(TypeDef type) {
			if (type.BaseType is null || type.BaseType.FullName != "System.Object")
				return false;
			if (type.Fields.Count != 2)
				return false;
			bool hasCultureInfo = false;
			bool hasResourceManager = false;
			foreach (var fd in type.Fields) {
				if (!fd.IsStatic)
					continue;
				var ftypeName = fd.FieldType?.FullName ?? string.Empty;
				if (ftypeName == "System.Globalization.CultureInfo")
					hasCultureInfo = true;
				else if (ftypeName == "System.Resources.ResourceManager")
					hasResourceManager = true;
			}
			return hasCultureInfo && hasResourceManager;
		}
	}

	static class GoToResourceCommand {
		[ExportMenuItem(Header = "res:GoToResourceCommand", Group = MenuConstants.GROUP_CTX_DOCVIEWER_OTHER, Order = 20)]
		sealed class TextEditorCommand : MenuItemBase {
			readonly IDocumentTabService documentTabService;

			[ImportingConstructor]
			TextEditorCommand(IDocumentTabService documentTabService) => this.documentTabService = documentTabService;

			public override void Execute(IMenuItemContext context) => GoToResourceCommand.Execute(documentTabService, TryCreate(context));

			static ResourceRef? TryCreate(TextReference @ref) {
				if (@ref is null)
					return null;
				return ResourceRef.TryCreate(@ref.Reference);
			}

			static ResourceRef? TryCreate(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
					return null;
				return TryCreate(context.Find<TextReference>());
			}

			public override bool IsVisible(IMenuItemContext context) => GoToResourceCommand.IsVisible(TryCreate(context));
		}

		[ExportMenuItem(Header = "res:GoToResourceCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_OTHER, Order = 20)]
		sealed class DocumentTreeViewCommand : MenuItemBase {
			readonly IDocumentTabService documentTabService;

			[ImportingConstructor]
			DocumentTreeViewCommand(IDocumentTabService documentTabService) => this.documentTabService = documentTabService;

			public override void Execute(IMenuItemContext context) => GoToResourceCommand.Execute(documentTabService, TryCreate(context));

			static ResourceRef? TryCreate(TreeNodeData[] nodes) {
				if (nodes is null || nodes.Length != 1)
					return null;
				if (nodes[0] is IMDTokenNode tokNode)
					return ResourceRef.TryCreate(tokNode.Reference);
				return null;
			}

			static ResourceRef? TryCreate(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID))
					return null;
				return TryCreate(context.Find<TreeNodeData[]>());
			}

			public override bool IsVisible(IMenuItemContext context) => GoToResourceCommand.IsVisible(TryCreate(context));
		}

		static bool IsVisible(ResourceRef? resRef) => resRef is not null;

		static void Execute(IDocumentTabService documentTabService, ResourceRef? resRef) {
			if (resRef is null)
				return;
			var modNode = documentTabService.DocumentTreeView.FindNode(resRef.Module);
			Debug2.Assert(modNode is not null);
			if (modNode is null)
				return;
			modNode.TreeNode.EnsureChildrenLoaded();
			var resDirNode = modNode.TreeNode.DataChildren.FirstOrDefault(a => a is ResourcesFolderNode);
			Debug2.Assert(resDirNode is not null);
			if (resDirNode is null)
				return;
			resDirNode.TreeNode.EnsureChildrenLoaded();
			var resSetNode = resDirNode.TreeNode.DataChildren.FirstOrDefault(a => a is ResourceElementSetNode && ((ResourceElementSetNode)a).Name == resRef.Filename);
			Debug2.Assert(resSetNode is not null);
			if (resSetNode is null)
				return;
			resSetNode.TreeNode.EnsureChildrenLoaded();
			var resNode = resSetNode.TreeNode.DataChildren.FirstOrDefault(a => ResourceElementNode.GetResourceElement((DocumentTreeNodeData)a) is ResourceElement resourceElement && resourceElement.Name == resRef.ResourceName);
			Debug2.Assert(resNode is not null);
			if (resNode is null)
				return;
			documentTabService.FollowReference(resNode);
		}
	}
}
