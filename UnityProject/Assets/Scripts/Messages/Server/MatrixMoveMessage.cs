using System.Collections;
using UnityEngine;
using Mirror;

public class MatrixMoveMessage : ServerMessage
{
	public MatrixState State;
	public uint Matrix;

	///To be run on client
	public override void Process()
	{
		LoadNetworkObject(Matrix);

		//Sometimes NetworkObject is gone because of game ending or just before exit
		if (NetworkObject != null) {
			var matrixMove = NetworkObject.GetComponent<MatrixMove>();
			matrixMove.UpdateClientState(State);
		}
	}

	public static MatrixMoveMessage Send(NetworkConnection recipient, GameObject matrix, MatrixState state)
	{
		var msg = new MatrixMoveMessage
		{
			Matrix = matrix != null ? matrix.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
			State = state,
		};
		msg.SendTo(recipient);
		return msg;
	}

	public static MatrixMoveMessage SendToAll(GameObject matrix, MatrixState state)
	{
		var msg = new MatrixMoveMessage
		{
			Matrix = matrix != null ? matrix.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
			State = state,
		};
		msg.SendToAll();
		return msg;
	}
}