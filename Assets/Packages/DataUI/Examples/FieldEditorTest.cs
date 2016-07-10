using UnityEngine;
using System.Collections;

namespace DataUI {
	public class FieldEditorTest : MonoBehaviour {
		public Data data;

		Rect _window;
		FieldEditor _dataEditor;

		void Start() {
			_window = new Rect(10, 10, 300, 400);
			_dataEditor = new FieldEditor(data);
		}
		void OnGUI() {
            _window = GUILayout.Window(GetInstanceID(), _window, GUIWindow, "GUI");
		}
		void GUIWindow(int id) {
			GUILayout.BeginVertical();
			_dataEditor.OnGUI();
			if (GUILayout.Button ("Load"))
				_dataEditor.Load ();
			GUILayout.EndVertical();
            GUI.DragWindow ();
		}

    	[System.Serializable]
    	public class Data {
    		public enum TeamEnum { Alpha, Bravo, Charlie }

    		public int intData;
    		public float floatData;
    		public Vector4 v4Data;
    		public Vector2 v2Data;
    		public Vector3 v3Data;
            public Matrix4x4 mData;
    		public Color colorData;
    		public TeamEnum team;
            public string stringData;
			public int[] arrayData;
    	}
    }
}