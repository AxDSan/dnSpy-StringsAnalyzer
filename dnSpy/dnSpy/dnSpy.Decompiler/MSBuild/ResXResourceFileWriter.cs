/*
    Copyright (C) 2023 ElektroKill

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
using System.Globalization;
using System.IO;
using System.Resources;
using System.Text;
using System.Xml;
using dnlib.DotNet.Resources;

namespace dnSpy.Decompiler.MSBuild {
	/// <summary>
	/// Implementation based on <see cref="ResXResourceWriter"/> and <see cref="ResXDataNode"/>
	/// </summary>
	sealed class ResXResourceFileWriter : IDisposable {
		readonly struct ResXResourceInfo {
			public readonly string ValueString;
			public readonly string? TypeName;
			public readonly string? MimeType;

			public ResXResourceInfo(string valueString, string? typeName, string? mimeType) {
				ValueString = valueString;
				TypeName = typeName;
				MimeType = mimeType;
			}

			public ResXResourceInfo(string valueString, string? typeName) {
				ValueString = valueString;
				TypeName = typeName;
				MimeType = null;
			}

			public ResXResourceInfo(string valueString) {
				ValueString = valueString;
				TypeName = null;
				MimeType = null;
			}
		}

		readonly Func<Type, string> typeNameConverter;
		readonly Func<string, bool> hasByteArrayConverter;
		readonly XmlTextWriter writer;
		bool written;

		public ResXResourceFileWriter(string fileName, Func<Type, string> typeNameConverter, Func<string, bool> hasByteArrayConverter) {
			this.typeNameConverter = typeNameConverter;
			this.hasByteArrayConverter = hasByteArrayConverter;
			writer = new XmlTextWriter(fileName, Encoding.UTF8) {
				Formatting = Formatting.Indented,
				Indentation = 2
			};
			InitializeWriter();
		}

		void InitializeWriter() {
			writer.WriteStartDocument();

			writer.WriteStartElement("root");

			var reader = new XmlTextReader(new StringReader(ResXResourceWriter.ResourceSchema)) {
				WhitespaceHandling = WhitespaceHandling.None
			};
			writer.WriteNode(reader, true);

			writer.WriteStartElement("resheader");
			writer.WriteAttributeString("name", "resmimetype");
			writer.WriteStartElement("value");
			writer.WriteString(ResXResourceWriter.ResMimeType);
			writer.WriteEndElement();
			writer.WriteEndElement();

			writer.WriteStartElement("resheader");
			writer.WriteAttributeString("name", "version");
			writer.WriteStartElement("value");
			writer.WriteString(ResXResourceWriter.Version);
			writer.WriteEndElement();
			writer.WriteEndElement();

			writer.WriteStartElement("resheader");
			writer.WriteAttributeString("name", "reader");
			writer.WriteStartElement("value");
			writer.WriteString(typeNameConverter(typeof(ResXResourceReader)));
			writer.WriteEndElement();
			writer.WriteEndElement();

			writer.WriteStartElement("resheader");
			writer.WriteAttributeString("name", "writer");
			writer.WriteStartElement("value");
			writer.WriteString(typeNameConverter(typeof(ResXResourceWriter)));
			writer.WriteEndElement();
			writer.WriteEndElement();
		}

		public void AddResourceData(ResourceElement resourceElement) {
			var nodeInfo = GetNodeInfo(resourceElement.ResourceData);

			writer.WriteStartElement("data");
			writer.WriteAttributeString("name", resourceElement.Name);

			if (nodeInfo.TypeName is not null)
				writer.WriteAttributeString("type", nodeInfo.TypeName);

			if (nodeInfo.MimeType is not null)
				writer.WriteAttributeString("mimetype", nodeInfo.MimeType);

			if (nodeInfo.TypeName is null && nodeInfo.MimeType is null || nodeInfo.TypeName is not null &&
			    nodeInfo.TypeName.StartsWith("System.Char", StringComparison.Ordinal))
				writer.WriteAttributeString("xml", "space", null, "preserve");

			writer.WriteStartElement("value");
			if (!string.IsNullOrEmpty(nodeInfo.ValueString))
				writer.WriteString(nodeInfo.ValueString);
			writer.WriteEndElement();

			writer.WriteEndElement();
		}

		ResXResourceInfo GetNodeInfo(IResourceData resourceData) {
			if (resourceData is BuiltInResourceData builtInResourceData) {
				// Mimic formatting used in ResXDataNode and TypeConverter implementations
				switch (builtInResourceData.Code) {
				case ResourceTypeCode.Null:
					return new ResXResourceInfo("", typeNameConverter(typeof(ResXDataNode).Assembly.GetType("System.Resources.ResXNullRef")!));
				case ResourceTypeCode.String:
					return new ResXResourceInfo((string)builtInResourceData.Data);
				case ResourceTypeCode.Boolean:
					return new ResXResourceInfo(((bool)builtInResourceData.Data).ToString(), typeNameConverter(typeof(bool)));
				case ResourceTypeCode.Char:
					var c = (char)builtInResourceData.Data;
					return new ResXResourceInfo(c == '\0' ? "" : c.ToString(), typeNameConverter(typeof(char)));
				case ResourceTypeCode.Byte:
				case ResourceTypeCode.SByte:
				case ResourceTypeCode.Int16:
				case ResourceTypeCode.UInt16:
				case ResourceTypeCode.Int32:
				case ResourceTypeCode.UInt32:
				case ResourceTypeCode.Int64:
				case ResourceTypeCode.UInt64:
				case ResourceTypeCode.Decimal: {
					var data = (IFormattable)builtInResourceData.Data;
					return new ResXResourceInfo(data.ToString("G", CultureInfo.InvariantCulture.NumberFormat), typeNameConverter(builtInResourceData.Data.GetType()));
				}
				case ResourceTypeCode.Single:
				case ResourceTypeCode.Double: {
					var data = (IFormattable)builtInResourceData.Data;
					return new ResXResourceInfo(data.ToString("R", CultureInfo.InvariantCulture.NumberFormat), typeNameConverter(builtInResourceData.Data.GetType()));
				}
				case ResourceTypeCode.TimeSpan:
					return new ResXResourceInfo(((IFormattable)builtInResourceData.Data).ToString()!, typeNameConverter(typeof(TimeSpan)));
				case ResourceTypeCode.DateTime:
					var dateTime = (DateTime)builtInResourceData.Data;
					string str;
					if (dateTime == DateTime.MinValue)
						str = string.Empty;
					else if (dateTime.TimeOfDay.TotalSeconds == 0.0)
						str = dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
					else
						str = dateTime.ToString(CultureInfo.InvariantCulture);
					return new ResXResourceInfo(str, typeNameConverter(typeof(DateTime)));
				case ResourceTypeCode.ByteArray:
					return new ResXResourceInfo(ToBase64WrappedString((byte[])builtInResourceData.Data), typeNameConverter(typeof(byte[])));
				case ResourceTypeCode.Stream:
					return new ResXResourceInfo(ToBase64WrappedString((byte[])builtInResourceData.Data), null, ResXResourceWriter.BinSerializedObjectMimeType);
				default:
					throw new ArgumentOutOfRangeException();
				}
			}
			if (resourceData is BinaryResourceData binaryResourceData) {
				var commaIndex = binaryResourceData.TypeName.IndexOf(',');
				string actualName = commaIndex == -1 ? binaryResourceData.TypeName : binaryResourceData.TypeName.Remove(commaIndex);
				// These 4 are common types which are known to have a byte array converter.
				// We check for them first to avoid the slow hasByteArrayConverter() call.
				string mimeType = actualName switch {
					"System.Drawing.Icon" => ResXResourceWriter.ByteArraySerializedObjectMimeType,
					"System.Drawing.Bitmap" => ResXResourceWriter.ByteArraySerializedObjectMimeType,
					"System.Drawing.Metafile" => ResXResourceWriter.ByteArraySerializedObjectMimeType,
					"System.Drawing.Image" => ResXResourceWriter.ByteArraySerializedObjectMimeType,
					_ => hasByteArrayConverter(binaryResourceData.TypeName) ? ResXResourceWriter.ByteArraySerializedObjectMimeType : ResXResourceWriter.BinSerializedObjectMimeType
				};
				return new ResXResourceInfo(ToBase64WrappedString(binaryResourceData.Data), binaryResourceData.TypeName, mimeType);
			}

			throw new ArgumentOutOfRangeException();
		}

		static string ToBase64WrappedString(byte[] data) {
			const int lineWrap = 80;
			const string crlf = "\r\n";
			const string prefix = "        ";
			string raw = Convert.ToBase64String(data);
			if (raw.Length > lineWrap) {
				var output = new StringBuilder(raw.Length + (raw.Length / lineWrap) * 3); // word wrap on lineWrap chars, \r\n
				int current = 0;
				for (; current < raw.Length - lineWrap; current += lineWrap) {
					output.Append(crlf);
					output.Append(prefix);
					output.Append(raw, current, lineWrap);
				}

				output.Append(crlf);
				output.Append(prefix);
				output.Append(raw, current, raw.Length - current);
				output.Append(crlf);
				return output.ToString();
			}

			return raw;
		}

		~ResXResourceFileWriter() => Dispose(false);

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing) {
			if (disposing)
				Close();
		}

		public void Close() {
			if (!written)
				Generate();

			writer.Close();
		}

		public void Generate() {
			if (written)
				throw new InvalidOperationException("The resource is already generated.");

			written = true;
			writer.WriteEndElement();
			writer.Flush();
		}
	}
}
