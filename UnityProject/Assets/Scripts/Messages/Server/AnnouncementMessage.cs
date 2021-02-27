using Mirror;

namespace Messages.Server
{
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
}