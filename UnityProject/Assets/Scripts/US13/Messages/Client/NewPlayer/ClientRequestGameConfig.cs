using Mirror;
using Newtonsoft.Json;
using US13.Managers;
using US13.Messages.Server.JoinedViewer;

namespace US13.Messages.Client.NewPlayer
{
	public class ClientRequestGameConfig : ClientMessage<ClientRequestGameConfig.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public NetworkIdentity whoAsked;
		}

		public override void Process(NetMessage msg)
		{
			string currentConfig = JsonConvert.SerializeObject(GameConfigManager.GameConfig);
			UpdateServerGameConfigForAll.SendTo(currentConfig, msg.whoAsked);
		}
	}
}