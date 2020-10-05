namespace Assets.Scripts.Messages.Server.SoundMessages
{
	/// <summary>
	///     Message that tells client to stop playing a sound
	/// </summary>
	public class StopSoundMessage: ServerMessage
	{
		public string Name;

		public override void Process()
		{
			SoundManager.Stop(Name);
		}

		/// <summary>
		/// Send to all client to stop playing a sound
		/// </summary>
		/// <param name="name">The name of the sound.</param>
		/// <returns>The sent message</returns>
		public static StopSoundMessage SendToAll(string name)
		{
			StopSoundMessage msg = new StopSoundMessage
			{
				Name = name
			};

			msg.SendToAll();

			return msg;
		}
	}
}
