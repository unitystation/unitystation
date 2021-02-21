using System.Collections;
using UnityEngine;
using Mirror;

///     Tells client to
public class MatrixMoveMessage : ServerMessage
{
	public struct MatrixMoveMessageNetMessage : NetworkMessage
	{
		public MatrixState State;
		public uint Matrix;

		public override string ToString()
		{
			return $"[MatrixMoveMessage {State}]";
		}
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public MatrixMoveMessageNetMessage IgnoreMe;

	//Reset client's prediction queue
	//public bool ResetQueue;

	///To be run on client
	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as MatrixMoveMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		LoadNetworkObject(newMsg.Matrix);

		//Sometimes NetworkObject is gone because of game ending or just before exit
		if (NetworkObject != null) {
			var matrixMove = NetworkObject.GetComponent<MatrixMove>();
			matrixMove.UpdateClientState(newMsg.State);
		}
	}

	public static MatrixMoveMessageNetMessage Send(NetworkConnection recipient, GameObject matrix, MatrixState state)
	{
		var msg = new MatrixMoveMessageNetMessage
		{
			Matrix = matrix != null ? matrix.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
			State = state,
		};
		new MatrixMoveMessage().SendTo(recipient, msg);
		return msg;
	}

	public static MatrixMoveMessageNetMessage SendToAll(GameObject matrix, MatrixState state)
	{
		var msg = new MatrixMoveMessageNetMessage
		{
			Matrix = matrix != null ? matrix.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
			State = state,
		};
		new MatrixMoveMessage().SendToAll(msg);
		return msg;
	}
}