using System;
using Logs;
using Mirror;
using Newtonsoft.Json;
using US13.Managers;

namespace US13.Messages.Server.JoinedViewer
{
	public class UpdateServerGameConfigForAll : ServerMessage<UpdateServerGameConfigForAll.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string GameConfigSeralized;
		}

		public override void Process(NetMessage msg)
		{
			try
			{
				GameConfig config = JsonConvert.DeserializeObject<GameConfig>(msg.GameConfigSeralized);
				GameConfigManager.Instance.SetGameConfig(config);
				Loggy.Info("Server updated gameConfig recently, and new config has been set.");
			}
			catch (Exception e)
			{
				Loggy.Error(e.ToString());
			}
		}

		public static NetMessage SendToAll(string gameConfigJson)
		{
			NetMessage msg = new()
			{
				GameConfigSeralized = gameConfigJson
			};
			SendToAll(msg);
			return msg;
		}
	}
}