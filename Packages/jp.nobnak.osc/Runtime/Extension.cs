using System.Net.Sockets;
using System.Net;
using UnityEngine;

namespace Osc2 {
    public static class Extension {

        public static IPAddress FindFromHostName(this string hostname) {
            var address = IPAddress.None;
            try {
                if (IPAddress.TryParse(hostname, out address))
                    return address;

                var addresses = Dns.GetHostAddresses(hostname);
                for (var i = 0; i < addresses.Length; i++) {
                    if (addresses[i].AddressFamily == AddressFamily.InterNetwork) {
                        address = addresses[i];
                        break;
                    }
                }
            } catch (System.Exception e) {
                Debug.LogErrorFormat(
                    "Failed to find IP for :\n host name = {0}\n exception={1}",
                    hostname, e);
            }
            return address;
        }
    }
}
