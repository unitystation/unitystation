using UnityEngine;
using UnityEngine.Networking;

public partial class PlayerNetworkActions : NetworkBehaviour
{
	[Command]
	public void CmdCrowBarRemoveFloorTile(GameObject originator,
		LayerType layer, Vector3 cellPos, Vector3 worldPos)
	{
		TileChangeManager tm = originator.GetComponentInParent<TileChangeManager>();
		if (tm == null)
		{
			Logger.LogError("TileChangeManager not found", Category.Construction);
			return;
		}

		tm.RemoveTile(Vector3Int.RoundToInt(cellPos), layer);

		CraftingManager.Construction.SpawnFloorTile(Vector3Int.RoundToInt(worldPos), null); // TODO parent ?
		RpcPlayerSoundAtPos("Crowbar", transform.position, true);
	}

	[Command]
	public void CmdPlaceFloorTile(GameObject originator,
		Vector3 cellPos, GameObject tileToPlace)
	{
		TileChangeManager tm = originator.GetComponentInParent<TileChangeManager>();
		if (tm == null)
		{
			Logger.LogError("TileChangeManager not found", Category.Construction);
			return;
		}
		UniFloorTile floorTile = tileToPlace.GetComponent<UniFloorTile>();

		tm.UpdateTile(Vector3Int.RoundToInt(cellPos), TileType.Floor, floorTile.FloorTileType );

		Consume(tileToPlace);
		RpcPlayerSoundAtPos("Deconstruct", transform.position, false);
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