When looking through our code, you'll notice different that some network actions are handles differently. We have a guideline with which one to use. But take note: although many are already in the codebase, their legacy use may be wrong in comparison to current guidelines. So don't trust the code blindly!

## NetMsg

Netmessages should be used in case where security of information is important. Ask yourself "If someone fakes this, could it influence the gameplay for others"?
Be mindfull that NetMsg does create some garbage, so use it wisely!

An example of a message sent by the server to a client:

``` c#
/// <summary>
///     Message that tells client to add a ChatEvent to their chat
/// </summary>
public class AnnouncementMessage : ServerMessage
{
	public struct AnnouncementMessageNetMessage : NetworkMessage
	{
		public string Text;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public AnnouncementMessageNetMessage IgnoreMe;

  //This function is called on client
	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as AnnouncementMessageNetMessage?;
		if(newMsgNull == null) return;
    var newMsg = newMsgNull.Value;

		//Yeah, that will lead to n +-simultaneous tts synth requests, mary will probably struggle
		MaryTTS.Instance.Synthesize( newMsg.Text, bytes => {
			Synth.Instance.PlayAnnouncement(bytes);
		} );
	}

  //Server sends message by this function
	public static AnnouncementMessageNetMessage SendToAll( string text )
	{
		AnnouncementMessageNetMessage msg = new AnnouncementMessageNetMessage{ Text = text };

		new AnnouncementMessage().SendToAll(msg);

		return msg;
	}
}

```
-The class needs to be dervied from ServerMessage (if you want to send it to clients) or ClientMessage (if you want to send something to the server)

-The message must contain a struct which has the variables you want to send.

-Message needs to have that IgnoreMe variable of the message struct type, which is used in the code to set handlers so the message has functionality.

-The process function is what is called on client (or server if ClientMessage) when the message is received, you'll need to add the next three line checks so the type is correct.

## RPC
RPC is a nice and clean solution to send non-secure data. An example for non-secure data is grafical-only information such as disabling a SpriteRenderer. RPC is however a clean and efficient protocol, that should be used where security is not an issue.

Please note, that some insignificant graffical updates, may be important to send sparsely.

## SyncVar
SyncVar can be a simple alternative to NetMsg or RPC for sending updates to all clients. But it has some caveats for how to use it without having unexpected behavior. See [SyncVar Best Practices for Easy Networking](SyncVar-Best-Practices-for-Easy-Networking.md)
