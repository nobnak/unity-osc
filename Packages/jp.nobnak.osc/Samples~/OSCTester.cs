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
            while (true) {
                var msgSync = new Encoder("/sync")
                    .Add(1)
                    .Add("hello")
                    .Add(3.14f);
                var msgAsync = new Encoder("/async")
                    .Add(1)
                    .Add("hello")
                    .Add(3.14f);
                var dataSync = msgSync.Encode();
                var dataAsync = msgAsync.Encode();

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var batchCount = 0;
                var batch = 10000;
                if (counter % 2 == 0) {
                    while (batchCount < batch && sw.ElapsedMilliseconds < 100) {
                        sender.Send(dataSync);
                        batchCount++;
                    }
                    sw.Stop();
                    Debug.Log($"Send sync: {1e3 * sw.ElapsedMilliseconds / batchCount}ns");
                } else {
                    while (batchCount < batch && sw.ElapsedMilliseconds < 100) {
                        sender.SendAsync(dataAsync);
                        batchCount++;
                    }
                    sw.Stop();
                    Debug.Log($"Send async: {1e3 * sw.ElapsedMilliseconds / batchCount}ns");
                }

                yield return null;
                counter++;
            }
        }
    }
    private System.Collections.IEnumerator ReceiveOnlyWork() {
        yield return null;

        var port = presets.port;
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