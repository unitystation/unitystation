using System.Collections;
using Mirror;

public class MatrixMoveNewPlayer: ClientMessage
{
	public uint MatrixMove;

	public override void Process()
	{
		// LoadNetworkObject returns bool, so it can be used to check if object is loaded correctly
		if (LoadNetworkObject(MatrixMove))
		{
			NetworkObject.GetComponent<MatrixMove>()?.UpdateNewPlayer(
				SentByPlayer.Connection);
		}
	}

	public static MatrixMoveNewPlayer Send(uint matrixMoveNetId)
	{
		MatrixMoveNewPlayer msg = new MatrixMoveNewPlayer
		{
			MatrixMove = matrixMoveNetId
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		MatrixMove = reader.ReadUInt32();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt32(MatrixMove);
	}
}