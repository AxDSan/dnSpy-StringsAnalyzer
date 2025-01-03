using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Mono.Debugger.Soft
{
	public class LaunchOptions {
		public string AgentArgs {
			get; set;
		}

		public bool Valgrind {
			get; set;
		}
		
		public ProcessLauncher CustomProcessLauncher {
			get; set;
		}

		public TargetProcessLauncher CustomTargetProcessLauncher {
			get; set;
		}

		public delegate Process ProcessLauncher (ProcessStartInfo info);
		public delegate ITargetProcess TargetProcessLauncher (ProcessStartInfo info);
	}

	public class VirtualMachineManager
	{
		internal VirtualMachineManager () {
		}

		static async Task<VirtualMachine> ConnectInternalAsync (Socket dbg_sock, Socket con_sock, IPEndPoint dbg_ep, IPEndPoint con_ep, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();
			if (con_sock != null) {
				try {
					await con_sock.ConnectAsync (con_ep).ConfigureAwait (false);
				} catch (Exception) {
					try {
						dbg_sock.Close ();
					} catch {}
					throw;
				}
			}
						
			cancellationToken.ThrowIfCancellationRequested();
			try {
				await dbg_sock.ConnectAsync (dbg_ep).ConfigureAwait (false);
			} catch (Exception) {
				if (con_sock != null) {
					try {
						con_sock.Close ();
					} catch {}
				}
				throw;
			}
			
			Connection transport = new TcpConnection (dbg_sock);
			StreamReader console = con_sock != null? new StreamReader (new NetworkStream (con_sock)) : null;
			
			cancellationToken.ThrowIfCancellationRequested();
			return Connect (transport, console, null);
		}

		public static Task<VirtualMachine> ConnectAsync (IPEndPoint dbg_ep, IPEndPoint con_ep, CancellationToken cancellationToken) {
			Socket dbg_sock = null;
			Socket con_sock = null;
			try {
				dbg_sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

				if (con_ep != null) {
					con_sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				}

				return ConnectInternalAsync(dbg_sock, con_sock, dbg_ep, con_ep, cancellationToken);
			}
			catch {
				dbg_sock?.Close ();
				con_sock?.Close ();
				throw;
			}
		}
		
		static VirtualMachine Connect (Connection transport, StreamReader standardOutput, StreamReader standardError)
		{
			VirtualMachine vm = new VirtualMachine (null, transport);
			
			vm.StandardOutput = standardOutput;
			vm.StandardError = standardError;
			
			transport.EventHandler = new EventHandler (vm);

			vm.connect ();

			return vm;
		}
	}
}
