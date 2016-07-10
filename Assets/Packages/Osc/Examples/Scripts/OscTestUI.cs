using UnityEngine;
using System.Collections;
using DataUI;

namespace Osc {
	
	public class OscTestUI : MonoBehaviour {
		public const string OSC_PATH = "/data";

		public OscPort server;
		public OscPort client;

		public Data serverData;
		public Data clientData;

		FieldEditor _serverField;
		FieldEditor _clientField;
		Rect _window = new Rect(10, 10, 300, 200);

		void Start() {
			_serverField = new FieldEditor (serverData);
			_clientField = new FieldEditor (clientData);
		}
		void OnGUI() {
			_window = GUILayout.Window (GetInstanceID(), _window, Window, "UI");
		}

		public void OnServerReceive(OscPort.Capsule c) {
			Debug.LogFormat ("On Server Receive");
			if (c.message.path == OSC_PATH) {
				JsonUtility.FromJsonOverwrite((string)c.message.data [0], serverData);
				_serverField.Load ();
			}
		}
		public void OnClientReceive(OscPort.Capsule c) {
			Debug.LogFormat ("On Client Receive");
			if (c.message.path == OSC_PATH) {
				JsonUtility.FromJsonOverwrite((string)c.message.data [0], clientData);
				_clientField.Load ();
			}
		}
		public void OnError(System.Exception e) {
			Debug.LogFormat ("Exception {0}", e);
		}

		void Window(int id) {
			GUILayout.BeginHorizontal ();

			GUILayout.BeginVertical ();
			GUILayout.Label ("Server");
			_serverField.OnGUI ();
			if (GUILayout.Button ("Send")) {
				var osc = new MessageEncoder (OSC_PATH);
				osc.Add (JsonUtility.ToJson (serverData));
				server.Send (osc);
			}
			if (GUILayout.Button ("Poll")) {
				foreach (var r in server.PollReceived())
					OnServerReceive (r);				
			}
			GUILayout.EndVertical ();

			GUILayout.BeginVertical ();
			GUILayout.Label ("Client");
			_clientField.OnGUI ();
			if (GUILayout.Button ("Send")) {
				var osc = new MessageEncoder (OSC_PATH);
				osc.Add (JsonUtility.ToJson (clientData));
				client.Send (osc);
			}
			if (GUILayout.Button ("Poll")) {
				foreach (var r in client.PollReceived())
					OnClientReceive (r);
			}
			GUILayout.EndVertical ();

			GUILayout.EndHorizontal ();
			GUI.DragWindow ();
		}

		[System.Serializable]
		public class Data {
			public string text;
			public int number;
		}
	}
}