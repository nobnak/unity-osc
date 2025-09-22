# unity-osc

OSC (Open Sound Control) Sender/Receiver library for Unity

## Installation

Available as a [UPM package](https://openupm.com/packages/jp.nobnak.osc/) on OpenUPM.

1. Add URL "https://package.openupm.com" as a Scoped Registry
2. Add scope "jp.nobnak"
3. Add package "jp.nobnak.osc"

## Features

- **High-performance OSC communication** optimized for Unity
- **Event-driven receiver** - no polling required
- **Thread-safe implementation** for reliable data transfer
- **Memory-efficient** design with proper resource management

## Quick Start

### Receiving OSC Messages

The receiver works entirely with events - no coroutines needed!

```csharp
public class OSCReceiverExample : MonoBehaviour 
{
    private OscReceiver receiver;

    void OnEnable() 
    {
        var port = 10000;
        receiver = new OscReceiver(port);
        receiver.Receive += OnReceiveOSC;
        receiver.Error += OnError;
    }

    void OnDisable() 
    {
        receiver?.Dispose();
        receiver = null;
    }

    void OnReceiveOSC(Capsule capsule) 
    {
        Debug.Log($"Received: {capsule}");
    }

    void OnError(System.Exception error) 
    {
        Debug.LogError($"OSC Error: {error}");
    }
}
```

### Sending OSC Messages

For continuous sending, use a coroutine for the sending logic only:

```csharp
public class OSCSenderExample : MonoBehaviour 
{
    private OscSender sender;
    private Coroutine sendingCoroutine;

    void OnEnable() 
    {
        var host = "localhost";
        var port = 10000;
        sender = new OscSender(host, port);
        sender.Error += OnError;

        sendingCoroutine = StartCoroutine(SendingLoop());
    }

    void OnDisable() 
    {
        if (sendingCoroutine != null) 
        {
            StopCoroutine(sendingCoroutine);
            sendingCoroutine = null;
        }

        sender?.Dispose();
        sender = null;
    }

    IEnumerator SendingLoop() 
    {
        while (isActiveAndEnabled) 
        {
            var message = new Encoder("/test")
                .Add(Time.time)
                .Add("hello")
                .Add(Random.Range(0f, 100f));

            sender.SendAsync(message);
            yield return new WaitForSeconds(0.1f);
        }
    }

    void OnError(System.Exception error) 
    {
        Debug.LogError($"OSC Error: {error}");
    }
}
```

### One-time Message Sending

For sending individual messages without coroutines:

```csharp
public class SimpleOSCSender : MonoBehaviour 
{
    private OscSender sender;

    void Start() 
    {
        sender = new OscSender("localhost", 10000);
    }

    void OnDestroy() 
    {
        sender?.Dispose();
    }

    public void SendMessage() 
    {
        var message = new Encoder("/button/pressed")
            .Add(1)
            .Add("click");
            
        sender.SendAsync(message);
    }
}
```

## Message Building

Use the `Encoder` class to build OSC messages:

```csharp
var message = new Encoder("/osc/address")
    .Add(123)           // int
    .Add(3.14f)         // float
    .Add("text")        // string
    .Add(true);         // bool

// Send asynchronously (recommended)
sender.SendAsync(message);

// Send synchronously (blocks until sent)
sender.Send(message.Encode());
```

## Important Notes

### Resource Management
- **Always dispose** OSC objects in `OnDisable()` or `OnDestroy()`
- **Initialize in OnEnable()**, not inside coroutines
- **Avoid `using` statements inside coroutines** - they may not dispose properly if the coroutine is stopped

### Performance Tips
- Use `SendAsync()` for better performance (non-blocking)
- Receivers work with events - no polling or coroutines needed
- Only use coroutines for continuous sending logic

### Thread Safety
- All callbacks (Receive, Error) are thread-safe
- You can safely update Unity objects from OSC callbacks

## Examples

Check out the included sample scenes:
- `SimpleOSCReceiver` - Basic receiver setup
- `LoopbackTester` - Performance testing and debugging

## License

See [LICENSE](LICENSE) file for details.