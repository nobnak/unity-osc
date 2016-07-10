using UnityEngine;
using System.Collections;

namespace DataUI {
    
	public class ReflectiveCopyTest : MonoBehaviour {
        public Data data0;
        public Data data1;

        ReflectiveCopy<Data, Data> _copy;

        void Start() {
            _copy = new ReflectiveCopy<Data, Data> (data0, data1);
        }
        void Update() {
            _copy.Sync ();
        }

    	[System.Serializable]
    	public class Data {
    		public enum TeamEnum { Alpha = 0, Bravo, Charlie }

    		public int intData;
    		public float floatData;
    		public string stringData;
    		public TeamEnum team;
    		public InnerClass innerClass;
    		public InnerStruct innerStruct;

    		[System.Serializable]
    		public class InnerClass {
    			public int innerIntData;
    		}

    		[System.Serializable]
    		public struct InnerStruct {
    			public int innerIntData;
    		}
    	}
    }
}