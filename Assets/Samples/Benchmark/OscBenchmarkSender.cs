using Osc2;
using System.Collections;
using System.Diagnostics;
using System.Net;
using UnityEngine;

/// <summary>OSCベンチマーク送信側（受信アプリと別プロセスで実行）</summary>
public class OscBenchmarkSender : MonoBehaviour {

    [System.Serializable]
    public class Presets {
        public string host = "localhost";
        public int port = 10000;
        [Tooltip("ログ出力間隔(秒)")]
        public float duration = 3f;
        [Tooltip("Send: 同期送信, SendAsync: 非同期送信")]
        public SendMode sendMode = SendMode.SendAsync;
        [Tooltip("メッセージタイプ")]
        public MessageSize messageSize = MessageSize.Small;
    }

    public enum SendMode { Send, SendAsync }
    public enum MessageSize { Small, Large }

    public Presets presets = new();

    OscSender sender;
    Coroutine benchmarkCoroutine;
    byte[] oscData;
    IPEndPoint remote;

    void OnEnable() {
        var host = presets.host.FindFromHostName();
        remote = new IPEndPoint(host, presets.port);
        sender = new OscSender(remote);
        sender.Error += e => UnityEngine.Debug.LogError(e);

        oscData = CreateTestMessage(presets.messageSize);
        benchmarkCoroutine = StartCoroutine(RunBenchmark());
    }

    void OnDisable() {
        if (benchmarkCoroutine != null) {
            StopCoroutine(benchmarkCoroutine);
            benchmarkCoroutine = null;
        }
        sender?.Dispose();
    }

    static byte[] CreateTestMessage(MessageSize size) {
        if (size == MessageSize.Small)
            return new Encoder("/bench").Add(1).Add("test").Add(3.14f).Encode();
        return new Encoder("/bench/large")
            .Add(1).Add(2).Add(3).Add(4).Add(5)
            .Add(1f).Add(2f).Add(3f).Add(4f).Add(5f)
            .Add("a").Add("bc").Add("def").Add("ghij").Add("klmno")
            .Encode();
    }

    IEnumerator RunBenchmark() {
        yield return new WaitForSeconds(0.5f);

        var sw = Stopwatch.StartNew();
        var sendCount = 0;

        while (true) {
            if (presets.sendMode == SendMode.Send)
                sender.Send(oscData);
            else
                sender.SendAsync(oscData);
            sendCount++;
            if (sendCount % 10000 == 0)
                yield return null;
            if (sw.Elapsed.TotalSeconds >= presets.duration) {
                var elapsed = sw.Elapsed.TotalSeconds;
                UnityEngine.Debug.Log($"[Benchmark Sender] Sent {sendCount} msg ({sendCount / elapsed:N0} msg/s) in {elapsed:F2}s");
                sw.Reset();
                sw.Start();
                sendCount = 0;
            }
        }
    }
}
