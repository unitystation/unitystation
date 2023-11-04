using Mirror;

namespace Messages.Server
{
	public class Activate3DMode : ServerMessage<Activate3DMode.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{

		}

		public override void Process(NetMessage msg)
		{
			if (CustomNetworkManager.IsServer)
			{
				return;
			}
			Manager3D.Instance.PromptConvertTo3D();
		}

		public static void SendToEveryone()
		{
			SendToAll(new NetMessage());
		}

		public static void SendTo(NetworkConnectionToClient Player)
		{
			SendTo(Player, new NetMessage());
		}


	}
}
