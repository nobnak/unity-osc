unity-osc
=========
OSC Sender/Receiver for Unity

# Installation
Released as [UPM package](https://openupm.com/packages/jp.nobnak.osc/) on OpenUPM.

- Add URL "https://package.openupm.com" in a Scoped Registry
- Add scope "jp.nobnak"
- Add package "jp.nobnak.osc".

# Usage
## Set Up a Server
 - Attach OscPortSocket script
 - Set Listening Port Number
 - Listen OnReceive Event

## Set Up a Client
 - Attach OscPortSocket script
 - Send messages

## Send a Message
```C#
 var msg = new MessageEncoder("/test")
     .Add(123)
     .Add("hello")
     .Add(3.14f);
var dest = new IPEndPoint("Server IP Address", "Server Port Number");
oscPort.Send(msg, dest);
```
