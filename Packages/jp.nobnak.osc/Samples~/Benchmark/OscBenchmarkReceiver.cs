using Osc2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>OSCベンチマーク受信側（送信アプリと別プロセスで実行）</summary>
public class OscBenchmarkReceiver : MonoBehaviour {

    [System.Serializable]
    public class Presets {
        public int port = 10000;
        [Tooltip("ログ出力間隔(秒)")]
        public float duration = 3f;
        [Tooltip("Receiverの文字列キャッシュ使用")]
        public bool useStringCaching = true;
        [Tooltip("メッセージタイプ（Senderと合わせること）")]
        public MessageSize messageSize = MessageSize.Small;
        [Tooltip("GC測定用に保持するメッセージ数")]
        public int retainCount = 50000;
    }

    public enum MessageSize { Small, Large }

    public Presets presets = new();

    OscReceiver receiver;
    Coroutine benchmarkCoroutine;

    int receivedCount;
    readonly List<Capsule> retained = new();
    readonly object retainedLock = new();

    void OnEnable() {
        receiver = new OscReceiver(presets.port, presets.useStringCaching);
        receiver.Receive += OnReceive;
        receiver.Error += e => UnityEngine.Debug.LogError(e);
        benchmarkCoroutine = StartCoroutine(RunBenchmark());
    }

    void OnDisable() {
        if (benchmarkCoroutine != null) {
            StopCoroutine(benchmarkCoroutine);
            benchmarkCoroutine = null;
        }
        receiver?.Dispose();
    }

    void OnReceive(Capsule c) {
        Interlocked.Increment(ref receivedCount);
        lock (retainedLock) {
            if (retained.Count < presets.retainCount)
                retained.Add(c);
        }
    }

    IEnumerator RunBenchmark() {
        yield return new WaitForSeconds(0.5f);

        var sw = Stopwatch.StartNew();
        var lastReceived = 0;

        while (true) {
            yield return new WaitForSeconds(presets.duration);

            var currentReceived = Volatile.Read(ref receivedCount);
            var received = currentReceived - lastReceived;
            lastReceived = currentReceived;
            var elapsed = sw.Elapsed.TotalSeconds;
            sw.Reset();
            sw.Start();

            var memPeak = Profiler.GetTotalAllocatedMemoryLong();
            int retainedCount;
            lock (retainedLock) {
                retainedCount = retained.Count;
                retained.Clear();
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memAfterGC = Profiler.GetTotalAllocatedMemoryLong();
            var gcReclaimed = memPeak - memAfterGC;

            var receiveRate = received / elapsed;
            var reclaimedPerMsg = retainedCount > 0 ? gcReclaimed / (double)retainedCount : 0;

            var report = $@"=== OSC Benchmark (Receiver) ===
Duration: {elapsed:F2}s
Receive: {received} msg ({receiveRate:N0} msg/s) total: {currentReceived}
Memory: {memPeak / 1024:N0} KB (peak) -> {memAfterGC / 1024:N0} KB (after GC)
GC reclaimed: {gcReclaimed:N0} bytes ({gcReclaimed / 1024:N0} KB) = {reclaimedPerMsg:F1} bytes/msg (cleared {retainedCount} retained)
Config: caching={presets.useStringCaching}, msgSize={presets.messageSize}
====================";
            UnityEngine.Debug.Log(report);
        }
    }
}
