using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;

public class TileChangeManager : NetworkBehaviour
{
	private MetaTileMap metaTileMap;

	private void Awake()
	{
		metaTileMap = GetComponent<MetaTileMap>();
	}

	[Server]
	public void UpdateTile(Vector3Int position, TileType tileType, string tileName)
	{
		RpcUpdateTile(position, tileType, tileName);
	}

	[Server]
	public void RemoveTile(Vector3Int position, LayerType layerType)
	{
		RpcRemoveTile(position, layerType, false);
	}

	[Server]
	public void RemoveEffect(Vector3Int position, LayerType layerType)
	{
		RpcRemoveTile(position, layerType, true);
	}

	[ClientRpc]
	private void RpcRemoveTile(Vector3Int position, LayerType layerType, bool onlyRemoveEffect)
	{
		if (onlyRemoveEffect)
		{
			position.z = -1;
		}

		metaTileMap.RemoveTile(position, layerType, !onlyRemoveEffect);
	}

	[ClientRpc]
	private void RpcUpdateTile(Vector3Int position, TileType tileType, string tileName)
	{
		LayerTile layerTile = TileManager.GetTile(tileType, tileName);

		if (tileType == TileType.Damaged)
		{
			position.z -= 1;
		}

		metaTileMap.SetTile(position, layerTile);
	}
}