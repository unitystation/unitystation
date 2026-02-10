using Mirror;

namespace US13.Messages.Client.NewPlayer
{
	public class TileChangeNewPlayer : ClientMessage<TileChangeNewPlayer.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint MatrixSyncNetId;
		}

		public override void Process(NetMessage msg)
		{
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
