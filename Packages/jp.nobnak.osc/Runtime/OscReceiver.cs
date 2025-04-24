using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Osc2 {
	public class OscReceiver : OscSocket {

        public event Action<Capsule> Receive;

		protected byte[] receiveBuffer;
		protected Thread reader;
        protected Parser oscParser;
        protected IPEndPoint local;

        public OscReceiver(int localPort) {
            oscParser = new Parser();
            receiveBuffer = new byte[MTU_SIZE];
            local = new IPEndPoint(IPAddress.Any, localPort);

            udp.Bind(local);

            reader = new Thread(() => Reader());
            reader.IsBackground = true;
            reader.Start();

            void Reader() {
                var clientEndpoint = new IPEndPoint(IPAddress.Any, 0);
                while (udp != null) {
                    try {
                        if (!udp.Poll(100, SelectMode.SelectRead)) {
                            continue;
                        }
                        var length = udp.Available;
                        if (length <= 0)
                            continue;

                        if (receiveBuffer.Length < length)
                            receiveBuffer = new byte[length];

                        var fromendpoint = (EndPoint)clientEndpoint;
                        length = udp.ReceiveFrom(receiveBuffer, ref fromendpoint);
                        var remote = fromendpoint as IPEndPoint;
                        if (length <= 0 || remote == null)
                            continue;

                        oscParser.FeedData(receiveBuffer.AsSpan(0, length));
                        while (oscParser.MessageCount > 0) {
                            var msg = oscParser.PopMessage();
                            Receive?.Invoke(new Capsule(msg, local, remote));
                        }
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

        #region IDisposable
        public override void Dispose() {
            base.Dispose();
            if (reader != null) {
                reader.Abort();
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
        #endregion
    }
}