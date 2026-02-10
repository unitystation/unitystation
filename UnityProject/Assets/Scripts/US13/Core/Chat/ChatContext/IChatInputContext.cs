namespace US13.Core.Chat.ChatContext
{
	public interface IChatInputContext
	{
		/// <summary>
		/// This is channel tagged as ':h'
		/// Depends on current headset, antags, etc
		/// </summary>
		ChatChannel DefaultChannel { get; }
	}
}
