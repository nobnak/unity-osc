using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Osc2 {
    public class OscSender : OscSocket {

        protected IPEndPoint defaultRemote;
        protected Thread sender;
        protected BlockingCollection<SendData> sendBuffer;

        public OscSender(IPEndPoint defaultRemote = null) {
            this.defaultRemote = defaultRemote;
            sendBuffer = new();

            sender = new Thread(() => Sender());
            sender.IsBackground = true;
            sender.Start();

            void Sender() {
                while (udp != null) {
                    try { 
                        var s = sendBuffer.Take();
                        Send(s.data, s.remote);
                    } catch (SocketException e) {
                        if (e.ErrorCode != E_CANCEL_BLOCKING_CALL)
                            LogError(e);
                    } catch (Exception e) {
                        if (e is ThreadInterruptedException || e is ThreadAbortException) {
#if UNITY_EDITOR && DEVELOPMENT_BUILD
                            UnityEngine.Debug.Log($"Reader thread interrupted:\n{e}");
#endif
                        } else {
                            LogError(e);
                        }
                    }
                }
            }
        }
        public OscSender(string host, int port) : this(new IPEndPoint(host.FindFromHostName(), port)) { }

        #region IDisposable
        public override void Dispose() {
            if (sender != null) {
                sender.Interrupt();
                sender = null;
            }
            base.Dispose();
        }
        #endregion

        #region methods
        public void Send(byte[] oscData, IPEndPoint remote) {
            if (udp == null) return;
            try {
				udp.SendTo(oscData, remote);
			} catch (SocketException e) {
				if (e.ErrorCode != E_CANCEL_BLOCKING_CALL)
                    LogError(e);
            } catch (Exception e) {
                if (e is ThreadInterruptedException || e is ThreadAbortException) {
#if UNITY_EDITOR && DEVELOPMENT_BUILD
                            UnityEngine.Debug.Log($"Reader thread interrupted:\n{e}");
#endif
                } else {
                    LogError(e);
                }
            }
        }
        public void Send(byte[] oscData) {
            if (defaultRemote == null) {
                throw new System.InvalidOperationException("defaultRemote is null");
            }
            Send(oscData, defaultRemote);
        }

        public void SendAsync(byte[] oscData, IPEndPoint remote) {
            if (udp == null) return;
            sendBuffer.Add(new SendData(remote, oscData));
        }
        public void SendAsync(byte[] oscData) {
            if (defaultRemote == null) {
                throw new System.InvalidOperationException("defaultRemote is null");
            }
            SendAsync(oscData, defaultRemote);
        }

        #endregion

        #region declarations
        public struct SendData {
            public readonly IPEndPoint remote;
            public readonly byte[] data;
            public SendData(IPEndPoint remote, byte[] data) {
                this.remote = remote;
                this.data = data;
            }
        }
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
