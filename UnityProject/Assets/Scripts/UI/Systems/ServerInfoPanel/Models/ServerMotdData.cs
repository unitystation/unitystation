namespace UI.Systems.ServerInfoPanel.Models
{
	//FIXME: once Nooney has finished the ServerData nuking, move this class to another place. We should also get all public server info in a single class and secrets in another.
	public class ServerMotdData
	{
		public string ServerName { get; init; }
		public string ServerDescription { get; init; }
		public string DiscordLink { get; init; }
	}
}