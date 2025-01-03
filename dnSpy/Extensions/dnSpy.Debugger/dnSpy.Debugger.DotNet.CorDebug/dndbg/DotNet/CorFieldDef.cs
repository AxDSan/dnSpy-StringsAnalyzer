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
using System.Diagnostics;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.PE;

namespace dndbg.DotNet {
	sealed class CorFieldDef : FieldDef, ICorHasCustomAttribute, ICorHasFieldMarshal {
		readonly CorModuleDef readerModule;
		readonly uint origRid;
		readonly CorTypeDef ownerType;
		FieldAttributes origAttrs;

		public MDToken OriginalToken => new MDToken(MDToken.Table, origRid);
		public CorTypeDef OwnerType => ownerType;

		public CorFieldDef(CorModuleDef readerModule, uint rid, CorTypeDef ownerType) {
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
				FieldOffset = ownerType.GetFieldOffset(this);
				ResetConstant();
				ResetMarshalType();
				ResetRVA();
				ResetInitialValue();
				InitCustomAttributes_NoLock();
				InitSignature_NoLock();
			}
			finally {
				initCounter--;
			}
		}
		int initCounter;

		protected override Constant? GetConstant_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			var c = MDAPI.GetFieldConstant(mdi, token, out var etype);
			if (etype == CorElementType.End)
				return null;
			return readerModule.UpdateRowId(new ConstantUser(c, (ElementType)etype));
		}

		protected override MarshalType? GetMarshalType_NoLock() => readerModule.ReadMarshalType(this, GenericParamContext.Create(ownerType));

		protected override RVA GetRVA_NoLock() {
			GetFieldRVA_NoLock(out var rva2);
			return rva2;
		}

		protected override byte[]? GetInitialValue_NoLock() {
			if (!GetFieldRVA_NoLock(out var rva2))
				return null;
			return ReadInitialValue_NoLock(rva2);
		}

		void InitNameAndAttrs_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			Name = Utils.GetUTF8String(MDAPI.GetUtf8Name(mdi, OriginalToken.Raw), MDAPI.GetFieldName(mdi, token) ?? string.Empty);
			Attributes = origAttrs = MDAPI.GetFieldAttributes(mdi, token);
		}

		unsafe void InitSignature_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			var data = MDAPI.GetFieldSignatureBlob(mdi, token);
			Signature = readerModule.ReadSignature(data, GenericParamContext.Create(ownerType));
		}

		void InitCustomAttributes_NoLock() => customAttributes = null;
		protected override void InitializeCustomAttributes() =>
			readerModule.InitCustomAttributes(this, ref customAttributes, GenericParamContext.Create(ownerType));

		bool GetFieldRVA_NoLock(out RVA rva) {
			rva = 0;
			if ((origAttrs & FieldAttributes.HasFieldRVA) == 0)
				return false;

			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			var rva2 = MDAPI.GetRVA(mdi, token);
			if (rva2 is null)
				return false;
			rva = (RVA)rva2.Value;
			return true;
		}

		byte[]? ReadInitialValue_NoLock(RVA rva) {
			// rva could be 0 if it's a dynamic module. The caller is responsible for checking the
			// HasFieldRVA bit before calling this method.
			int ptrSize = IntPtr.Size;
			if (!GetFieldSize(ownerType, signature as FieldSig, ptrSize, out uint size))
				return null;
			if (size >= int.MaxValue)
				return null;
			return readerModule.ReadFieldInitialValue(this, (uint)rva, (int)size);
		}
	}
}
