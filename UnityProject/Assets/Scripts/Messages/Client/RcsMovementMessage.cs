using UnityEngine;

/// <summary>
/// Sends clients rcs move commands to the server
/// </summary>
public class RcsMovementMessage : ClientMessage
{
	public Vector2Int Direction;
	public uint MatrixMoveNetId;

	public override void Process()
	{
		LoadNetworkObject(MatrixMoveNetId);
		NetworkObject.GetComponent<MatrixMove>().ProcessRcsMoveRequest(SentByPlayer, Direction);
	}

	public static RcsMovementMessage Send(Vector2Int direction, uint matrixMoveId)
	{
		var msg = new RcsMovementMessage
		{
			Direction = direction,
			MatrixMoveNetId = matrixMoveId
		};
		msg.Send();
		return msg;
	}
}
