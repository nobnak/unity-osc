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

## Important Note about Resource Management
**重要**: Coroutine内で`using`を使用すると、Coroutineが途中で停止された場合にDisposeが呼ばれない可能性があります。OSCのSender/ReceiverはOnEnableで初期化し、OnDisableで明示的にDisposeするようにし、Coroutineでは送信/受信処理のみを分離してください。

## Set Up a Receiver (MonoBehaviour Example)
```C#
public class OSCReceiverExample : MonoBehaviour {
    protected OscReceiver receiver;

    private void OnEnable() {
        // Initialize receiver in OnEnable - no coroutine needed!
        var port = 10000;
        receiver = new OscReceiver(port);
        receiver.Receive += OnReceive;
        receiver.Error += (e) => {
            Debug.LogError(e);
        };
        // Receiver works with events - no coroutine required
    }

    private void OnDisable() {
        // Dispose receiver
        if (receiver != null) {
            receiver.Dispose();
            receiver = null;
        }
    }

    void OnReceive(Capsule capsule) {
        Debug.Log(capsule);
    }
}
```

## Set Up a Sender (MonoBehaviour Example)
```C#
public class OSCSenderExample : MonoBehaviour {
    protected IEnumerator senderEnumerator;
    protected Coroutine senderCoroutine;
    protected OscSender sender;

    private void OnEnable() {
        // Initialize sender in OnEnable, not in Coroutine
        var host = "localhost";
        var port = 10000;
        sender = new OscSender(host, port);
        sender.Error += (e) => {
            Debug.LogError(e);
        };

        // Start coroutine for sending only
        senderEnumerator = SendWork();
        senderCoroutine = StartCoroutine(senderEnumerator);
    }

    private void OnDisable() {
        // Stop coroutine first
        if (senderCoroutine != null) {
            StopCoroutine(senderCoroutine);
            senderCoroutine = null;
            senderEnumerator = null;
        }

        // Then dispose sender
        if (sender != null) {
            sender.Dispose();
            sender = null;
        }
    }

    System.Collections.IEnumerator SendWork() {
        // Coroutine only handles sending, not resource management
        while (isActiveAndEnabled) {
            var msg = new Encoder("/async")
                .Add(1)
                .Add("hello")
                .Add(3.14f);
            sender.SendAsync(msg);

            yield return new WaitForSeconds(0.1f);
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
