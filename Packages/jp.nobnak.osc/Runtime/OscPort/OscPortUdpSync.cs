using System.Net.Sockets;
using System;
using System.Net;
using Osc;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using System.Threading;

namespace Osc {
	public class OscPortUdpSync : OscPort {
		UdpClient _udp;
		Thread _reader;

		#region private
		protected override void OnEnable() {
			base.OnEnable ();
			try {
				_udp = new UdpClient (config.localPort, AddressFamily.InterNetwork);
				_reader = new Thread(Reader);
				_reader.Start();
			} catch (System.Exception e) {
				RaiseError (e);
			}
		}
		protected override void OnDisable() {
			if (_udp != null) {
				_udp.Close();
				_udp = null;
			}
			if (_reader != null) {
				_reader.Interrupt ();
				_reader = null;
			}
			base.OnDisable ();
		}

		protected override void SendImpl(byte[] oscData, IPEndPoint remote) {
			try {
				_udp.Send (oscData, oscData.Length, remote);
			} catch (System.Exception e) {
				RaiseError (e);
			}
		}


		protected void Reader() {
			while (_udp != null) {
				try {
					var clientEndpoint = new IPEndPoint(IPAddress.Any, 0);
					var receivedData = _udp.Receive(ref clientEndpoint);
					_oscParser.FeedData(receivedData, receivedData.Length);
					while (_oscParser.MessageCount > 0) {
						var msg = _oscParser.PopMessage();
						Receive(new Capsule(msg, clientEndpoint));
					}
				} catch (ThreadInterruptedException e) {
#if UNITY_EDITOR
					Debug.LogFormat("Reader interrupted:\n{0}",e);
#endif
				} catch (SocketException e) {
					if (_udp != null && e.ErrorCode != E_CANCEL_BLOCKING_CALL)
						RaiseError(e);
				} catch (Exception e) {
					RaiseError (e);
				}
			}
		}
		#endregion
	}
}