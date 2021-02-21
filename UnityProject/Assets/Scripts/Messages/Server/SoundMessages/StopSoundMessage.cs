using Mirror;

namespace Assets.Scripts.Messages.Server.SoundMessages
{
	/// <summary>
	///     Message that tells client to stop playing a sound
	/// </summary>
	public class StopSoundMessage : ServerMessage
	{
		public struct StopSoundMessageNetMessage : NetworkMessage
		{
			public string SoundSpawnToken;
		}

		//This is needed so the message can be discovered in NetworkManagerExtensions
		public StopSoundMessageNetMessage message;

		public override void Process<T>(T msg)
		{
			var newMsgNull = msg as StopSoundMessageNetMessage?;
			if(newMsgNull == null) return;
			var newMsg = newMsgNull.Value;

			SoundManager.Stop(newMsg.SoundSpawnToken);
		}

		/// <summary>
		/// Send to all client to stop playing a sound
		/// </summary>
		/// <param name="name">The SoundSpawn Token that identifies the sound instance to stop.</param>
		/// <returns>The sent message</returns>
		public static StopSoundMessageNetMessage SendToAll(string soundSpawnToken)
		{
			StopSoundMessageNetMessage msg = new StopSoundMessageNetMessage
			{
				SoundSpawnToken = soundSpawnToken
			};

			new StopSoundMessage().SendToAll(msg);

			return msg;
		}
	}
}
