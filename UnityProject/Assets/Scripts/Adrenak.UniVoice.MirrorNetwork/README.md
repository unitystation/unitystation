## UniVoice Mirror Network

This repository contains the [Mirror](https://www.github.com/vis2k/mirror) based implementation of the IChatroomNetwork interface in [UniVoice](https://www.github.com/adrenak/univoice)

## Dependencies
### com.adrenak.univoice@3.0.0 
The UniVoice version this implementation uses.

### com.adrenak.brw@1.0.1
A simple byte array reader and writer it uses to send and receive messages 

## Installation
1. Ensure the UniVoice package has been imported in your project.
1. Import this package.
1. Import Mirror. This package was last tested with Mirror version 87.1.7. The Mirror APIs used don't tend to change much, so breaking changes should be minimal.
1. Add `UNIVOICE_MIRROR` to your projects Scripting Define Symbols to enable the `UniVoiceMirrorNetwork` class.

## How it works & how to use
The `UniVoiceMirrorNetwork` class is incapable of hosting, joining, closing and leaving a chatroom by itself. Tt just listens to and uses your Mirror server/client. At the root of it, it just calls:  
- `NetworkServer.SendToAll` for the server to broadcast messages to the peers.
- `NetworkClient.Send` for the peers to send their audio to the server/host.
- Connection & Disconnection events of the Client & Server using `NetworkManager.single.transport`
- `RegisterHandler` on `NetworkClient` and `NetworkServer` to get messages.

You create a ChatroomAgent with 
1. an instance of `UniVoiceMirrorNetwork`
1. the audio output implementation of your choice. You're likely looking for [AudioSourceOutput](https://www.github.com/adrenak/univoice-audiosource-output) which plays audio using Unity's AudioSource
1. the factory method of an audio input implementation of your choice. Again, you're likely looking for [UniMicInput](https://www.github.com/adrenak/univoice-unimic-input) which uses [UniMic](https://www.github.com/adrenak/unimic), a wrapper over Unity's `Microphone` class to capture audio from the microphone.
This could look like this:
```
var chatroomAgent = new ChatroomAgent (
    new UniVoiceMirrorNetwork(),
    new UniVoiceUniMicInput(0, 8000, 50),
    new UniVoiceAudioSourceOutput.Factory()
);
```

That's it! When you create of join a server/host using Mirror, the audio chat will be initialized.

If you're looking for a lighter network, maybe if you're not using Mirror and just want WLAN audio chat, try [UniVoice-Telepathy-Network](https://www.github.com/adrenak/univoice-telepathy-network) based on [Telepathy by vis2k](https://www.github.com/vis2k/telepathy)

## Notes
- This network implementation has only been tested with 1 server + 2 clients. A deployment on EdgeGap (cloud) worked just fine. It's not been used in production so far, although two parties have shown interest in doing so. Due to this upcoming improvements are expected and I'll try to keep them non breaking.
- You would want to use some sort of UDP transport in Mirror. There is usually some sort of a packet size limit, for example, Kcp (packaged with Mirror) has a limit of 1194 bytes. Adjust your UniVoiceMicInput constructor parameters accordingly. 

## Contact
The developer Vatsal Ambastha can be reached at the following links:
[website](http://www.vatsalambastha.com)  
[linkedin](https://www.linkedin.com/in/vatsalambastha)  
[github](https://www.github.com/adrenak)  
[twitter](https://www.twitter.com/vatsalambastha)  
[Discord server](https://discord.gg/4n6rUcRQuN)  

UniVoice has been used by games, apps, as well as research projects. Commercial development and consultation can be arranged, although it's very much subject to availability and schedule.  