using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Osc2.Samples {

    public class SampleOSCSender : MonoBehaviour {

        [SerializeField] protected Config config = new();

        protected List<OscSender> senders = new(); 

        #region unity
        private void OnEnable() {
            foreach (var portSender in config.ports) {
                var port = portSender.port;
                var sender = new OscSender(config.host, port);
                sender.Error += (e) => {
                    Debug.LogError(e);
                };
                senders.Add(sender);
            }
        }
        private void OnDisable() {
            foreach (var sender in senders) {
                sender.Dispose();
            }
            senders.Clear();
        }
        private void Update() {
            if (senders.Count < 2) return;

            var msg = new Encoder("/sync");
            msg.Add("Hello");
            senders[0].Send(msg);

            var msg2 = new Encoder("/async");
            msg2.Add("World");
            senders[1].SendAsync(msg2);
        }
        #endregion

        #region declarations
        [System.Serializable]
        public class PortSender {
            public int port = 10000;
        }
        [System.Serializable]
        public class Config {
            public string host = "localhost";
            public List<PortSender> ports = new();
        }
        #endregion
    }
}