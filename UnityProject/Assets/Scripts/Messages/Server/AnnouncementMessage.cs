using System.Collections;

/// <summary>
///     Message that tells client to add a ChatEvent to their chat
/// </summary>
public class AnnouncementMessage : ServerMessage
{
	public class AnnouncementMessageNetMessage : ActualMessage
	{
		public string Text;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as AnnouncementMessageNetMessage;
		if(newMsg == null) return;

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