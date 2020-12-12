using UnityEngine;
using Mirror;

public class UpdateTileMessage : ServerMessage
{
	public Vector3Int Position;
	public TileType TileType;
	public string TileName;
	public Matrix4x4 TransformMatrix;
	public Color Colour;
	public uint TileChangeManager;

	public override void Process()
	{
		LoadNetworkObject(TileChangeManager);
		var tileChangerManager = NetworkObject.GetComponent<TileChangeManager>();
		tileChangerManager.InternalUpdateTile(Position, TileType, TileName, TransformMatrix, Colour);
	}

	public static UpdateTileMessage Send(uint tileChangeManagerNetID, Vector3Int position, TileType tileType, string tileName,
		Matrix4x4 transformMatrix, Color colour)
	{
		UpdateTileMessage msg = new UpdateTileMessage
		{
			Position = position,
			TileType = tileType,
			TileName = tileName,
			TransformMatrix = transformMatrix,
			Colour = colour,
			TileChangeManager = tileChangeManagerNetID
		};
		msg.SendToAll();
		return msg;
	}
}