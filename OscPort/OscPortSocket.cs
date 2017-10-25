using System.Net.Sockets;
using System;
using System.Net;
using Osc;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using System.Threading;

namespace Osc {
	public class OscPortSocket : OscPort {
		Socket _udp;
		byte[] _receiveBuffer;
		Thread _reader;

		protected override void OnEnable() {
			try {
				base.OnEnable();

				_udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				_udp.Bind(new IPEndPoint(IPAddress.Any, localPort));

				_receiveBuffer = new byte[BUFFER_SIZE];

				_reader = new Thread(Reader);
				_reader.Start();
			} catch (System.Exception e) {
				RaiseError (e);
				enabled = false;
			}
		}
		protected override void OnDisable() {
			if (_udp != null) {
				_udp.Close ();
				_udp = null;
			}
			if (_reader != null) {
				_reader.Abort ();
				_reader = null;
			}

			base.OnDisable ();
		}

		public override void Send(byte[] oscData, IPEndPoint remote) {
			try {
				_udp.SendTo(oscData, remote);
			} catch (System.Exception e) {
				RaiseError (e);
			}
		}
			
		void Reader() {
			while (_udp != null) {
				try {
					var clientEndpoint = new IPEndPoint (IPAddress.Any, 0);
					var fromendpoint = (EndPoint)clientEndpoint;
					var length = _udp.ReceiveFrom(_receiveBuffer, ref fromendpoint);
					if (length == 0 || (clientEndpoint = fromendpoint as IPEndPoint) == null)
						continue;
					
					_oscParser.FeedData (_receiveBuffer, length);
					while (_oscParser.MessageCount > 0) {
						lock (_received) {
							var msg = _oscParser.PopMessage ();
							Receive(new Capsule (msg, clientEndpoint));
						}
					}
				} catch (Exception e) {
					RaiseError (e);
				}
			}
		}
	}
}