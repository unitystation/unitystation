using System.Collections;
using PlayGroup;
using UnityEngine;
using UnityEngine.Networking;

///     Tells client to 
public class MatrixMoveMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.MatrixMoveMessage;
	public MatrixState State;
	public NetworkInstanceId Matrix;
	//Reset client's prediction queue
//	public bool ResetQueue;

	///To be run on client
	public override IEnumerator Process()
	{
//		Debug.Log("Processed " + ToString());
		yield return WaitFor(Matrix);
		var matrixMove = NetworkObject.GetComponent<MatrixMove>();
		matrixMove.UpdateClientState(State);
	}

	public static MatrixMoveMessage Send(GameObject recipient, GameObject matrix, MatrixState state)
	{
		var msg = new MatrixMoveMessage
		{
			Matrix = matrix != null ? matrix.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			State = state,
		};
		msg.SendTo(recipient);
		return msg;
	}

	public static MatrixMoveMessage SendToAll(GameObject matrix, MatrixState state)
	{
		var msg = new MatrixMoveMessage
		{
			Matrix = matrix != null ? matrix.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			State = state,
		};
		msg.SendToAll();
		return msg;
	}

	public override string ToString()
	{
		return $"[MatrixMoveMessage {State}]";
	}
}