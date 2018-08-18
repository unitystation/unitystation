using System.Collections;

/// <summary>
///     Message that tells client to add a ChatEvent to their chat
/// </summary>
public class AnnouncementMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.AnnouncementMessage;
	public string Text;
//	public byte[] Sound;
	//later: announcement type/voice settings etc

	public override IEnumerator Process() {
		yield return null;

//		Synth.Instance.PlayAnnouncement(Sound);

		//Yeah, that will lead to n +-simultaneous tts synth requests, mary will probably struggle
		MaryTTS.Instance.Synthesize( Text, bytes => {
			Synth.Instance.PlayAnnouncement(bytes);
		} );
	}

	public static AnnouncementMessage SendToAll( string text ) {
		AnnouncementMessage msg = new AnnouncementMessage{ Text = text };

//		AnnouncementMessage msg = new AnnouncementMessage();
//		MaryTTS.Instance.Synthesize( text, bytes => {
//			msg.Sound = bytes;
//			msg.SendToAll();
//		} );

		msg.SendToAll();

		return msg;
	}

	public override string ToString()
	{
		return $"[AnnouncementMessage Text={Text}]";
//		return string.Format("[AnnouncementMessage size={0}]", Sound?.Length);
	}
}