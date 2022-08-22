
 using Mirror;

namespace Messages.Server.SoundMessages
{
	/// <summary>
	///     Message that tells client to stop playing a sound
	/// </summary>
	public class StopMusicMessage : ServerMessage<StopMusicMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string MusicSpawnToken;
		}

		public override void Process(NetMessage msg)
		{
			Audio.Containers.MusicManager.StopMusic();
		}

		/// <summary>
		/// Send to all client to stop playing a Music
		/// </summary>
		/// <param name="name">The MusicSpawn Token that identifies the Music instance to stop.</param>
		/// <returns>The sent message</returns>
		public static NetMessage SendToAll(string MusicSpawnToken)
		{
			NetMessage msg = new NetMessage
			{
				MusicSpawnToken = MusicSpawnToken
			};

			SendToAll(msg);
			return msg;
		}
	}
}
