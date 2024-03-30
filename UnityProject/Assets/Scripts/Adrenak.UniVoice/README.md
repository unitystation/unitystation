Note: Inbuilt implementations and samples have been removed from this repository. They'll be added to separate repositories soon.

# UniVoice
UniVoice is a voice chat/VoIP solution for Unity.
  
Some features of UniVoice:
- 👥 Group voice chat. Multiple peers can join a chatroom and exchange audio.  

- ⚙ Peer specific settings. Don't want to listen to a peer? Mute them. Don't want someone listening to you? Mute yourself against them.
 
- 🎨 Customize your audio input, output and networking layer. 
  * 🎤 __Configurable Audio Input__: Decide what the input of your outgoing audio is. Let it be from [Unity's Microphone](https://docs.unity3d.com/ScriptReference/Microphone.html) class, or a live streaming audio, or an MP4 file on the disk.
    
  * 🔊 __Configurable Audio Output__:  Decide where the incoming peer audio goes. Let the output of incoming audio be [Unity AudioSource](https://docs.unity3d.com/ScriptReference/AudioSource.html) to play the audio in-game, or write it into an MP4 on the disk, or stream it to an online service.

  * 🌐 __Configurable Network__: Want to use UniVoice in a WLAN project using [Telepathy?](https://github.com/vis2k/Telepathy) Just adapt its API for UniVoice with a simple the `IChatroomNetwork` interface. Using your own backend for multiplayer? Create and expose your audio API and write a UniVoice implementation, again with the same interface.
  
# Docs
Manuals and sample projects are not available yet. For the API reference, please visit http://www.vatsalambastha.com/univoice
  
# Usage
## Creating a chatroom agent
- To be able to host and join voice chatrooms, you need a `ChatroomAgent` instance.
  
```
var agent = new ChatroomAgent(IChatroomNetwork network, IAudioInput audioInput, IAudioOutput audioOutput);
```

## Hosting and joining chatrooms

Every peer in the chatroom is assigned an ID by the host. And every peer has a peer list, representing the other peers in the chatroom.

- To get your ID  
`agent.Network.OwnID;`
  
- To get a list of the other peers in the chatroom, use this:  
`agent.Network.PeersIDs`

`agent.Network` also provides methods to host or join a chatroom. Here is how you use them:
  
```
// Host a chatroom using a name
agent.Network.HostChatroom(optional_data);

// Join an existing chatroom using a name
agent.Network.JoinChatroom(optional_data);

// Leave the chatroom, if connected to one
agent.Network.LeaveChatroom(optional_data);

// Closes a chatroom, if is hosting one
agent.Network.CloseChatroom(optional_data);

```
## Muting Audio
To mute everyone in the chatroom, use `agent.MuteOthers = true;` or set it to `false` to unmute them all.  
  
To mute yourself use `agent.MuteSelf = true;` or set it to `false` to unmute yourself. This will stop sending your audio to all the peers in the chatroom.

For muting a specific peer, first get the peers settings object using this:  
```
agent.PeerSettings[id].muteThem = true; // where id belongs to the peer in question
```
  
If you want to mute yourself towards a specific peer, use this:
`agent.PeerSettings[id].muteSelf = true; // where id belongs to the peer in question`
  
## Events
`agent.Network` provides several network related events. Refer to the [API reference](http://www.vatsalambastha.com/univoice/api/Adrenak.UniVoice.ChatroomAgent.html) for them.

# License and Support
This project is under the [MIT license](https://github.com/adrenak/univoice/blob/master/LICENSE).

Updates and maintenance are not guaranteed and the project is maintained by the original developer in his free time. Community contributions are welcome.
  
__Commercial consultation and development can be arranged__ but is subject to schedule and availability.  
  
# Contact
The developer can be reached at the following links:
  
[Website](http://www.vatsalambastha.com)  
[LinkedIn](https://www.linkedin.com/in/vatsalAmbastha)  
[GitHub](https://www.github.com/adrenak)  
[Twitter](https://www.twitter.com/vatsalAmbastha)  
