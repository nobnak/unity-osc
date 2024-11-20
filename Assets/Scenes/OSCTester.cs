using Osc;
using UnityEngine;

public class OSCTester : MonoBehaviour {

    public Presets presets = new();

    protected Coroutine worker;

    #region unity
    private void OnEnable() {
        worker = StartCoroutine(SendWork());
    }
    private void OnDisable() {
        if (worker != null) {
            StopCoroutine(worker);
            worker = null;
        }
    }
    #endregion

    #region worker
    private System.Collections.IEnumerator SendWork() {
        yield return null;

        var osc = presets.osc;
        while (true) {
            if (osc != null) {
                var msg = new MessageEncoder("/test");
                msg.Add(123);
                msg.Add("hello");
                msg.Add(3.14f);
                osc.Send(msg);
                yield return null;
            } else {
                Debug.LogWarning("OSCTester.cs : SendWork : osc is null");
                yield return new WaitForSeconds(1);
            }
        }
    }
    #endregion

    #region listener
    public void OnReceive(OscPort.Capsule capsule) {
        Debug.LogFormat("OSCTester.cs : OnReceive : {0}", capsule.message);
    }
    #endregion

    #region declarations
    [System.Serializable]
    public class Presets {
        public OscPort osc;
    }
    #endregion
}