using Osc2;
using System.Net;
using UnityEngine;

public class OSCTester : MonoBehaviour {

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

        IPEndPoint remoteEndpoint = new IPEndPoint("locaohost".FindFromHostName(), 10000);
        using (var osc = new OscSender(remoteEndpoint)) {
            while (true) {
                if (osc != null) {
                    var msg = new Encoder("/test")
                        .Add(123)
                        .Add("hello")
                        .Add(3.14f);
                    osc.Send(msg.Encode());
                    yield return null;
                } else {
                    Debug.LogWarning("OSCTester.cs : SendWork : osc is null");
                    yield return new WaitForSeconds(1);
                }
            }
        }
    }
    #endregion

    #region listener
    public void OnReceive(Capsule capsule) {
        Debug.LogFormat("OSCTester.cs : OnReceive : {0}", capsule.message);
    }
    #endregion

    #region declarations
    #endregion
}