using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Osc2.Samples {

    public class SimpleOSCReceiver : MonoBehaviour {

        [SerializeField] protected Config config = new();

        protected List<OscReceiver> receivers = new();
        protected ConcurrentQueue<(int index, Capsule capsule)> queue = new();

        #region unity
        private void OnEnable() {
            for (var i = 0; i < config.portReceivers.Count; i++) {
                var index = i;
                var port = config.portReceivers[i].port;
                var recv = new OscReceiver(port);
                recv.Receive += (capsule) => {
                    lock (queue)
                        queue.Enqueue((index, capsule));
                };
                recv.Error += (e) => {
                    Debug.LogError(e);
                };
                receivers.Add(recv);
            }
        }
        private void OnDisable() {
            foreach (var recv in receivers) {
                recv.Dispose();
            }
            receivers.Clear();
        }
        private void Update() {
            if (Monitor.TryEnter(queue)) {
                try {
                    while (queue.TryDequeue(out var item)) {
                        var index = item.index;
                        var capsule = item.capsule;
                        var portReceiver = config.portReceivers[index];
                        portReceiver.OnReceive?.Invoke(capsule);
                    }
                } finally {
                    Monitor.Exit(queue);
                }
            }
        }
        #endregion

        #region declarations
        [System.Serializable]
        public class PortReceiver {
            public int port = 10000;
            public UnityEvent<Capsule> OnReceive = new();
        }
        [System.Serializable]
        public class Config {
            public List<PortReceiver> portReceivers = new();
        }
        #endregion
    }
}