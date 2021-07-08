using Mirror;

namespace Messages.Client.NewPlayer
{
	public class TileChangeNewPlayer : ClientMessage<TileChangeNewPlayer.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint MatrixSyncNetId;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.MatrixSyncNetId);

			if (NetworkObject == null)
			{
				Logger.LogError("Failed to load matrix sync for new player", Category.Matrix);
				return;
			}

			NetworkObject.transform.parent.GetComponent<TileChangeManager>().UpdateNewPlayer(
				SentByPlayer.Connection);
		}

		public static NetMessage Send(uint matrixSyncNetId)
		{
			NetMessage msg = new NetMessage
			{
				MatrixSyncNetId = matrixSyncNetId
			};

			Send(msg);
			return msg;
		}
	}
}
