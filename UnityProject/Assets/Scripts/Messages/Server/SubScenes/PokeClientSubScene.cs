using Mirror;

namespace Messages.Server.SubScenes
{
	//TODO update mirror!!!!!!!!!!!, this is only here because mirror didn't update a synchronised list properly
	public class PokeClientSubScene : ServerMessage<PokeClientSubScene.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string ToLoadSceneName;
		}

		public override void Process(NetMessage msg)
		{
			if (CustomNetworkManager.Instance._isServer) return;

			SubSceneManager.ManuallyLoadScene(msg.ToLoadSceneName);
		}

		public static void SendToAll(string SceneName = "")
		{
			var msg = new NetMessage
			{
				ToLoadSceneName = SceneName
			};

			SendToAll(msg);
		}
	}
}
