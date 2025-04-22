using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Osc2 {
    public class OscSender : OscSocket, System.IDisposable {
        protected IPEndPoint defaultRemote;

        public OscSender(IPEndPoint defaultRemote = null) {
            this.defaultRemote = defaultRemote;
		}
        public OscSender(string host, int port) : this(new IPEndPoint(host.FindFromHostName(), port)) { }

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

        #endregion

        #region declarations
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
