using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;

namespace Osc {
	public abstract class OscPort : MonoBehaviour {

		public Events events = new();
		public Config config = new();

		protected Parser _oscParser;
		protected Queue<Capsule> _received;
		protected Queue<System.Exception> _errors;
		protected IPEndPoint _defaultRemote;

		protected Queue<Capsule> tmpReceived;

		#region public
		public virtual IEnumerable<Capsule> PollReceived() {
			lock (_received) {
				while (_received.Count > 0)
					yield return _received.Dequeue();
			}
		}
		public virtual IEnumerable<System.Exception> PollException() {
			lock (_errors) {
				while (_errors.Count > 0)
					yield return _errors.Dequeue();
			}
		}

		public virtual void Send(MessageEncoder oscMessage) {
			Send(oscMessage, _defaultRemote);
		}
		public virtual void Send(MessageEncoder oscMessage, IPEndPoint remote) {
			Send(oscMessage.Encode(), remote);
		}
		public virtual void Send(byte[] oscData) {
			Send(oscData, _defaultRemote);
		}
		public void Send(byte[] oscData, IPEndPoint remote) {
			if (remote == null)
				return;
			SendImpl(oscData, remote);
		}

		public virtual void UpdateDefaultRemote() {
			try {
				_defaultRemote = new IPEndPoint(FindFromHostName(config.defaultRemoteHost), config.defaultRemotePort);
				if (_defaultRemote != null)
					Debug.LogFormat("OscPort.cs : default remote set to: {0}, {1}", _defaultRemote.Address, _defaultRemote.Port);
			} catch {
				_defaultRemote = null;
			}
		}
		public virtual Diagnostics GetDiagnostics() {
			return new Diagnostics(
				0f, 0f
			);
		}

		public IPAddress GetDefaultRemoteIP() {
			return _defaultRemote.Address;
		}

		public int GetDefaultRemotePort() {
			return _defaultRemote.Port;
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
		}
		protected virtual void OnEnable() {
			_oscParser = new Parser();
			_received = new Queue<Capsule>();
			_errors = new Queue<Exception>();
			tmpReceived = new Queue<Capsule>();
			UpdateDefaultRemote();
		}
		protected virtual void OnDisable() {
		}

		protected virtual void Update() {
			if (config.receiveMode == ReceiveModeEnum.Event) {
				lock (_received)
					while (_received.Count > 0)
						tmpReceived.Enqueue(_received.Dequeue());
				while (tmpReceived.Count > 0)
					NotifyReceived(tmpReceived.Dequeue());

				lock (_errors)
					while (_errors.Count > 0)
						events.OnError.Invoke(_errors.Dequeue());
			}
		}
		#endregion

		#region private
		protected abstract void SendImpl(byte[] oscData, IPEndPoint remote);
		protected virtual void RaiseError(System.Exception e) {
#if UNITY_EDITOR
			Debug.LogError(e);
#endif
			_errors.Enqueue(e);
		}
		protected virtual void Receive(OscPort.Capsule c) {
			lock (_received) {
				if (config.limitReceiveBuffer <= 0 || _received.Count < config.limitReceiveBuffer)
					_received.Enqueue(c);
			}
		}

		protected virtual void NotifyReceived(Capsule c) {
			events.OnReceive.Invoke(c);
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
		#endregion

		#region declarations
		public const int E_CANCEL_BLOCKING_CALL = unchecked((int)0x80004005);
		public enum ReceiveModeEnum { Event = 0, Poll }

		public const int BUFFER_SIZE = 1 << 16;

		[System.Serializable]
		public class Events {
			public CapsuleEvent OnReceive = new();
			public ExceptionEvent OnError = new();

			[System.Serializable]
			public class ExceptionEvent : UnityEvent<Exception> { }
			[System.Serializable]
			public class CapsuleEvent : UnityEvent<OscPort.Capsule> { }
			[System.Serializable]
			public class MessageEvent : UnityEvent<Message> { }
        }

        [System.Serializable]
        public class Config {
            public ReceiveModeEnum receiveMode = ReceiveModeEnum.Event;
            public int localPort = 0;
            public string defaultRemoteHost = "localhost";
            public int defaultRemotePort = 10000;
            public int limitReceiveBuffer = 10;
        }

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
		#endregion
	}
}