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
		metaTileMap = GetComponentInChildren<MetaTileMap>();
	}

	[Server]
	public void UpdateTile(Vector3Int position, TileType tileType, string tileName)
	{
		if (IsDifferent(position, tileType, tileName))
		{
			RpcUpdateTile(position, tileType, tileName);
		}
	}

	[Server]
	public void RemoveTile(Vector3Int position, LayerType layerType)
	{
		if(metaTileMap.HasTile(position, layerType))
		{
			RpcRemoveTile(position, layerType, false);
		}
	}

	[Server]
	public void RemoveEffect(Vector3Int position, LayerType layerType)
	{
		position.z = -1;

		if (metaTileMap.HasTile(position, layerType))
		{
			RpcRemoveTile(position, layerType, true);
		}
	}

	[ClientRpc]
	private void RpcRemoveTile(Vector3 position, LayerType layerType, bool onlyRemoveEffect)
	{
		if (onlyRemoveEffect)
		{
			position.z = -1;
		}

		metaTileMap.RemoveTile(position.RoundToInt(), layerType, !onlyRemoveEffect);
	}

	[ClientRpc]
	private void RpcUpdateTile(Vector3 position, TileType tileType, string tileName)
	{
		LayerTile layerTile = TileManager.GetTile(tileType, tileName);

		if (tileType == TileType.Damaged)
		{
			position.z -= 1;
		}

		metaTileMap.SetTile(position.RoundToInt(), layerTile);
	}

	private bool IsDifferent(Vector3Int position, TileType tileType, string tileName)
	{
		LayerTile layerTile = TileManager.GetTile(tileType, tileName);

		return metaTileMap.GetTile(position, layerTile.LayerType) != layerTile;
	}
}