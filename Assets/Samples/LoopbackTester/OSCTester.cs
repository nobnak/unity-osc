using Osc2;
using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.Profiling;
using System.Threading;

public class OSCTester : MonoBehaviour {

    public Presets presets = new();

    protected Coroutine worker;
    protected OscReceiver receiver;
    protected OscSender sender;

    #region unity
    private void OnEnable() {
        switch (presets.mode) {
            case TestMode.Sender: {
                // Initialize sender in OnEnable, not in Coroutine
                var host = presets.host.FindFromHostName();
                var port = presets.port;
                IPEndPoint remoteEndpoint = new IPEndPoint(host, port);
                sender = new OscSender(remoteEndpoint);
                sender.Error += (e) => {
                    Debug.LogError(e);
                };

                worker = StartCoroutine(SendOnlyWork(presets.batch));
                break;
            }
            case TestMode.Receiver: {
                // Initialize receiver in OnEnable, not in Coroutine
                var port = presets.port;
                receiver = new OscReceiver(port);
                receiver.Receive += OnReceive;
                receiver.Error += (e) => {
                    Debug.LogError(e);
                };
                // No coroutine needed for receiver mode - events handle everything
                break;
            }
        }
    }
    private void OnDisable() {
        // Stop coroutine if running
        if (worker != null) {
            StopCoroutine(worker);
            worker = null;
        }

        // Dispose OSC resources
        if (receiver != null) {
            receiver.Dispose();
            receiver = null;
        }
        if (sender != null) {
            sender.Dispose();
            sender = null;
        }
    }
    #endregion

    #region worker
    private System.Collections.IEnumerator SendOnlyWork(int batch = 10000) {
        yield return null;

        // Sender is already initialized in OnEnable, just use it for sending
        var counter = 0;
        while (isActiveAndEnabled && sender != null) {
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
            if (counter % 2 == 0) {
                while (batchCount < batch && sw.ElapsedMilliseconds < 100) {
                    sender.Send(dataSync);
                    batchCount++;
                }
                sw.Stop();
                if (presets.verbose) {
                    Debug.Log($"Send sync: {1e3 * sw.ElapsedMilliseconds / batchCount:f1}us");
                }
            } else {
                while (batchCount < batch && sw.ElapsedMilliseconds < 100) {
                    sender.SendAsync(dataAsync);
                    batchCount++;
                }
                sw.Stop();
                if (presets.verbose) {
                    Debug.Log($"Send async: {1e3 * sw.ElapsedMilliseconds / batchCount:f1}us");
                }
            }

            yield return new WaitForSeconds(0.1f);
            counter++;
        }
    }
    #endregion

    #region listener
    public void OnReceive(Capsule capsule) {
        if (presets.verbose)
            Debug.Log($"OnReceive : {capsule}");
    }
    #endregion

    #region declarations

    public enum TestMode {
        Sender,
        Receiver
    }
    [System.Serializable]
    public class Presets {
        public bool verbose = false;
        public TestMode mode = TestMode.Sender;
        public string host = "localhost";
        public int port = 10000;
        public int batch = 10000;
    }
    #endregion
}