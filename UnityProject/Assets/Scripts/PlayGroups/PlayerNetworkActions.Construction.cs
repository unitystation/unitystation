using UnityEngine;
using UnityEngine.Networking;

public partial class PlayerNetworkActions : NetworkBehaviour
{
	[Command]
	public void CmdCrowBarRemoveFloorTile(GameObject tileChangeRoot,
		TileChangeLayer layer, Vector2 cellPos, Vector2 worldPos)
	{
		TileChangeManager tm = tileChangeRoot.GetComponent<TileChangeManager>();
		if (tm == null)
		{
			Logger.LogError("TileChangeManager not found", Category.Construction);
			return;
		}

		tm.RemoveTile(Vector2Int.RoundToInt(cellPos), layer);

		CraftingManager.Construction.SpawnFloorTile(Vector3Int.RoundToInt(worldPos), tm.ObjectParent.transform);
		RpcPlayerSoundAtPos("Crowbar", transform.position, true);
	}

	[Command]
	public void CmdPlaceFloorTile(GameObject tileChangeRoot,
		Vector2 cellPos, GameObject tileToPlace)
	{
		TileChangeManager tm = tileChangeRoot.GetComponent<TileChangeManager>();
		if (tm == null)
		{
			Logger.LogError("TileChangeManager not found", Category.Construction);
			return;
		}
		var floorTile = tileToPlace.GetComponent<UniFloorTile>();
		tm.ChangeTile(floorTile.FloorTileType,Vector2Int.RoundToInt(cellPos), TileChangeLayer.Floor);
		Consume(tileToPlace);
	}

	[ClientRpc(channel = 1)]
	public void RpcPlayerSoundAtPos(string soundName, Vector3 position, bool pitchvariations)
	{
		if (!pitchvariations)
		{
			SoundManager.PlayAtPosition(soundName, position);
		}
		else
		{
			SoundManager.PlayAtPosition(soundName, position, Random.Range(0.8f, 1.2f));
		}
	}

}