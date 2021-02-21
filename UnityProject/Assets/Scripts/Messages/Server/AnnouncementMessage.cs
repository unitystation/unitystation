using System.Collections;
using Mirror;

/// <summary>
///     Message that tells client to add a ChatEvent to their chat
/// </summary>
public class AnnouncementMessage : ServerMessage
{
	public class AnnouncementMessageNetMessage : NetworkMessage
	{
		public string Text;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as AnnouncementMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		//Yeah, that will lead to n +-simultaneous tts synth requests, mary will probably struggle
		MaryTTS.Instance.Synthesize( newMsg.Text, bytes => {
			Synth.Instance.PlayAnnouncement(bytes);
		} );
	}

	public static AnnouncementMessageNetMessage SendToAll( string text )
	{
		AnnouncementMessageNetMessage msg = new AnnouncementMessageNetMessage{ Text = text };

		new AnnouncementMessage().SendToAll(msg);

		return msg;
	}
}