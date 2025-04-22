using System.Net.Sockets;
using UnityEngine;

namespace Osc2 {

    public class OscSocket : System.IDisposable {

        protected Socket udp;

        public event System.Action<System.Exception> Error;

        public OscSocket() 
            : this(new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)) { }
        public OscSocket(Socket udp) {
            this.udp = udp;
        }

        #region IDisposable
        public virtual void Dispose() {
            if (udp != null) {
                var u = udp;
                udp = null;
                u.Close();
            }
        }
#endregion

        public void LogError(System.Exception e) {
            if (Error != null) Error(e); else Debug.LogError(e);
        }

        #region declarations
        public const int E_CANCEL_BLOCKING_CALL = unchecked((int)0x80004005);
        public const int MTU_SIZE = 1500;
        #endregion
    }
}