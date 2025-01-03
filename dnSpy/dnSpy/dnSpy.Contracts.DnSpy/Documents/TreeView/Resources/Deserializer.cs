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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using dnlib.DotNet;

namespace dnSpy.Contracts.Documents.TreeView.Resources {
	sealed class DeserializedDataInfo {
		public Type ObjectType { get; }
		public string Name { get; }
		public object? Value { get; }

		public DeserializedDataInfo(Type objectType, string name, object? value) {
			ObjectType = objectType;
			Name = name;
			Value = value;
		}
	}

	/// <summary>
	/// Deserializes data without loading the deserialized types. Only our types get "deserialized".
	/// This is only useful if it's eg. an image and it's stored in a byte[].
	/// </summary>
	static class Deserializer {
		sealed class MyBinder : SerializationBinder {
			readonly ModuleDef module;
			readonly ITypeDefOrRef type;

			public MyBinder(string asmName, string typeName) {
				module = new ModuleDefUser();
				type = TypeNameParser.ParseReflection(module, $"{typeName}, {asmName}", null);
			}

			public override Type BindToType(string assemblyName, string typeName) {
				var otherType = TypeNameParser.ParseReflection(module, $"{typeName}, {assemblyName}", null);
				if (string.IsNullOrEmpty(type.TypeName) || string.IsNullOrEmpty(otherType.TypeName))
					return typeof(DontDeserializeType);
				if (!new SigComparer().Equals(type, otherType))
					return typeof(DontDeserializeType);
				var flags = AssemblyNameComparerFlags.All & ~AssemblyNameComparerFlags.Version;
				if (!new AssemblyNameComparer(flags).Equals(type.DefinitionAssembly, otherType.DefinitionAssembly))
					return typeof(DontDeserializeType);

				return typeof(DeserializedType);
			}
		}

		[Serializable]
		sealed class DeserializedType : ISerializable {
			public readonly Dictionary<string, DeserializedDataInfo> DeserializedDataInfos = new Dictionary<string, DeserializedDataInfo>(StringComparer.Ordinal);

			public DeserializedType(SerializationInfo info, StreamingContext context) {
				foreach (var c in info)
					DeserializedDataInfos[c.Name] = new DeserializedDataInfo(c.ObjectType, c.Name, c.Value);
			}

			public void GetObjectData(SerializationInfo info, StreamingContext context) => throw new NotImplementedException();
		}

		[Serializable]
		sealed class DontDeserializeType : ISerializable {
			public DontDeserializeType(SerializationInfo info, StreamingContext context) {
			}

			public void GetObjectData(SerializationInfo info, StreamingContext context) => throw new NotImplementedException();
		}

		public static Dictionary<string, DeserializedDataInfo> Deserialize(string asmName, string typeName, byte[] data) {
			var fmt = new BinaryFormatter();
			fmt.Binder = new MyBinder(asmName, typeName);
#pragma warning disable SYSLIB0011
			var obj = fmt.Deserialize(new MemoryStream(data)) as DeserializedType;
#pragma warning restore SYSLIB0011
			Debug2.Assert(obj is not null);
			if (obj is null)
				return new Dictionary<string, DeserializedDataInfo>();
			return obj.DeserializedDataInfos;
		}
	}
}
