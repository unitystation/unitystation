using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

//long name I know. This is for syncing new clients when they join to all of the tile changes
public class TileChangesNewClientSync : ServerMessage
{
	//just a best guess, try increasing it until the message exceeds mirror's limit
	private static readonly int MAX_CHANGES_PER_MESSAGE = 20;

	public string data;
	public uint ManagerSubject;

	public override void Process()
	{
		//server doesn't need this message, it messes with its own tiles.
		if (CustomNetworkManager.IsServer) return;
		LoadNetworkObject(ManagerSubject);

		TileChangeManager tm = NetworkObject.GetComponent<TileChangeManager>();
		tm.InitServerSync(data);
	}

	public static void Send(GameObject managerSubject, NetworkConnection recipient, TileChangeList changeList)
	{
		if (changeList == null || changeList.List.Count == 0) return;

		foreach (var changeChunk in changeList.List.ToArray().Chunk(MAX_CHANGES_PER_MESSAGE).Select(TileChangeList.FromList))
		{
			foreach (var entry in changeChunk.List)
			{
				Logger.LogTraceFormat("Sending update for {0} layer {1}", Category.TileMaps, entry.Position,
					entry.LayerType);
			}

			string jsondata = JsonUtility.ToJson (changeChunk);

			TileChangesNewClientSync msg =
				new TileChangesNewClientSync
				{ManagerSubject = managerSubject.GetComponent<NetworkIdentity>().netId,
					data = jsondata
				};

			msg.SendTo(recipient);

		}
	}

	public override string ToString()
	{
		return string.Format("[Sync Tile ChangeData]");
	}
}
