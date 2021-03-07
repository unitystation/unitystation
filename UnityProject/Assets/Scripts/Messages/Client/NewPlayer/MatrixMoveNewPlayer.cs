using Mirror;

namespace Messages.Client.NewPlayer
{
	public class MatrixMoveNewPlayer : ClientMessage<MatrixMoveNewPlayer.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint MatrixMove;
		}

		public override void Process(NetMessage msg)
		{
			// LoadNetworkObject returns bool, so it can be used to check if object is loaded correctly
			if (LoadNetworkObject(msg.MatrixMove))
			{
				NetworkObject.GetComponent<MatrixMove>()?.UpdateNewPlayer(
					SentByPlayer.Connection);
			}
		}

		public static NetMessage Send(uint matrixMoveNetId)
		{
			NetMessage msg = new NetMessage
			{
				MatrixMove = matrixMoveNetId
			};

			Send(msg);
			return msg;
		}
	}
}
