using Mirror;

namespace Messages.Client.NewPlayer
{
	public class CustomNetTransformNewPlayer : ClientMessage<CustomNetTransformNewPlayer.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint CNT;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.CNT);
			if (NetworkObject == null) return;
			NetworkObject.GetComponent<CustomNetTransform>()?.NotifyPlayer(
				SentByPlayer.Connection);
		}

		public static NetMessage Send(uint netId)
		{
			NetMessage msg = new NetMessage
			{
				CNT = netId
			};
			Send(msg);
			return msg;
		}
	}
}
