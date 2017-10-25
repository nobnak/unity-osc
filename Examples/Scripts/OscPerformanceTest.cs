using UnityEngine;
using System.Collections;

namespace Osc {

	public class OscPerformanceTest : MonoBehaviour {
		public const string OSC_PATH = "/path";
		public Dataset[] datasets;
		public int packetsPerSec = 1000;

		public int index = 0;

		int _nsent = 0;
		int _nreceived = 0;

		void Start() {
			StartCoroutine (Stat ());
		}
		void Update () {
			index = Mathf.Clamp (index, 0, datasets.Length);
			var sender = datasets [index].sender;
			var packetsInFrame = Mathf.RoundToInt(Time.deltaTime * packetsPerSec);
			for (var i = 0; i < packetsInFrame; i++) {
				_nsent++;
				var oscdata = new MessageEncoder (OSC_PATH);
				oscdata.Add (_nsent);
				sender.Send (oscdata);
			}
		}
		
		public void OnReceive(OscPort.Capsule c) {
			_nreceived++;
		}
		public void OnError(System.Exception e) {
			Debug.LogFormat ("Exception {0}", e);
		}
		IEnumerator Stat() {
			while (true) {
				yield return new WaitForSeconds (1f);

				Debug.LogFormat ("Stat {0} throughput={1}% {2}/{3}", 
					datasets[index].name, 100f * _nreceived / _nsent, _nreceived, _nsent);
				_nsent = _nreceived = 0;
			}
		}

		[System.Serializable]
		public class Dataset {
			public string name;
			public OscPort sender;
		}

	}
}
