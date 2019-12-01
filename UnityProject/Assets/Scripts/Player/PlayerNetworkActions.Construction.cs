using UnityEngine;
using Mirror;

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
		var floorTile = tileToPlace.GetComponent<PlaceableTile>();

		tm.UpdateTile(Vector3Int.RoundToInt(cellPos), floorTile.LayerTile);

		Inventory.ServerDespawn(tileToPlace.GetComponent<Pickupable>().ItemSlot);
		RpcPlayerSoundAtPos("Deconstruct", transform.position, false);
	}

	[ClientRpc]
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