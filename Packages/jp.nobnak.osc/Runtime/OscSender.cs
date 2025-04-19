using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine.Profiling;

namespace Osc2 {
    public class OscSender : System.IDisposable {

        public event Action<Exception> Error;

        protected Socket _udp;

		public OscSender() {
			_udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		}

        #region IDisposable
        public void Dispose() {
			if (_udp != null) {
                _udp.Dispose();
                _udp = null;
            }
		}
        #endregion

        #region methods
        public void Send(byte[] oscData, IPEndPoint remote) {
			try {
				_udp.SendTo(oscData, remote);
            } catch (ThreadInterruptedException e) {
#if UNITY_EDITOR
				UnityEngine.Debug.LogFormat("Sender thread interrupted:\n{0}",e);
#endif
			} catch (SocketException e) {
				if (_udp != null && e.ErrorCode != E_CANCEL_BLOCKING_CALL)
					Error?.Invoke(e);
			} catch(Exception e) {
				if (_udp != null) Error?.Invoke(e);
            }
		}
        #endregion

        #region declarations
        public const int E_CANCEL_BLOCKING_CALL = unchecked((int)0x80004005);
		
		public struct SendData {
            public readonly byte[] oscData;
            public readonly IPEndPoint remote;

            public SendData(byte[] oscData, IPEndPoint remote) {
                this.oscData = oscData;
                this.remote = remote;
            }
            public int Send(Socket s) {
                return s.SendTo(oscData, remote);
            }
        }
        #endregion
    }
}
