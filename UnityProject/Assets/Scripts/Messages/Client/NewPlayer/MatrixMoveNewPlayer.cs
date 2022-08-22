using Mirror;
using Shuttles;

namespace Messages.Client.NewPlayer
{
	public class MatrixMoveNewPlayer : ClientMessage<MatrixMoveNewPlayer.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint MatrixSyncNetId;
		}

		public override void Process(NetMessage msg)
		{
			// LoadNetworkObject returns bool, so it can be used to check if object is loaded correctly
			if (LoadNetworkObject(msg.MatrixSyncNetId))
			{
				NetworkObject.GetComponent<MatrixSync>().OrNull()?.MatrixMove.OrNull()?.UpdateNewPlayer(
					SentByPlayer.Connection);
			}
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
