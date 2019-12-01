using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

public class TileChangeManager : NetworkBehaviour
{
	private MetaTileMap metaTileMap;

	private TileChangeList changeList = new TileChangeList();

	public Vector3IntEvent OnFloorOrPlatingRemoved = new Vector3IntEvent();

	private void Awake()
	{
		metaTileMap = GetComponentInChildren<MetaTileMap>();
	}

	public void InitServerSync(string data)
	{
		//Unpacking the data example (and then run action change)
		changeList = JsonUtility.FromJson<TileChangeList>(data);

		foreach (TileChangeEntry entry in changeList.List)
		{
			// load tile & apply
			if (entry.TileType.Equals(TileType.None))
			{
				InternalRemoveTile(entry.Position, entry.LayerType, entry.RemoveAll);
			}
			else
			{
				InternalUpdateTile(entry.Position, entry.TileType, entry.TileName);
			}
		}
	}

	[Server]
	public void NotifyPlayer (GameObject requestedBy)
	{
		if (changeList.List.Count > 0)
		{
			Logger.LogFormat("Request all updates: ", Category.TileMaps, requestedBy.name);
			string jsondata = JsonUtility.ToJson (changeList);

			TileChangesNewClientSync.Send(gameObject, requestedBy, jsondata);
		}
	}

	[Server]
	public void UpdateTile(Vector3Int cellPosition, TileType tileType, string tileName)
	{
		if (IsDifferent(cellPosition, tileType, tileName))
		{
			InternalUpdateTile(cellPosition, tileType, tileName);

			RpcUpdateTile(cellPosition, tileType, tileName);

			AddToChangeList(cellPosition, tileType:tileType, tileName:tileName);
		}
	}


	[Server]
	public void UpdateTile(Vector3Int cellPosition, LayerTile layerTile)
	{
		if (IsDifferent(cellPosition, layerTile))
		{
			InternalUpdateTile(cellPosition, layerTile);

			RpcUpdateTile(cellPosition, layerTile.TileType, layerTile.name);

			AddToChangeList(cellPosition, layerTile);
		}
	}

	[Server]
	public void RemoveTile( Vector3Int cellPosition )
	{
		foreach ( var layerType in metaTileMap.LayersKeys )
		{
			RemoveTile( cellPosition, layerType );
		}
	}

	[Server]
	public bool RemoveTile(Vector3Int cellPosition, LayerType layerType)
	{
		if(metaTileMap.HasTile(cellPosition, layerType, true))
		{
			InternalRemoveTile(cellPosition, layerType, false);

			RpcRemoveTile(cellPosition, layerType, false);

			AddToChangeList(cellPosition, layerType);

			if ( layerType == LayerType.Floors || layerType == LayerType.Base )
			{
				OnFloorOrPlatingRemoved.Invoke( cellPosition );
			}
			return true;
		}

		return false;
	}

	[Server]
	public void RemoveEffect(Vector3Int cellPosition, LayerType layerType)
	{
		cellPosition.z = -1;

		if (metaTileMap.HasTile(cellPosition, layerType, true))
		{
			InternalRemoveTile(cellPosition, layerType, true);

			RpcRemoveTile(cellPosition, layerType, true);

			AddToChangeList(cellPosition, layerType, removeAll:true);
		}
	}

	[ClientRpc]
	private void RpcRemoveTile(Vector3 position, LayerType layerType, bool onlyRemoveEffect)
	{
		if ( isServer )
		{
			return;
		}
		InternalRemoveTile(position, layerType, onlyRemoveEffect);
	}

	private void InternalRemoveTile(Vector3 position, LayerType layerType, bool onlyRemoveEffect)
	{
		if (onlyRemoveEffect)
		{
			position.z = -1;
		}

		Vector3Int p = position.RoundToInt();

		metaTileMap.RemoveTile(p, layerType, !onlyRemoveEffect);
	}

	[ClientRpc]
	private void RpcUpdateTile(Vector3 position, TileType tileType, string tileName)
	{
		if ( isServer )
		{
			return;
		}
		InternalUpdateTile(position, tileType, tileName);
	}

	private void InternalUpdateTile(Vector3 position, TileType tileType, string tileName)
	{
		LayerTile layerTile = TileManager.GetTile(tileType, tileName);

		if (tileType == TileType.WindowDamaged)
		{
			position.z -= 1;
		}

		Vector3Int p = position.RoundToInt();

		metaTileMap.SetTile(p, layerTile);
	}

	private void InternalUpdateTile(Vector3 position, LayerTile layerTile)
	{
		if (layerTile.TileType == TileType.WindowDamaged)
		{
			position.z -= 1;
		}

		Vector3Int p = position.RoundToInt();

		metaTileMap.SetTile(p, layerTile);
	}

	private void AddToChangeList(Vector3 position, LayerType layerType=LayerType.None, TileType tileType=TileType.None, string tileName=null, bool removeAll = false)
	{
		changeList.List.Add(new TileChangeEntry()
		{
			Position = position,
			LayerType = layerType,
			TileType = tileType,
			TileName = tileName,
			RemoveAll = removeAll
		});
	}

	private void AddToChangeList(Vector3 position, LayerTile layerTile, LayerType layerType=LayerType.None, bool removeAll = false)
	{
		changeList.List.Add(new TileChangeEntry()
		{
			Position = position,
			LayerType = layerType,
			TileType = layerTile.TileType,
			TileName = layerTile.name,
			RemoveAll = removeAll
		});
	}

	private bool IsDifferent(Vector3Int position, TileType tileType, string tileName)
	{
		LayerTile layerTile = TileManager.GetTile(tileType, tileName);

		return metaTileMap.GetTile(position, layerTile.LayerType) != layerTile;
	}

	private bool IsDifferent(Vector3Int position, LayerTile layerTile)
	{
		return metaTileMap.GetTile(position, layerTile.LayerType) != layerTile;
	}
}

[System.Serializable]
public class TileChangeList
{
	public List<TileChangeEntry> List = new List<TileChangeEntry>();
}

[System.Serializable]
public class TileChangeEntry
{
	public Vector3 Position;

	public TileType TileType;

	public LayerType LayerType;

	public string TileName;

	public bool RemoveAll;
}

public class TileChangeEvent : UnityEvent<Vector3Int, GenericTile> { }