using UnityEngine;

namespace Osc2.Samples {

    public class PacketLogger : MonoBehaviour {

        public void Listen(Capsule capsule) {
            Debug.Log($"Packet: {capsule}");
        }
    }
}