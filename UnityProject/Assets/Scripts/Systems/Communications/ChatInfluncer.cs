namespace Systems.Communications
{
	public interface IChatInfluencer
	{
		public bool WillInfluenceChat();
		public ChatEvent InfluenceChat(ChatEvent chatToManipulate);
	}
}