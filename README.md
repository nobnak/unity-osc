unity-osc
=========
OSC for Unity

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
var msg = new MessageEncoder("/path");
msg.Add(3.14f);
msg.Add(12345);
var dest = new IPEndPoint("Client IP Address", "Client Port Number");
oscPort.Send(msg, dest);
```
