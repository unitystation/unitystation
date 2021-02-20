using System.Collections;
using Messages.Client;

public class MatrixMoveNewPlayer : ClientMessage
{
	public class MatrixMoveNewPlayerNetMessage : ActualMessage
	{
		public uint MatrixMove;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as MatrixMoveNewPlayerNetMessage;
		if(newMsg == null) return;

		// LoadNetworkObject returns bool, so it can be used to check if object is loaded correctly
		if (LoadNetworkObject(newMsg.MatrixMove))
		{
			NetworkObject.GetComponent<MatrixMove>()?.UpdateNewPlayer(
				SentByPlayer.Connection);
		}
	}

	public static MatrixMoveNewPlayerNetMessage Send(uint matrixMoveNetId)
	{
		MatrixMoveNewPlayerNetMessage msg = new MatrixMoveNewPlayerNetMessage
		{
			MatrixMove = matrixMoveNetId
		};
		new MatrixMoveNewPlayer().Send(msg);
		return msg;
	}
}
