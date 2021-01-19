using UnityEngine;
using Mirror;

public class RemoveTileMessage : ServerMessage
{
	public Vector3 Position;
	public LayerType LayerType;
	public bool RemoveAll;
	public uint TileChangeManager;

	public override void Process()
	{
		LoadNetworkObject(TileChangeManager);
		var tileChangerManager = NetworkObject.GetComponent<TileChangeManager>();
		tileChangerManager.InternalRemoveTile(Position, LayerType, RemoveAll);
	}

	public static RemoveTileMessage Send(uint tileChangeManagerNetID, Vector3 position, LayerType layerType, bool removeAll)
	{
		RemoveTileMessage msg = new RemoveTileMessage
		{
			Position = position,
			LayerType = layerType,
			RemoveAll = removeAll,
			TileChangeManager = tileChangeManagerNetID
		};
		msg.SendToAll();
		return msg;
	}
}