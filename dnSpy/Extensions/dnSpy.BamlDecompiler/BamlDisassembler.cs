/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using dnlib.DotNet;
using dnSpy.BamlDecompiler.Baml;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Utilities;

namespace dnSpy.BamlDecompiler {
	sealed class BamlDisassembler {
		#region Record handler map

		static Action<BamlContext, BamlRecord> Thunk<TRecord>(Action<BamlContext, TRecord> handler) where TRecord : BamlRecord => (ctx, record) => handler(ctx, (TRecord)record);

		readonly Dictionary<BamlRecordType, Action<BamlContext, BamlRecord>> handlerMap =
			new Dictionary<BamlRecordType, Action<BamlContext, BamlRecord>>();

		void InitRecordHandlers() {
			handlerMap[BamlRecordType.XmlnsProperty] = Thunk<XmlnsPropertyRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PresentationOptionsAttribute] = Thunk<PresentationOptionsAttributeRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PIMapping] = Thunk<PIMappingRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.AssemblyInfo] = Thunk<AssemblyInfoRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.Property] = Thunk<PropertyRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyWithConverter] = Thunk<PropertyWithConverterRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyCustom] = Thunk<PropertyCustomRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.DefAttribute] = Thunk<DefAttributeRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.DefAttributeKeyString] = Thunk<DefAttributeKeyStringRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.TypeInfo] = Thunk<TypeInfoRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.TypeSerializerInfo] = Thunk<TypeSerializerInfoRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.AttributeInfo] = Thunk<AttributeInfoRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.StringInfo] = Thunk<StringInfoRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.Text] = Thunk<TextRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.TextWithConverter] = Thunk<TextWithConverterRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.TextWithId] = Thunk<TextWithIdRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.LiteralContent] = Thunk<LiteralContentRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.RoutedEvent] = Thunk<RoutedEventRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.DocumentStart] = Thunk<DocumentStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.ElementStart] = Thunk<ElementStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.KeyElementStart] = Thunk<KeyElementStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.ConnectionId] = Thunk<ConnectionIdRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyWithExtension] = Thunk<PropertyWithExtensionRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyTypeReference] = Thunk<PropertyTypeReferenceRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyStringReference] = Thunk<PropertyStringReferenceRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyWithStaticResourceId] = Thunk<PropertyWithStaticResourceIdRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.ContentProperty] = Thunk<ContentPropertyRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.DefAttributeKeyType] = Thunk<DefAttributeKeyTypeRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyListStart] = Thunk<PropertyListStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyDictionaryStart] = Thunk<PropertyDictionaryStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyArrayStart] = Thunk<PropertyArrayStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyComplexStart] = Thunk<PropertyComplexStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.ConstructorParameterType] = Thunk<ConstructorParameterTypeRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.DeferableContentStart] = Thunk<DeferableContentStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.StaticResourceStart] = Thunk<StaticResourceStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.StaticResourceId] = Thunk<StaticResourceIdRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.OptimizedStaticResource] = Thunk<OptimizedStaticResourceRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.LineNumberAndPosition] = Thunk<LineNumberAndPositionRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.LinePosition] = Thunk<LinePositionRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.NamedElementStart] = Thunk<NamedElementStartRecord>(DisassembleRecord);
		}

		#endregion

		readonly IDecompilerOutput output;
		readonly CancellationToken token;

		public BamlDisassembler(IDecompilerOutput output, CancellationToken token) {
			this.output = output;
			this.token = token;

			InitRecordHandlers();
		}

		void WriteText(string value) => output.Write(value, BoxedTextColor.Text);

		void WriteRecordField(string value) => output.Write(value, BoxedTextColor.StaticField);

		void WriteFlagField(string value) => output.Write(value, BoxedTextColor.EnumField);

		void WriteOperator(string value) => output.Write(value, BoxedTextColor.Operator);

		void WriteComma(bool spaceAfterComma) {
			output.Write(",", BoxedTextColor.Punctuation);
			if (spaceAfterComma)
				WriteSpace();
		}

		void WriteSpace(bool writeSpace = true) {
			if (writeSpace)
				WriteText(" ");
		}

		void WriteString(string value) {
			string str = SimpleTypeConverter.ToString(value, true);
			int start = output.NextPosition;
			output.Write(str, BoxedTextColor.String);
			int end = output.NextPosition;
			output.AddBracePair(new TextSpan(start, 1), new TextSpan(end - 1, 1), CodeBracesRangeFlags.DoubleQuotes);
		}

		void WriteIdentifierNameString(string value, bool allowSpaces = false) {
			int start = output.NextPosition;
			output.Write($"\"{IdentifierEscaper.Escape(value, allowSpaces)}\"", BoxedTextColor.String);
			int end = output.NextPosition;
			output.AddBracePair(new TextSpan(start, 1), new TextSpan(end - 1, 1), CodeBracesRangeFlags.DoubleQuotes);
		}

		const DecompilerReferenceFlags toolTipReferenceFlags = DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Hidden | DecompilerReferenceFlags.NoFollow;

		void WriteHexNumber(byte num) => output.Write($"0x{num:x2}", num, toolTipReferenceFlags, BoxedTextColor.Number);

		void WriteHexNumber(ushort num) => output.Write($"0x{num:x4}", num, toolTipReferenceFlags, BoxedTextColor.Number);

		void WriteHexNumber(uint num) => output.Write($"0x{num:x8}", num, toolTipReferenceFlags, BoxedTextColor.Number);

		void WriteBool(bool value) => output.Write(value ? "true" : "false", BoxedTextColor.Keyword);

		void WriteVersion(BamlDocument.BamlVersion value) {
			var bh = BracePairHelper.Create(output, "[", CodeBracesRangeFlags.SquareBrackets);
			WriteHexNumber(value.Major);
			WriteComma(spaceAfterComma: true);
			WriteHexNumber(value.Minor);
			bh.Write("]");
		}

		readonly Dictionary<ushort, object> assemblyReferences = new Dictionary<ushort, object>();

		object GetAssemblyReferenceObject(BamlContext ctx, ushort id) {
			if (assemblyReferences.TryGetValue(id, out var reference))
				return reference;
			string assemblyFullName;
			if (id == 0xffff)
				assemblyFullName = ctx.KnownThings.PresentationFrameworkAssembly.FullName;
			else if (ctx.AssemblyIdMap.TryGetValue(id, out var assemblyRecord))
				assemblyFullName = assemblyRecord.AssemblyFullName;
			else
				assemblyFullName = null;
			return assemblyReferences[id] = BamlAssemblyReference.Create(assemblyFullName);
		}

		void WriteAssemblyId(BamlContext ctx, ushort id, bool isDefinition = false) =>
			output.Write($"0x{id:x4}", GetAssemblyReferenceObject(ctx, id), DecompilerReferenceFlags.Local | (isDefinition ? DecompilerReferenceFlags.Definition : 0), BoxedTextColor.Number);

		readonly Dictionary<ushort, object> typeReferences = new Dictionary<ushort, object>();

		object GetTypeReferenceObject(BamlContext ctx, ushort id) {
			if (typeReferences.TryGetValue(id, out var reference))
				return reference;
			if (id > 0x7fff) {
				return typeReferences[id] = BamlTypeReference.Create(ctx.KnownThings.Types((KnownTypes)(-id)).TypeDef);
			}
			if (ctx.TypeIdMap.TryGetValue(id, out var typeRecord)) {
				var type = TypeNameParser.ParseReflection(ctx.Module, typeRecord.TypeFullName, new DummyAssemblyRefFinder(ctx.ResolveAssembly(typeRecord.AssemblyId)));
				if (type is not null)
					return typeReferences[id] = BamlTypeReference.Create(type);
			}
			return typeReferences[id] = null;
		}

		void WriteTypeId(BamlContext ctx, ushort id, bool isDefinition = false) =>
			output.Write($"0x{id:x4}", GetTypeReferenceObject(ctx, id), DecompilerReferenceFlags.Local | (isDefinition ? DecompilerReferenceFlags.Definition : 0), BoxedTextColor.Number);

		readonly Dictionary<ushort, object> attributeReferences = new Dictionary<ushort, object>();

		object GetAttributeReferenceObject(BamlContext ctx, ushort id) {
			if (attributeReferences.TryGetValue(id, out var reference))
				return reference;

			if (id > 0x7fff) {
				var knownMember = ctx.KnownThings.Members((KnownMembers)(-id));
				return attributeReferences[id] = BamlAttributeReference.Create(knownMember.DeclaringType.TypeDef, knownMember.Name);
			}
			if (ctx.AttributeIdMap.TryGetValue(id, out var attrInfo)) {
				if (attrInfo.OwnerTypeId > 0x7fff) {
					return attributeReferences[id] = BamlAttributeReference.Create(ctx.KnownThings.Types((KnownTypes)(-attrInfo.OwnerTypeId)).TypeDef, attrInfo.Name);
				}
				if (ctx.TypeIdMap.TryGetValue(attrInfo.OwnerTypeId, out var typeRecord)) {
					var type = TypeNameParser.ParseReflection(ctx.Module, typeRecord.TypeFullName, new DummyAssemblyRefFinder(ctx.ResolveAssembly(typeRecord.AssemblyId)));
					if (type is not null)
						return attributeReferences[id] = BamlAttributeReference.Create(type, attrInfo.Name);
				}
			}
			return attributeReferences[id] = null;
		}

		void WriteAttributeId(BamlContext ctx, ushort id, bool isDefinition = false) =>
			output.Write($"0x{id:x4}", GetAttributeReferenceObject(ctx, id), DecompilerReferenceFlags.Local | (isDefinition ? DecompilerReferenceFlags.Definition : 0), BoxedTextColor.Number);

		readonly Dictionary<ushort, object> stringReferences = new Dictionary<ushort, object>();

		object GetStringReferenceObject(BamlContext ctx, ushort id) {
			if (stringReferences.TryGetValue(id, out var reference))
				return reference;
			string str;
			if (id > 0x7fff)
				str = ctx.KnownThings.Strings((short)-id);
			else if (ctx.StringIdMap.TryGetValue(id, out var stringInfo))
				str = stringInfo.Value;
			else
				str = null;
			return stringReferences[id] = BamlStringReference.Create(SimpleTypeConverter.ToString(str, true));
		}

		void WriteStringId(BamlContext ctx, ushort id, bool isDefinition = false) =>
			output.Write($"0x{id:x4}", GetStringReferenceObject(ctx, id), DecompilerReferenceFlags.Local | (isDefinition ? DecompilerReferenceFlags.Definition : 0), BoxedTextColor.Number);

		readonly Dictionary<ushort, object> resourceReferences = new Dictionary<ushort, object>();

		object GetResourceReferenceObject(BamlContext ctx, ushort id) {
			if (resourceReferences.TryGetValue(id, out var reference))
				return reference;
			bool isKey = true;
			short bamlId = unchecked((short)-id);
			if (bamlId > 232 && bamlId < 464) {
				bamlId -= 232;
				isKey = false;
			}
			else if (bamlId > 464 && bamlId < 467) {
				bamlId -= 231;
			}
			else if (bamlId > 467 && bamlId < 470) {
				bamlId -= 234;
				isKey = false;
			}
			var res = ctx.KnownThings.Resources(bamlId);
			return resourceReferences[id] = isKey
					? BamlResourceReference.CreateKey(res.TypeName, res.KeyName)
					: BamlResourceReference.CreateProperty(res.TypeName, res.PropertyName);
		}

		void WriteResourceId(BamlContext ctx, ushort id) =>
			output.Write($"0x{id:x4}", GetResourceReferenceObject(ctx, id), DecompilerReferenceFlags.Local, BoxedTextColor.Number);

		readonly Dictionary<BamlRecord, object> recordReferences = new Dictionary<BamlRecord, object>();

		object GetRecordReference(BamlRecord record) {
			if (recordReferences.TryGetValue(record, out var value))
				return value;
			return recordReferences[record] = BamlRecordReference.Create(record);
		}

		void WriteRecordName(BamlRecord record, bool isDefinition = false) =>
			output.Write(record.Type.ToString(), GetRecordReference(record), DecompilerReferenceFlags.Local | (isDefinition ? DecompilerReferenceFlags.Definition : 0), BoxedTextColor.Keyword);

		public void Disassemble(ModuleDef module, BamlDocument document) {
			WriteText("Signature");
			output.Write(":", BoxedTextColor.Punctuation);
			WriteText("      \t");
			WriteString(document.Signature);
			output.WriteLine();

			WriteText("Reader Version");
			output.Write(":", BoxedTextColor.Punctuation);
			WriteText(" \t");
			WriteVersion(document.ReaderVersion);
			output.WriteLine();

			WriteText("Updater Version");
			output.Write(":", BoxedTextColor.Punctuation);
			WriteText("\t");
			WriteVersion(document.UpdaterVersion);
			output.WriteLine();

			WriteText("Writer Version");
			output.Write(":", BoxedTextColor.Punctuation);
			WriteText(" \t");
			WriteVersion(document.WriterVersion);
			output.WriteLine();

			WriteText("Record #:       \t");
			output.Write(document.Count.ToString(CultureInfo.InvariantCulture), document.Count, toolTipReferenceFlags, BoxedTextColor.Number);
			output.WriteLine();

			output.WriteLine();

			// Reset all references
			assemblyReferences.Clear();
			typeReferences.Clear();
			attributeReferences.Clear();
			stringReferences.Clear();
			resourceReferences.Clear();
			recordReferences.Clear();

			var ctx = BamlContext.ConstructContext(module, document, token);
			scopeStack.Clear();
			foreach (var record in document) {
				token.ThrowIfCancellationRequested();
				DisassembleRecord(ctx, record);
			}
		}

		readonly Stack<(BamlRecord Record, TextSpan HeaderSpan)> scopeStack = new Stack<(BamlRecord Record, TextSpan HeaderSpan)>();

		void DisassembleRecord(BamlContext ctx, BamlRecord record) {
			TextSpan startBraceSpan = default;
			if (BamlNode.IsFooter(record)) {
				while (scopeStack.Count > 0 && !BamlNode.IsMatchHeaderAndFooter(scopeStack.Peek().Record, record)) {
					scopeStack.Pop();
					output.DecreaseIndent();
				}
				if (scopeStack.Count > 0) {
					startBraceSpan = scopeStack.Pop().HeaderSpan;
					output.DecreaseIndent();
				}
			}

			int recordNameStart = output.NextPosition;
			WriteRecordName(record, isDefinition: true);
			var recordSpan = new TextSpan(recordNameStart, output.NextPosition - recordNameStart);

			if (!startBraceSpan.IsEmpty)
				output.AddBracePair(startBraceSpan, recordSpan, CodeBracesRangeFlags.OtherBlockBraces);

			if (handlerMap.TryGetValue(record.Type, out var handler)) {
				WriteSpace();
				var bh = BracePairHelper.Create(output, "[", CodeBracesRangeFlags.SquareBrackets);
				handler(ctx, record);
				bh.Write("]");
			}

			output.WriteLine();

			if (BamlNode.IsHeader(record)) {
				scopeStack.Push((record, recordSpan));
				output.IncreaseIndent();
			}
		}

		#region Record handlers

		void DisassembleRecord(BamlContext ctx, XmlnsPropertyRecord record) {
			WriteRecordField("Prefix");
			WriteOperator("=");
			WriteString(record.Prefix);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("XmlNamespace");
			WriteOperator("=");
			WriteString(record.XmlNamespace);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("AssemblyIds");
			WriteOperator("=");
			var bh = BracePairHelper.Create(output, "{", CodeBracesRangeFlags.CurlyBraces);
			for (int i = 0; i < record.AssemblyIds.Length; i++) {
				if (i != 0)
					WriteComma(spaceAfterComma: true);
				WriteAssemblyId(ctx, record.AssemblyIds[i]);
			}
			bh.Write("}");
		}

		void DisassembleRecord(BamlContext ctx, PresentationOptionsAttributeRecord record) {
			WriteRecordField("Value");
			WriteOperator("=");
			WriteString(record.Value);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("NameId");
			WriteOperator("=");
			WriteStringId(ctx, record.NameId);
		}

		void DisassembleRecord(BamlContext ctx, PIMappingRecord record) {
			WriteRecordField("XmlNamespace");
			WriteOperator("=");
			WriteString(record.XmlNamespace);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("ClrNamespace");
			WriteOperator("=");
			WriteString(record.ClrNamespace);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("AssemblyId");
			WriteOperator("=");
			WriteAssemblyId(ctx, record.AssemblyId);
		}

		void DisassembleRecord(BamlContext ctx, AssemblyInfoRecord record) {
			WriteRecordField("AssemblyId");
			WriteOperator("=");
			WriteAssemblyId(ctx, record.AssemblyId, isDefinition: true);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("AssemblyFullName");
			WriteOperator("=");
			WriteString(record.AssemblyFullName);
		}

		void DisassembleRecord(BamlContext ctx, PropertyRecord record) {
			WriteRecordField("AttributeId");
			WriteOperator("=");
			WriteAttributeId(ctx, record.AttributeId);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("Value");
			WriteOperator("=");
			WriteString(record.Value);
		}

		void DisassembleRecord(BamlContext ctx, PropertyWithConverterRecord record) {
			DisassembleRecord(ctx, (PropertyRecord)record);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("ConverterTypeId");
			WriteOperator("=");
			WriteTypeId(ctx, record.ConverterTypeId);
		}

		void DisassembleRecord(BamlContext ctx, PropertyCustomRecord record) {
			WriteRecordField("AttributeId");
			WriteOperator("=");
			WriteAttributeId(ctx, record.AttributeId);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("IsValueTypeId");
			WriteOperator("=");
			WriteBool(record.IsValueTypeId);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("SerializerType");
			WriteOperator("=");
			WriteFlagField(((KnownTypes)record.SerializerTypeId).ToString());

			WriteComma(spaceAfterComma: true);
			WriteRecordField("Data");
			WriteOperator("=");
			for (int i = 0; i < record.Data.Length; i++)
				output.Write(record.Data[i].ToString("x2"), BoxedTextColor.Number);
		}

		void DisassembleRecord(BamlContext ctx, DefAttributeRecord record) {
			WriteRecordField("Value");
			WriteOperator("=");
			WriteString(record.Value);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("NameId");
			WriteOperator("=");
			WriteStringId(ctx, record.NameId);
		}

		void DisassembleRecord(BamlContext ctx, DefAttributeKeyStringRecord record) {
			WriteRecordField("ValueId");
			WriteOperator("=");
			WriteStringId(ctx, record.ValueId);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("Shared");
			WriteOperator("=");
			WriteBool(record.Shared);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("SharedSet");
			WriteOperator("=");
			WriteBool(record.SharedSet);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("Record");
			WriteOperator("=");
			WriteRecordName(record.Record);
		}

		void DisassembleRecord(BamlContext ctx, TypeInfoRecord record) {
			WriteRecordField("TypeId");
			WriteOperator("=");
			WriteTypeId(ctx, record.TypeId, isDefinition: true);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("AssemblyId");
			WriteOperator("=");
			WriteAssemblyId(ctx, record.AssemblyId);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("TypeFullName");
			WriteOperator("=");
			WriteIdentifierNameString(record.TypeFullName, true);
		}

		void DisassembleRecord(BamlContext ctx, TypeSerializerInfoRecord record) {
			DisassembleRecord(ctx, (TypeInfoRecord)record);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("SerializerTypeId");
			WriteOperator("=");
			WriteTypeId(ctx, record.SerializerTypeId);
		}

		void DisassembleRecord(BamlContext ctx, AttributeInfoRecord record) {
			WriteRecordField("AttributeId");
			WriteOperator("=");
			WriteAttributeId(ctx, record.AttributeId, isDefinition: true);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("OwnerTypeId");
			WriteOperator("=");
			WriteTypeId(ctx, record.OwnerTypeId);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("AttributeUsage");
			WriteOperator("=");
			switch (record.AttributeUsage) {
			case BamlAttributeUsage.Default:
				WriteFlagField("Default");
				break;
			case BamlAttributeUsage.XmlLang:
				WriteFlagField("XmlLang");
				break;
			case BamlAttributeUsage.XmlSpace:
				WriteFlagField("XmlSpace");
				break;
			case BamlAttributeUsage.RuntimeName:
				WriteFlagField("XmlSpace");
				break;
			default:
				WriteHexNumber((byte)record.AttributeUsage);
				break;
			}

			WriteComma(spaceAfterComma: true);
			WriteRecordField("Name");
			WriteOperator("=");
			WriteIdentifierNameString(record.Name);
		}

		void DisassembleRecord(BamlContext ctx, StringInfoRecord record) {
			WriteRecordField("StringId");
			WriteOperator("=");
			WriteStringId(ctx, record.StringId, isDefinition: true);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("Value");
			WriteOperator("=");
			WriteString(record.Value);
		}

		void DisassembleRecord(BamlContext ctx, TextRecord record) {
			WriteRecordField("Value");
			WriteOperator("=");
			WriteString(record.Value);
		}

		void DisassembleRecord(BamlContext ctx, TextWithConverterRecord record) {
			DisassembleRecord(ctx, (TextRecord)record);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("ConverterTypeId");
			WriteOperator("=");
			WriteTypeId(ctx, record.ConverterTypeId);
		}

		void DisassembleRecord(BamlContext ctx, TextWithIdRecord record) {
			WriteRecordField("ValueId");
			WriteOperator("=");
			WriteStringId(ctx, record.ValueId);
		}

		void DisassembleRecord(BamlContext ctx, LiteralContentRecord record) {
			WriteRecordField("Value");
			WriteOperator("=");
			WriteString(record.Value);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("Reserved0");
			WriteOperator("=");
			WriteHexNumber(record.Reserved0);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("Reserved1");
			WriteOperator("=");
			WriteHexNumber(record.Reserved1);
		}

		void DisassembleRecord(BamlContext ctx, RoutedEventRecord record) {
			WriteRecordField("Value");
			WriteOperator("=");
			WriteString(record.Value);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("AttributeId");
			WriteOperator("=");
			WriteAttributeId(ctx, record.AttributeId);
		}

		void DisassembleRecord(BamlContext ctx, DocumentStartRecord record) {
			WriteRecordField("LoadAsync");
			WriteOperator("=");
			WriteBool(record.LoadAsync);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("MaxAsyncRecords");
			WriteOperator("=");
			WriteHexNumber(record.MaxAsyncRecords);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("DebugBaml");
			WriteOperator("=");
			WriteBool(record.DebugBaml);
		}

		void DisassembleRecord(BamlContext ctx, ElementStartRecord record) {
			WriteRecordField("TypeId");
			WriteOperator("=");
			WriteTypeId(ctx, record.TypeId);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("Flags");
			WriteOperator("=");

			var flags = record.Flags;
			if (flags == 0)
				WriteHexNumber(0);
			else {
				bool first = true;
				if ((flags & ElementStartRecordFlags.CreateUsingTypeConverter) != 0) {
					WriteFlagField("CreateUsingTypeConverter");
					first = false;
					flags &= ~ElementStartRecordFlags.CreateUsingTypeConverter;
				}

				if ((flags & ElementStartRecordFlags.IsInjected) != 0) {
					if (!first)
						WriteOperator("|");
					WriteFlagField("IsInjected");
					first = false;
					flags &= ~ElementStartRecordFlags.IsInjected;
				}

				if (flags != 0) {
					if (!first)
						WriteOperator("|");
					WriteHexNumber((byte)flags);
				}
			}
		}

		void DisassembleRecord(BamlContext ctx, ConnectionIdRecord record) {
			WriteRecordField("ConnectionId");
			WriteOperator("=");
			WriteHexNumber(record.ConnectionId);
		}

		void DisassembleRecord(BamlContext ctx, PropertyWithExtensionRecord record) {
			WriteRecordField("AttributeId");
			WriteOperator("=");
			WriteAttributeId(ctx, record.AttributeId);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("Flags");
			WriteOperator("=");
			WriteHexNumber(record.Flags);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("ValueId");
			WriteOperator("=");
			if (record.IsValueTypeExtension)
				WriteTypeId(ctx, record.ValueId);
			else if (record.IsValueStaticExtension) {
				if (record.ValueId > 0x7fff)
					WriteResourceId(ctx, record.ValueId);
				else
					WriteAttributeId(ctx, record.ValueId);
			}
			else
				WriteHexNumber(record.ValueId);
		}

		void DisassembleRecord(BamlContext ctx, PropertyTypeReferenceRecord record) {
			DisassembleRecord(ctx, (PropertyComplexStartRecord)record);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("TypeId");
			WriteOperator("=");
			WriteTypeId(ctx, record.TypeId);
		}

		void DisassembleRecord(BamlContext ctx, PropertyStringReferenceRecord record) {
			DisassembleRecord(ctx, (PropertyComplexStartRecord)record);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("StringId");
			WriteOperator("=");
			WriteStringId(ctx, record.StringId);
		}

		void DisassembleRecord(BamlContext ctx, PropertyWithStaticResourceIdRecord record) {
			WriteRecordField("AttributeId");
			WriteOperator("=");
			WriteAttributeId(ctx, record.AttributeId);
			WriteComma(spaceAfterComma: true);

			DisassembleRecord(ctx, (StaticResourceIdRecord)record);
		}

		void DisassembleRecord(BamlContext ctx, ContentPropertyRecord record) {
			WriteRecordField("AttributeId");
			WriteOperator("=");
			WriteAttributeId(ctx, record.AttributeId);
		}

		void DisassembleRecord(BamlContext ctx, DefAttributeKeyTypeRecord record) {
			DisassembleRecord(ctx, (ElementStartRecord)record);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("Shared");
			WriteOperator("=");
			WriteBool(record.Shared);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("SharedSet");
			WriteOperator("=");
			WriteBool(record.SharedSet);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("Record");
			WriteOperator("=");
			WriteRecordName(record.Record);
		}

		void DisassembleRecord(BamlContext ctx, PropertyComplexStartRecord record) {
			WriteRecordField("AttributeId");
			WriteOperator("=");
			WriteAttributeId(ctx, record.AttributeId);
		}

		void DisassembleRecord(BamlContext ctx, ConstructorParameterTypeRecord record) {
			WriteRecordField("TypeId");
			WriteOperator("=");
			WriteTypeId(ctx, record.TypeId);
		}

		void DisassembleRecord(BamlContext ctx, DeferableContentStartRecord record) {
			WriteRecordField("Record");
			WriteOperator("=");
			WriteRecordName(record.Record);
		}

		void DisassembleRecord(BamlContext ctx, StaticResourceIdRecord record) {
			WriteRecordField("StaticResourceId");
			WriteOperator("=");
			WriteHexNumber(record.StaticResourceId);
		}

		void DisassembleRecord(BamlContext ctx, OptimizedStaticResourceRecord record) {
			WriteRecordField("Flags");
			WriteOperator("=");
			WriteHexNumber(record.Flags);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("ValueId");
			WriteOperator("=");
			if (record.IsValueTypeExtension)
				WriteTypeId(ctx, record.ValueId);
			else if (record.IsValueStaticExtension) {
				if (record.ValueId > 0x7fff)
					WriteResourceId(ctx, record.ValueId);
				else
					WriteAttributeId(ctx, record.ValueId);
			}
			else
				WriteStringId(ctx, record.ValueId);
		}

		void DisassembleRecord(BamlContext ctx, LineNumberAndPositionRecord record) {
			WriteRecordField("LineNumber");
			WriteOperator("=");
			WriteHexNumber(record.LineNumber);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("LinePosition");
			WriteOperator("=");
			WriteHexNumber(record.LinePosition);
		}

		void DisassembleRecord(BamlContext ctx, LinePositionRecord record) {
			WriteRecordField("LinePosition");
			WriteOperator("=");
			WriteHexNumber(record.LinePosition);
		}

		void DisassembleRecord(BamlContext ctx, NamedElementStartRecord record) {
			DisassembleRecord(ctx, (ElementStartRecord)record);

			WriteComma(spaceAfterComma: true);
			WriteRecordField("RuntimeName");
			WriteOperator("=");
			WriteString(record.RuntimeName);
		}

		#endregion
	}
}
