using nobnak.Gist.Profiling;
using nobnak.Gist.ThreadSafe;
using System;
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
			sendFrequency.Increment();
			SendImpl(oscData, remote);
		}

		public virtual IPAddress FindFromHostName(string hostname) {
			var addresses = Dns.GetHostAddresses (hostname);
			IPAddress address = IPAddress.None;
			for (var i = 0; i < addresses.Length; i++) {
				if (addresses[i].AddressFamily == AddressFamily.InterNetwork) {
					address = addresses[i];
					break;
				}
			}
			return address;
		}
		public virtual void UpdateDefaultRemote () {
            _defaultRemote = new IPEndPoint (FindFromHostName (defaultRemoteHost), defaultRemotePort);
        }
		public virtual Diagnostics GetDiagnostics() {
			return new Diagnostics(
				sendFrequency.CurrentFrequency,
				recvFrequency.CurrentFrequency);
		}
		#endregion

		#region Unity
		protected abstract void SendImpl(byte[] oscData, IPEndPoint remote);
		protected virtual void OnEnable() {
			_oscParser = new Parser ();
			_received = new Queue<Capsule> ();
			_errors = new Queue<Exception> ();
			UpdateDefaultRemote();
		}
		protected virtual void OnDisable() {
		}

		protected virtual void Update() {
			if (receiveMode == ReceiveModeEnum.Event) {
				lock (_received)
					while (_received.Count > 0)
						NotifyReceived (_received.Dequeue ());
				lock (_errors)
					while (_errors.Count > 0)
						OnError.Invoke (_errors.Dequeue ());
			}
		}
		#endregion

		#region private
		protected virtual void RaiseError(System.Exception e) {
            //Debug.LogError(e);
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