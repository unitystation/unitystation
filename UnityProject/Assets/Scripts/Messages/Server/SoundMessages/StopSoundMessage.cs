namespace Assets.Scripts.Messages.Server.SoundMessages
{
	/// <summary>
	///     Message that tells client to stop playing a sound
	/// </summary>
	public class StopSoundMessage : ServerMessage
	{
		public class StopSoundMessageNetMessage : ActualMessage
		{
			public string SoundSpawnToken;
		}

		public override void Process(ActualMessage msg)
		{
			var newMsg = msg as StopSoundMessageNetMessage;
			if(newMsg == null) return;

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
