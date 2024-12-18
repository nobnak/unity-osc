using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine.Profiling;

namespace Osc {
	public class OscPortSocket : OscPort {
		protected Socket _udp;
		protected byte[] _receiveBuffer;
		protected Thread _reader;
		protected Thread _sender;
		protected Queue<SendData> _willBeSent;

		protected CustomSampler sampler;

		#region unity
		protected override void OnEnable() {
			try {
				base.OnEnable();

				_udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				_receiveBuffer = new byte[BUFFER_SIZE];
				_willBeSent = new Queue<SendData>();

				_reader = new Thread(Reader);
				_sender = new Thread(Sender);
				sampler = CustomSampler.Create("Sampler");

				_udp.Bind(new IPEndPoint(IPAddress.Any, config.localPort));
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
				_reader.Interrupt ();
				_reader = null;
			}
			if (_sender != null) {
				_sender.Interrupt();
				_sender = null;
			}

			base.OnDisable ();
		}
		#endregion

		#region private
		protected override void SendImpl(byte[] oscData, IPEndPoint remote) {
			lock (_willBeSent)
				_willBeSent.Enqueue(new SendData(oscData, remote));
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

					_oscParser.FeedData(_receiveBuffer, length);
					while (_oscParser.MessageCount > 0) {
						var msg = _oscParser.PopMessage();
						Receive(new Capsule(msg, fromipendpoint));
					}
				} catch (ThreadInterruptedException e) {
#if UNITY_EDITOR
					UnityEngine.Debug.LogFormat("Reader thread interrupted:\n{0}",e);
#endif
				} catch (SocketException e) {
					if (_udp != null && e.ErrorCode != E_CANCEL_BLOCKING_CALL)
						RaiseError(e);
				} catch (Exception e) {
                    if (_udp != null)
					    RaiseError (e);
				}
			}
		}
		void Sender() {
			#if UNITY_2018_OR_NEWER
			Profiler.BeginThreadProfiling(typeof(OscPortSocket).Name, "Sender");
			#endif

			while (_udp != null) {
				try {
					Thread.Sleep(0);
					if (_willBeSent.Count == 0 || !_udp.IsBound)
						continue;
#if UNITY_2018_OR_NEWER
					sampler.Begin();
#endif
					lock (_willBeSent) {
						while (_willBeSent.Count > 0)
							_willBeSent.Dequeue().Send(_udp);
					}
#if UNITY_2018_OR_NEWER
					sampler.End();
#endif
				} catch (ThreadInterruptedException e) {
#if UNITY_EDITOR
					UnityEngine.Debug.LogFormat("Sender thread interrupted:\n{0}",e);
#endif
				} catch (SocketException e) {
					if (_udp != null && e.ErrorCode != E_CANCEL_BLOCKING_CALL)
						RaiseError(e);
				} catch(Exception e) {
					if (_udp != null)
						RaiseError(e);
				}
			}
#if UNITY_2018_OR_NEWER
			Profiler.EndThreadProfiling();
#endif
		}
		#endregion
	}
}