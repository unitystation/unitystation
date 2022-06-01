
 using Mirror;

namespace Messages.Server.SoundMessages
{
	/// <summary>
	///     Message that tells client to stop playing a sound
	/// </summary>
	public class StopSoundMessage : ServerMessage<StopSoundMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string SoundSpawnToken;
			public int soundID;
		}

		public override void Process(NetMessage msg)
		{
			if(msg.soundID != 0)
			{
				SoundManager.Stop(msg.soundID);
				return;
			}
			SoundManager.Stop(msg.SoundSpawnToken);
		}

		/// <summary>
		/// Send to all client to stop playing a sound
		/// </summary>
		/// <param name="name">The SoundSpawn Token that identifies the sound instance to stop.</param>
		/// <returns>The sent message</returns>
		public static NetMessage SendToAll(string soundSpawnToken, int ID = 0)
		{
			NetMessage msg = new NetMessage
			{
				SoundSpawnToken = soundSpawnToken,
				soundID = ID,
			};

			SendToAll(msg);
			return msg;
		}
	}
}
