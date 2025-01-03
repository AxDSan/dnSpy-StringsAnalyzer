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

using System.Diagnostics;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnlib.DotNet;

namespace dndbg.DotNet {
	sealed class CorPropertyDef : PropertyDef, ICorHasCustomAttribute {
		readonly CorModuleDef readerModule;
		readonly uint origRid;
		readonly CorTypeDef ownerType;

		public MDToken OriginalToken => new MDToken(MDToken.Table, origRid);
		public CorTypeDef OwnerType => ownerType;

		public CorPropertyDef(CorModuleDef readerModule, uint rid, CorTypeDef ownerType) {
			this.readerModule = readerModule;
			this.rid = rid;
			origRid = rid;
			this.ownerType = ownerType;
		}

		public bool MustInitialize {
			get { lock (lockObj) return mustInitialize; }
			set { lock (lockObj) mustInitialize = value; }
		}
		bool mustInitialize;
		readonly object lockObj = new object();

		public void Initialize() {
			lock (lockObj) {
				if (!mustInitialize)
					return;
				Initialize_NoLock();
				mustInitialize = false;
			}
		}

		void Initialize_NoLock() {
			try {
				if (initCounter++ != 0) {
					Debug.Fail("Initialize() called recursively");
					return;
				}

				declaringType2 = ownerType;
				InitNameAndAttrs_NoLock();
				ResetConstant();
				ResetMethods();
				InitCustomAttributes_NoLock();
			}
			finally {
				initCounter--;
			}
		}
		int initCounter;

		void InitCustomAttributes_NoLock() => customAttributes = null;
		protected override void InitializeCustomAttributes() =>
			readerModule.InitCustomAttributes(this, ref customAttributes, GenericParamContext.Create(ownerType));

		protected override Constant? GetConstant_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			var c = MDAPI.GetPropertyConstant(mdi, token, out var etype);
			if (etype == CorElementType.End)
				return null;
			return readerModule.UpdateRowId(new ConstantUser(c, (ElementType)etype));
		}

		void InitNameAndAttrs_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			Name = Utils.GetUTF8String(MDAPI.GetUtf8Name(mdi, OriginalToken.Raw), MDAPI.GetPropertyName(mdi, token) ?? string.Empty);
			Attributes = MDAPI.GetPropertyAttributes(mdi, token);
			Type = readerModule.ReadSignature(MDAPI.GetPropertySignatureBlob(mdi, token), new GenericParamContext(ownerType));
		}

		protected override void InitializePropertyMethods_NoLock() {
			if (otherMethods is not null)
				return;
			ownerType.InitializeProperty(this, out var newGetMethods, out var newSetMethods, out var newOtherMethods);
			getMethods = newGetMethods;
			setMethods = newSetMethods;
			// Must be initialized last
			otherMethods = newOtherMethods;
		}
	}
}
