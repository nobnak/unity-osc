using Osc2;
using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.Profiling;

public class OSCTester : MonoBehaviour {

    public Presets presets = new();

    protected Coroutine worker;

    #region unity
    private void OnEnable() {
        System.Func<IEnumerator> action = presets.mode switch {
            TestMode.Sender => SendOnlyWork,
            TestMode.Receiver => ReceiveOnlyWork,
            _ => null,
        };
        worker = StartCoroutine(action());
    }
    private void OnDisable() {
        if (worker != null) {
            StopCoroutine(worker);
            worker = null;
        }
    }
    #endregion

    #region worker
    private System.Collections.IEnumerator SendOnlyWork() {
        yield return null;

        var host = presets.host.FindFromHostName();
        var port = presets.port;
        IPEndPoint remoteEndpoint = new IPEndPoint(host, port);
        using (var sender = new OscSender(remoteEndpoint)) {
            var counter = 0;
            var batch = 1000;
            while (true) {
                var msg = new Encoder("/test")
                    .Add(counter++)
                    .Add("hello")
                    .Add(Time.time);
                var data = msg.Encode();

                if (counter % 2 == 0) {
                    Profiler.BeginSample("SendWork async");
                    for (var i = 0; i < batch; i++)
                        sender.SendAsync(data);
                    Profiler.EndSample();
                } else {
                    Profiler.BeginSample("SendWork sync");
                    for (var i = 0; i < batch; i++)
                        sender.Send(data);
                    Profiler.EndSample();
                }

                yield return null;
            }
        }
    }
    private System.Collections.IEnumerator ReceiveOnlyWork() {
        yield return null;

        var host = presets.host.FindFromHostName();
        var port = presets.port;
        IPEndPoint remoteEndpoint = new IPEndPoint(host, port);
        using (var receiver = new OscReceiver(port)) {
            receiver.Receive += OnReceive;

            while (true) {
                yield return null;
            }
        }
    }
    #endregion

    #region listener
    public void OnReceive(Capsule capsule) {
        Debug.Log($"OSCTester.cs : OnReceive : {capsule.message}");
    }
    #endregion

    #region declarations

    public enum TestMode {
        Sender,
        Receiver
    }
    [System.Serializable]
    public class Presets {
        public TestMode mode = TestMode.Sender;
        public string host = "localhost";
        public int port = 10000;
    }
    #endregion
}