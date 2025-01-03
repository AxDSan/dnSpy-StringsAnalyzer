using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Mono.Debugger.Soft
{
	public class MethodBodyMirror : Mirror
	{
		MethodMirror method;
		MethodBodyInfo info;

		internal MethodBodyMirror (VirtualMachine vm, MethodMirror method, MethodBodyInfo info) : base (vm, 0) {
			this.method = method;
			this.info = info;
		}

		public MethodMirror Method {
			get {
				return method;
			}
		}

		public List<ILExceptionHandler> ExceptionHandlers {
			get {
				vm.CheckProtocolVersion (2, 18);
				return info.clauses.Select (c =>
				{
					var handler = new ILExceptionHandler (c.try_offset, c.try_length, (ILExceptionHandlerType) c.flags, c.handler_offset, c.handler_length);
					if (c.flags == ExceptionClauseFlags.None)
						handler.CatchType = vm.GetType (c.catch_type_id);
					else if (c.flags == ExceptionClauseFlags.Filter)
						handler.FilterOffset = c.filter_offset;

					return handler;
				}).ToList ();
			}
		}

		public byte[] GetILAsByteArray () {
			return info.il;
		}

		Dictionary<int, ResolvedToken> tokensCache = new Dictionary<int, ResolvedToken> ();

		ResolvedToken ResolveToken (int token)
		{
			lock (tokensCache) {
				ResolvedToken resolvedToken;
				if (!tokensCache.TryGetValue (token, out resolvedToken)) {
					resolvedToken = vm.conn.Method_ResolveToken (Method.Id, token);
					tokensCache.Add (token, resolvedToken);
				}
				return resolvedToken;
			}
		}
	}
}
