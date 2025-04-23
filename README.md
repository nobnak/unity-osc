unity-osc
=========
OSC Sender/Receiver for Unity

# Installation
Released as [UPM package](https://openupm.com/packages/jp.nobnak.osc/) on OpenUPM.

- Add URL "https://package.openupm.com" in a Scoped Registry
- Add scope "jp.nobnak"
- Add package "jp.nobnak.osc".

# Usage
## Samples
See the SimpleOSCReceiver scene in the package examples to see how to receive OSC packets.

## Set Up a Receiver
```C#
StartCoroutine(Receiver());
System.Collections.IEnumerator Receiver() {
    yield return null;

    var port = 10000;
    using (var recv = new OscReceiver(port)) {
        recv.Receive += (capsule) => {
            Debug.Log(capsule);
        };
        recv.Error += (e) => {
            Debug.LogError(e);
        };

        while (true) {
            yield return null;
        }
    }
}
```

## Set Up a Sender
```C#
StartCoroutine(Sender());
System.Collections.IEnumerator Sender() {
    yield return null;

    var host = "localhost";
    var port = 10000;
    using (var sndr = new OscSender(host, port)) {
        while (true) {
            var msg = new Encoder("/async")
                .Add(1)
                .Add("hello")
                .Add(3.14f);
            sndr.SendAsync(msg);

            yield return null;
        }
    }
}
```

## Send a Message
```C#
 var msg = new Encoder("/test")
     .Add(123)
     .Add("hello")
     .Add(3.14f);
sender.SendAsync(msg);
//sender.Send(msg, dest);
```
