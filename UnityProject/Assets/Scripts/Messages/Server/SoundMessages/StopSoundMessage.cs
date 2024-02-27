
 using Mirror;
 using UnityEngine.Serialization;

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
			public bool Pool;
			public bool Play;
			public bool PlayOneShot;
		}

		public override void Process(NetMessage msg)
		{
			if (msg.Play)
			{
				SoundManager.ClientTokenPlay(msg.SoundSpawnToken, msg.PlayOneShot);
			}
			else
			{
				SoundManager.ClientStop(msg.SoundSpawnToken, msg.Pool);
			}

		}

		/// <summary>
		/// Send to all client to stop playing a sound
		/// </summary>
		/// <param name="name">The SoundSpawn Token that identifies the sound instance to stop.</param>
		/// <returns>The sent message</returns>
		public static NetMessage SendToAll(string soundSpawnToken, bool Pool, bool Play, bool PlayOneShot)
		{
			NetMessage msg = new NetMessage
			{
				SoundSpawnToken = soundSpawnToken,
				Pool = Pool,
				Play = Play,
				PlayOneShot =  PlayOneShot
			};

			SendToAll(msg);
			return msg;
		}
	}
}
