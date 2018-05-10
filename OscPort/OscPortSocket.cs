using System.Net.Sockets;
using System;
using System.Net;
using Osc;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using System.Threading;
using UnityEngine.Profiling;

namespace Osc {
	public class OscPortSocket : OscPort {
		Socket _udp;
		byte[] _receiveBuffer;
		Thread _reader;
		Thread _sender;

		CustomSampler sampler;

		protected override void OnEnable() {
			try {
				base.OnEnable();

				_udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				_udp.Bind(new IPEndPoint(IPAddress.Any, localPort));

				_receiveBuffer = new byte[BUFFER_SIZE];

				_reader = new Thread(Reader);
				_sender = new Thread(Sender);
				sampler = CustomSampler.Create("Sampler");

				_reader.Start();
				_sender.Start();
			} catch (System.Exception e) {
				RaiseError (e);
				enabled = false;
			}
		}
		protected override void OnDisable() {
			if (_udp != null) {
                var u = _udp;
                _udp = null;
                u.Close ();
			}
			if (_reader != null) {
				_reader.Abort ();
				_reader = null;
			}
			if (_sender != null) {
				_sender.Abort();
				_sender = null;
			}

			base.OnDisable ();
		}
			
		void Reader() {
			var clientEndpoint = new IPEndPoint(IPAddress.Any, 0);
			while (_udp != null) {
				try {
					var fromendpoint = (EndPoint)clientEndpoint;
					var length = _udp.ReceiveFrom(_receiveBuffer, ref fromendpoint);
					var fromipendpoint = fromendpoint as IPEndPoint;
					if (length == 0 || fromipendpoint == null)
						continue;
					
					_oscParser.FeedData (_receiveBuffer, length);
					while (_oscParser.MessageCount > 0) {
						var msg = _oscParser.PopMessage();
						Receive(new Capsule(msg, clientEndpoint));
					}
				} catch (Exception e) {
                    if (_udp != null)
					    RaiseError (e);
				}
			}
		}
		void Sender() {
			Profiler.BeginThreadProfiling(typeof(OscPortSocket).Name, "Sender");

			while (_udp != null) {
				try {
					Thread.Sleep(0);
					if (_willBeSent.Count == 0)
						continue;

					sampler.Begin();
					lock (_willBeSent)
						while (_willBeSent.Count > 0)
							_willBeSent.Dequeue().Send(_udp);
					sampler.End();

				} catch(Exception e) {
					if (_udp != null)
						RaiseError(e);
				}
			}

			Profiler.EndThreadProfiling();
		}
	}
}