using Logs;
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
				Loggy.LogError("Failed to load matrix sync for new player", Category.Matrix);
				return;
			}

			var parent = NetworkObject.transform.parent;
			parent.GetComponent<TileChangeManager>().UpdateNewPlayer(
				SentByPlayer.Connection);

			parent.GetComponentInChildren<MetaDataLayer>().UpdateNewPlayer(
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
