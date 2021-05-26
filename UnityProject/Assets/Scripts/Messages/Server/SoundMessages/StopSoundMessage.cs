
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
		}

		public override void Process(NetMessage msg)
		{
			SoundManager.Stop(msg.SoundSpawnToken);
		}

		/// <summary>
		/// Send to all client to stop playing a sound
		/// </summary>
		/// <param name="name">The SoundSpawn Token that identifies the sound instance to stop.</param>
		/// <returns>The sent message</returns>
		public static NetMessage SendToAll(string soundSpawnToken)
		{
			NetMessage msg = new NetMessage
			{
				SoundSpawnToken = soundSpawnToken
			};

			SendToAll(msg);
			return msg;
		}
	}
}
