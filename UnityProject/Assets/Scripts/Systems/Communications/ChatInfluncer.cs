namespace Systems.Communications
{
	public interface IChatInfluncer
	{
		public bool RunChecks();
		public ChatEvent InfluenceChat(ChatEvent chatToManipulate);
	}
}