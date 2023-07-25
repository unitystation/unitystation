When looking through our code, you'll notice different that some network actions are handles differently. We have a guideline with which one to use. But take note: although many are already in the codebase, their legacy use may be wrong in comparison to current guidelines. So don't trust the code blindly!


### Important note:
Any class/struct used to send data, Must be public otherwise this will cause IL errors and your Assembly will be rejected by the IL Scanner

## NetMsg

Netmessages should be used in case where security of information is important. Ask yourself "If someone fakes this, could it influence the gameplay for others"?
Be mindfull that NetMsg does create some garbage, so use it wisely!

An example of a message sent by the server to a client:

``` c#
	/// <summary>
	///     Message that tells client to add a ChatEvent to their chat
	/// </summary>
	public class AnnouncementMessage : ServerMessage<AnnouncementMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Text;
		}

		public override void Process(NetMessage msg)
		{
			//Yeah, that will lead to n +-simultaneous tts synth requests, mary will probably struggle
			MaryTTS.Instance.Synthesize( msg.Text, bytes => {
				Synth.Instance.PlayAnnouncement(bytes);
			} );
		}

		public static NetMessage SendToAll( string text )
		{
			NetMessage msg = new NetMessage{ Text = text };

			SendToAll(msg);
			return msg;
		}
	}

```
-The class needs to be dervied from ServerMessage (if you want to send it to clients) or ClientMessage (if you want to send something to the server)

-The message must contain a struct which has the variables you want to send, can be called anything but for consistancy should be call NetMessage, and is dervied from NetworkMessage.

-The process function is what is called on client (or server if ClientMessage) when the message is received.

-The SentToAll is static so can be called without an instance in any other script that needs to send this type of message, you create an instance of the message struct in it and fill it with the needed data, modifying the struct after it has been sent will do nothing.

## RPC
RPC is a nice and clean solution to send non-secure data. An example for non-secure data is grafical-only information such as disabling a SpriteRenderer. RPC is however a clean and efficient protocol, that should be used where security is not an issue.

Please note, that some insignificant graffical updates, may be important to send sparsely.

For target RPCs Please do not use NetworkConnectionToClient Use NetworkConnection Instead in the Function parameter, You doing it the wrong way will cause IL errors
You Should still pass in to The function x.ConnectionToClient, mirror will be able to handle conversion fine
## SyncVar
SyncVar can be a simple alternative to NetMsg or RPC for sending updates to all clients. But it has some caveats for how to use it without having unexpected behavior. See [SyncVar Best Practices for Easy Networking](SyncVar-Best-Practices-for-Easy-Networking.md)
