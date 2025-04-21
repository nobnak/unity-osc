using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Osc2 {
    public class OscSender : System.IDisposable {

        public event System.Action<System.Exception> Error;

        protected IPEndPoint defaultRemote;
        protected Socket udp;

		public OscSender(IPEndPoint defaultRemote = null) {
            this.defaultRemote = defaultRemote;
            this.udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		}
        public OscSender(string host, int port) : this(new IPEndPoint(host.FindFromHostName(), port)) { }

        #region IDisposable
        public void Dispose() {
			if (udp != null) {
                udp.Dispose();
                udp = null;
            }
		}
        #endregion

        #region methods
        public void Send(byte[] oscData, IPEndPoint remote) {
            if (udp == null) return;
            try {
				udp.SendTo(oscData, remote);
            } catch (ThreadInterruptedException e) {
#if UNITY_EDITOR
				Debug.LogFormat("Sender thread interrupted:\n{0}",e);
#endif
			} catch (SocketException e) {
				if (e.ErrorCode != E_CANCEL_BLOCKING_CALL)
                    LogError(e);
			} catch(System.Exception e) {
                LogError(e);
            }
		}
        public void Send(byte[] oscData) {
            if (defaultRemote == null) {
                throw new System.InvalidOperationException("defaultRemote is null");
            }
            Send(oscData, defaultRemote);
        }

        public async Task SendAsync(byte[] oscData, IPEndPoint remote) {
            if (udp == null) return;
            try {
                await new SendAwaitable(udp, oscData, remote);
            } catch (ThreadInterruptedException e) {
#if UNITY_EDITOR
                Debug.LogFormat("Sender thread interrupted:\n{0}", e);
#endif
            } catch (SocketException e) {
                if (e.ErrorCode != E_CANCEL_BLOCKING_CALL)
                    LogError(e);
            } catch (System.Exception e) {
                LogError(e);
            }
        }
        public async Task SendAsync(byte[] oscData) {
            if (defaultRemote == null) {
                throw new System.InvalidOperationException("defaultRemote is null");
            }
            await SendAsync(oscData, defaultRemote);
        }

        public OscSender LogError(System.Exception e) {
            if (Error != null) Error(e); else Debug.LogError(e);
            return this;
        }
        #endregion

        #region declarations
        public const int E_CANCEL_BLOCKING_CALL = unchecked((int)0x80004005);

        public struct SendAwaitable {
            private Socket sender;
            private byte[] data;
            private IPEndPoint remote;

            public SendAwaitable(Socket sender, byte[] data, IPEndPoint remote) {
                this.sender = sender;
                this.data = data;
                this.remote = remote;
            }
            public SendAwaiter GetAwaiter() {
                return new SendAwaiter(sender, data, remote);
            }
        }
        public class SendAwaiter : INotifyCompletion {
            public readonly SocketAsyncEventArgs args;

            protected System.Action continuation;

            public SendAwaiter(Socket sender, byte[] data, IPEndPoint remote) {
                args = new SocketAsyncEventArgs();
                args.SetBuffer(data, 0, data.Length);
                args.RemoteEndPoint = remote;
                args.Completed += (s, e) => {
                    if (e.SocketError != SocketError.Success) {
                        new SocketException((int)e.SocketError);
                    }

                    IsCompleted = true;
                    continuation?.Invoke();
                };
                IsCompleted = !sender.SendToAsync(args);
            }

            public bool IsCompleted { get; protected set; }
            public void OnCompleted(System.Action continuation) {
                this.continuation = continuation;
            }
            public void GetResult() { }
        }
        #endregion
    }
}
