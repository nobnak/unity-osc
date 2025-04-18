using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Profiling;
using static Osc.OscPort;

namespace Osc {
	public class OscReceiver : System.IDisposable {

        public event Action<Capsule> Receive;
        public event Action<Exception> Error;

        protected Socket udp;
		protected byte[] receiveBuffer;
        protected CancellationTokenSource cancelSource;
		protected Thread reader;
        protected Parser oscParser;

        public OscReceiver(int localPort) {
            oscParser = new Parser();
            udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            receiveBuffer = new byte[ReceiveBufferSize];
            cancelSource = new CancellationTokenSource();

            udp.Bind(new IPEndPoint(IPAddress.Any, localPort));

            reader = new Thread(() => Reader(cancelSource.Token));
            reader.IsBackground = true;
            reader.Start();

            void Reader(CancellationToken token) {
                var clientEndpoint = new IPEndPoint(IPAddress.Any, 0);
                while (udp != null) {
                    try {
                        var fromendpoint = (EndPoint)clientEndpoint;
                        var length = udp.ReceiveFrom(receiveBuffer, ref fromendpoint);
                        var fromipendpoint = fromendpoint as IPEndPoint;
                        if (length == 0 || fromipendpoint == null)
                            continue;

                        oscParser.FeedData(receiveBuffer, length);
                        while (oscParser.MessageCount > 0) {
                            var msg = oscParser.PopMessage();
                            Receive?.Invoke(new Capsule(msg, fromipendpoint));
                        }
                    } catch (ThreadInterruptedException e) {
#if UNITY_EDITOR
                        UnityEngine.Debug.LogFormat("Reader thread interrupted:\n{0}", e);
#endif
                    } catch (SocketException e) {
                        if (udp != null && e.ErrorCode != E_CANCEL_BLOCKING_CALL)
                            Error?.Invoke(e);
                    } catch (Exception e) {
                        if (udp != null)
                            Error?.Invoke(e);
                    }
                }
            }
        }

        #region IDisposable
        public void Dispose() {
            if (udp != null) {
                var u = udp;
                udp = null;
                u.Close();
            }
            if (reader != null) {
                reader = null;
            }
        }
        #endregion

        #region methods
        public OscReceiver AddReceiver(Action<Capsule> action) {
            Receive += action;
            return this;
        }
        public OscReceiver RemoveReceiver(Action<Capsule> action) {
            Receive -= action;
            return this;
        }
        public OscReceiver AddError(Action<Exception> action) {
            Error += action;
            return this;
        }
        public OscReceiver RemoveError(Action<Exception> action) {
            Error -= action;
            return this;
        }
        public int ReceiveBufferSize {
			get => udp.ReceiveBufferSize;
			set {
				if (value != udp.ReceiveBufferSize) {
					udp.ReceiveBufferSize = value;
					receiveBuffer = new byte[value];
				}
            }
		}
        #endregion

        #region declarations
        public const int E_CANCEL_BLOCKING_CALL = unchecked((int)0x80004005);
        #endregion
    }
}