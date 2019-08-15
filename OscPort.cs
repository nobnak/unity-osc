using nobnak.Gist.Profiling;
using nobnak.Gist.ThreadSafe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;

namespace Osc {
	public abstract class OscPort : MonoBehaviour {
		public enum ReceiveModeEnum { Event = 0, Poll }

		public const int BUFFER_SIZE = 1 << 16;
		public CapsuleEvent OnReceive;
		public ReceiveEventOnSpecifiedPath[] OnReceivesSpecified;
		public ExceptionEvent OnError;

		public ReceiveModeEnum receiveMode = ReceiveModeEnum.Event;
		public int localPort = 0;
		public string defaultRemoteHost = "localhost";
		public int defaultRemotePort = 10000;
		public int limitReceiveBuffer = 10;

		protected Parser _oscParser;
		protected Queue<Capsule> _received;
		protected Queue<System.Exception> _errors;
		protected IPEndPoint _defaultRemote;

		protected Queue<Capsule> tmpReceived;

		protected Frequency sendFrequency = new Frequency();
		protected Frequency recvFrequency = new Frequency();

		#region public
		public virtual IEnumerable<Capsule> PollReceived() {
			lock (_received) {
				while (_received.Count > 0)
					yield return _received.Dequeue ();
			}
		}
		public virtual IEnumerable<System.Exception> PollException() {
			lock (_errors) {
				while (_errors.Count > 0)
					yield return _errors.Dequeue ();
			}
		}

		public virtual void Send(MessageEncoder oscMessage) {
			Send (oscMessage, _defaultRemote);
		}
		public virtual void Send(MessageEncoder oscMessage, IPEndPoint remote) {
			Send (oscMessage.Encode (), remote);
		}
		public virtual void Send(byte[] oscData) {
			Send (oscData, _defaultRemote);
		}
		public void Send(byte[] oscData, IPEndPoint remote) {
			if (remote == null)
				return;
			sendFrequency.Increment();
			SendImpl(oscData, remote);
		}

		public virtual void UpdateDefaultRemote () {
			try {
				_defaultRemote = new IPEndPoint(FindFromHostName(defaultRemoteHost), defaultRemotePort);
			} catch {
				_defaultRemote = null;
			}
        }
		public virtual Diagnostics GetDiagnostics() {
			return new Diagnostics(
				sendFrequency.CurrentFrequency,
				recvFrequency.CurrentFrequency);
		}
        #endregion

        #region static
        public static IPAddress FindFromHostName(string hostname) {
            var address = IPAddress.None;
            try {
                if (IPAddress.TryParse(hostname, out address))
                    return address;

                var addresses = Dns.GetHostAddresses(hostname);
                for (var i = 0; i < addresses.Length; i++) {
                    if (addresses[i].AddressFamily == AddressFamily.InterNetwork) {
                        address = addresses[i];
                        break;
                    }
                }
            } catch (System.Exception e) {
                Debug.LogErrorFormat(
                    "Failed to find IP for :\n host name = {0}\n exception={1}",
                    hostname, e);
            }
            return address;
        }
        #endregion

        #region Unity
        protected virtual void Awake() {
#if UNITY_EDITOR
			StartCoroutine(Logger());
#endif
		}
		protected virtual void OnEnable() {
			_oscParser = new Parser ();
			_received = new Queue<Capsule> ();
			_errors = new Queue<Exception> ();
			tmpReceived = new Queue<Capsule>();
			UpdateDefaultRemote();
		}
		protected virtual void OnDisable() {
		}

		protected virtual void Update() {
			if (receiveMode == ReceiveModeEnum.Event) {
				lock (_received)
					while (_received.Count > 0)
						tmpReceived.Enqueue(_received.Dequeue());
				while(tmpReceived.Count > 0)
					NotifyReceived (tmpReceived.Dequeue ());

				lock (_errors)
					while (_errors.Count > 0)
						OnError.Invoke (_errors.Dequeue ());
			}
		}
#endregion

#region private
		protected abstract void SendImpl(byte[] oscData, IPEndPoint remote);
		protected virtual void RaiseError(System.Exception e) {
#if UNITY_EDITOR
			Debug.LogError(e);
#endif
			_errors.Enqueue (e);
		}
		protected virtual void Receive(OscPort.Capsule c) {
			recvFrequency.Increment();
			lock (_received) {
				if (limitReceiveBuffer <= 0 || _received.Count < limitReceiveBuffer)
					_received.Enqueue(c);
			}
		}

		protected virtual void NotifyReceived (Capsule c) {
			OnReceive.Invoke (c);
			foreach (var e in OnReceivesSpecified)
				if (e.TryToAccept (c.message))
					break;
		}
		protected virtual IEnumerator Logger() {
			while (true) {
				yield return new WaitForSeconds(60f);
				Debug.LogFormat("OSC Recv : freq={0} count={1}) Send : freq={2} count={3})",
					recvFrequency.CurrentFrequency,
					recvFrequency.CurrentCount,
					sendFrequency.CurrentFrequency,
					sendFrequency.CurrentCount);
			}
		}
#endregion

#region classes
		public struct Capsule {
			public Message message;
			public IPEndPoint ip;

			public Capsule(Message message, IPEndPoint ip) {
				this.message = message;
				this.ip = ip;
			}

			public override string ToString() {
				return string.Format("{0}, {1}", ip, message);
			}
		}
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
		[System.Serializable]
		public class ReceiveEventOnSpecifiedPath {
			public string path;
			public MessageEvent OnReceive;

			public bool TryToAccept(Message m) {
				if (m.path == path) {
					OnReceive.Invoke (m);
					return true;
				}
				return false;
			}
		}
#endregion
	}

	[System.Serializable]
	public class ExceptionEvent : UnityEvent<Exception> {}
	[System.Serializable]
	public class CapsuleEvent : UnityEvent<OscPort.Capsule> {}
	[System.Serializable]
	public class MessageEvent : UnityEvent<Message> {}

	public struct Diagnostics {
		public readonly float sendFrequency;
		public readonly float recvFrequency;

		public Diagnostics(float sendFrequency, float recvFrequency) {
			this.sendFrequency = sendFrequency;
			this.recvFrequency = recvFrequency;
		}

		public override string ToString() {
			return string.Format("<Diagnostics: frequencies (send={0:f1} recv={1:f1})>",
				sendFrequency, recvFrequency);
		}
	}
}