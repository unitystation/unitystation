using US13.Core.Chat;

namespace US13.Systems.Communications
{
	public interface IChatInfluencer
	{
		public bool WillInfluenceChat();
		public ChatEvent InfluenceChat(ChatEvent chatToManipulate);
	}
}