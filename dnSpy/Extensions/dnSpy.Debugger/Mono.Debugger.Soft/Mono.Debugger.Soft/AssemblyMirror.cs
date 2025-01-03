using System;
using System.Reflection;
using System.Collections.Generic;

namespace Mono.Debugger.Soft
{
	public class AssemblyMirror : Mirror
	{
		string location;
		MethodMirror entry_point;
		bool entry_point_set;
		ModuleMirror main_module;
		AssemblyName aname;
		AppDomainMirror domain;
		byte[] metadata_blob;
		bool? isDynamic;
		byte[] pdb_blob;
		bool? has_debug_info;
		Dictionary<string, long> typeCacheIgnoreCase = new Dictionary<string, long> (StringComparer.InvariantCultureIgnoreCase);
		Dictionary<string, long> typeCache = new Dictionary<string, long> (StringComparer.Ordinal);
		Dictionary<uint, long> tokenTypeCache = new Dictionary<uint, long> ();
		Dictionary<uint, long> tokenMethodCache = new Dictionary<uint, long> ();
		CustomAttributeDataMirror[] cattrs;

		internal AssemblyMirror (VirtualMachine vm, long id) : base (vm, id) {
		}

		public string Location {
			get {
				if (location == null)
					location = vm.conn.Assembly_GetLocation (id);
				return location;
			}
	    }

		public MethodMirror EntryPoint {
			get {
				if (!entry_point_set) {
					long mid = vm.conn.Assembly_GetEntryPoint (id);

					if (mid != 0)
						entry_point = vm.GetMethod (mid);
					entry_point_set = true;
				}
				return entry_point;
			}
	    }

		public ModuleMirror ManifestModule {
			get {
				if (main_module == null) {
					main_module = vm.GetModule (vm.conn.Assembly_GetManifestModule (id));
				}
				return main_module;
			}
		}

		public AppDomainMirror Domain {
			get {
				if (domain == null) {
					if (vm.Version.AtLeast (2, 45))
						domain = vm.GetDomain (vm.conn.Assembly_GetIdDomain (id));
					else
						domain = GetAssemblyObject ().Domain;
				}
				return domain;
			}
		}

		public virtual AssemblyName GetName () {
			if (aname == null) {
				string name = vm.conn.Assembly_GetName (id);
				aname = new AssemblyName (name);
			}
			return aname;
		}

		public ObjectMirror GetAssemblyObject () {
			return vm.GetObject (vm.conn.Assembly_GetObject (id));
		}

		public TypeMirror GetType (string name, bool throwOnError, bool ignoreCase)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			if (name.Length == 0)
				throw new ArgumentException ("name", "Name cannot be empty");

			if (throwOnError)
				throw new NotImplementedException ();
			long typeId;
			if (ignoreCase) {
				if (!typeCacheIgnoreCase.TryGetValue (name, out typeId)) {
					typeId = vm.conn.Assembly_GetType (id, name, ignoreCase);
					typeCacheIgnoreCase.Add (name, typeId);
					var type = vm.GetType (typeId);
					if (type != null) {
						typeCache.Add (type.FullName, typeId);
					}
					return type;
				}
			} else {
				if (!typeCache.TryGetValue (name, out typeId)) {
					typeId = vm.conn.Assembly_GetType (id, name, ignoreCase);
					typeCache.Add (name, typeId);
				}
			}
			return vm.GetType (typeId);
		}

		public TypeMirror GetType (String name, Boolean throwOnError)
		{
			return GetType (name, throwOnError, false);
		}

		public TypeMirror GetType (String name) {
			return GetType (name, false, false);
		}

		public byte[] GetMetadataBlob () {
			if (metadata_blob != null)
				return metadata_blob;

			vm.CheckProtocolVersion (2, 47);

			return metadata_blob = vm.conn.Assembly_GetMetadataBlob (id);
		}

		public bool IsDynamic {
			get {
				if (isDynamic.HasValue)
					return isDynamic.Value;

				vm.CheckProtocolVersion (2, 47);

				isDynamic = vm.conn.Assembly_IsDynamic (id);
				return isDynamic.Value;
			}
		}

		public bool HasPdb {
			get {
				return pdb_blob != null;
			}
		}

		public bool HasFetchedPdb { get; private set; }

		public byte[] GetPdbBlob () {
			if (HasFetchedPdb)
				return pdb_blob;

			vm.CheckProtocolVersion (2, 47);
			var blob = vm.conn.Assembly_GetPdbBlob (id);
			if (blob != null && blob.Length > 0) {
				pdb_blob = blob;
			}
			HasFetchedPdb = true;
			return pdb_blob;
		}

		public TypeMirror GetType (uint token) {
			vm.CheckProtocolVersion (2, 47);
			if (IsDynamic)
				throw new NotSupportedException ();

			long typeId;
			if (!tokenTypeCache.TryGetValue (token, out typeId)) {
				typeId = vm.conn.Assembly_GetType (id, token);
				tokenTypeCache.Add (token, typeId);
			}
			return vm.GetType (typeId);
		}

		public MethodMirror GetMethod (uint token) {
			vm.CheckProtocolVersion (2, 47);
			if (IsDynamic)
				throw new NotSupportedException ();

			long methodId;
			if (!tokenMethodCache.TryGetValue (token, out methodId)) {
				methodId = vm.conn.Assembly_GetMethod (id, token);
				tokenMethodCache.Add (token, methodId);
			}
			return vm.GetMethod (methodId);
		}

		public bool HasDebugInfo {
			get {
				if (has_debug_info.HasValue)
					return has_debug_info.Value;

				vm.CheckProtocolVersion (2, 51);

				has_debug_info = vm.conn.Assembly_HasDebugInfo (id);
				return has_debug_info.Value;
			}
		}

		/* Since protocol version 2.58 */
		public CustomAttributeDataMirror[] GetCustomAttributes () {
			return GetCAttrs (null);
		}

		/* Since protocol version 2.58 */
		public CustomAttributeDataMirror[] GetCustomAttributes (TypeMirror attributeType) {
			if (attributeType == null)
				throw new ArgumentNullException ("attributeType");
			return GetCAttrs (attributeType);
		}

		CustomAttributeDataMirror[] GetCAttrs (TypeMirror type) {
			vm.CheckProtocolVersion (2, 58);

			if (cattrs == null) {
				CattrInfo[] info = vm.conn.Assembly_GetCustomAttributes (id, 0);
				cattrs = CustomAttributeDataMirror.Create (vm, info);
			}
			var res = new List<CustomAttributeDataMirror> ();
			foreach (var attr in cattrs)
				if (type == null || attr.Constructor.DeclaringType == type)
					res.Add (attr);
			return res.ToArray ();
		}
	}
}
