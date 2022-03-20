namespace Systems.Communications
{
	public interface IChatInfluencer 
	{
		public bool RunChecks();
		public ChatEvent InfluenceChat(ChatEvent chatToManipulate);
	}
}