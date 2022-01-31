using Mirror;
using Player;

namespace Messages.Client.NewPlayer
{
	public class PlayerNewPlayer : ClientMessage<PlayerNewPlayer.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint Player;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.Player);
			if (NetworkObject == null) return;
			NetworkObject.GetComponent<PlayerSync>()?.NotifyPlayer(
				SentByPlayer.Connection);
			NetworkObject.GetComponent<PlayerSprites>()?.NotifyPlayer(
				SentByPlayer.Connection);
			NetworkObject.GetComponent<Equipment>()?.NotifyPlayer(
				SentByPlayer.Connection);
		}

		public static NetMessage Send(uint netId)
		{
			NetMessage msg = new NetMessage
			{
				Player = netId
			};

			Send(msg);
			return msg;
		}
	}
}
