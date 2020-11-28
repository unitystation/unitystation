namespace Assets.Scripts.Messages.Server.SoundMessages
{
	/// <summary>
	///     Message that tells client to stop playing a sound
	/// </summary>
	public class StopSoundMessage: ServerMessage
	{
		public string SoundSpawnToken;

		public override void Process()
		{
			SoundManager.Stop(SoundSpawnToken);
		}

		/// <summary>
		/// Send to all client to stop playing a sound
		/// </summary>
		/// <param name="name">The SoundSpawn Token that identifies the sound instance to stop.</param>
		/// <returns>The sent message</returns>
		public static StopSoundMessage SendToAll(string soundSpawnToken)
		{
			StopSoundMessage msg = new StopSoundMessage
			{
				SoundSpawnToken = soundSpawnToken
			};

			msg.SendToAll();

			return msg;
		}
	}
}
