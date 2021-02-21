using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RemoveTileMessage : ServerMessage
{
	public class RemoveTileMessageNetMessage : NetworkMessage
	{
		public Vector3 Position;
		public LayerType LayerType;
		public bool RemoveAll;
		public uint TileChangeManager;
	}

	public static List<delayedData> DelayedStuff = new List<delayedData>();

	public struct delayedData
	{
		public Vector3 Position;
		public LayerType LayerType;
		public bool RemoveAll;
		public uint TileChangeManager;
		public delayedData(Vector3 inPosition, LayerType inLayerType, bool inRemoveAll, uint inTileChangeManager)
		{
			Position = inPosition;
			LayerType = inLayerType;
			RemoveAll = inRemoveAll;
			TileChangeManager = inTileChangeManager;
		}
	}
	public override void Process<T>(T msg)
	{
		var newMsg = msg as RemoveTileMessageNetMessage;
		if(newMsg == null) return;

		LoadNetworkObject(newMsg.TileChangeManager);
		if (NetworkObject == null)
		{
			DelayedStuff.Add(new delayedData(newMsg.Position, newMsg.LayerType, newMsg.RemoveAll, newMsg.TileChangeManager));
		}
		else
		{
			var tileChangerManager = NetworkObject.GetComponent<TileChangeManager>();
			tileChangerManager.InternalRemoveTile(newMsg.Position, newMsg.LayerType, newMsg.RemoveAll);
			TryDoNotDoneTiles();
		}
	}

	public void TryDoNotDoneTiles()
	{
		for (int i = 0; i < DelayedStuff.Count; i++)
		{
			NetworkObject = null;
			LoadNetworkObject(DelayedStuff[i].TileChangeManager);
			if (NetworkObject != null)
			{
				var tileChangerManager = NetworkObject.GetComponent<TileChangeManager>();
				tileChangerManager.InternalRemoveTile(DelayedStuff[i].Position, DelayedStuff[i].LayerType, DelayedStuff[i].RemoveAll);
				DelayedStuff.RemoveAt(i);
				i--;
			}
		}
	}

	public static RemoveTileMessageNetMessage Send(uint tileChangeManagerNetID, Vector3 position, LayerType layerType, bool removeAll)
	{
		RemoveTileMessageNetMessage msg = new RemoveTileMessageNetMessage
		{
			Position = position,
			LayerType = layerType,
			RemoveAll = removeAll,
			TileChangeManager = tileChangeManagerNetID
		};
		new RemoveTileMessage().SendToAll(msg);
		return msg;
	}
}