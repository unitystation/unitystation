using System.Collections;
using Messages.Client;
using Mirror;

public class MatrixMoveNewPlayer : ClientMessage
{
	public struct MatrixMoveNewPlayerNetMessage : NetworkMessage
	{
		public uint MatrixMove;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public MatrixMoveNewPlayerNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as MatrixMoveNewPlayerNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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
