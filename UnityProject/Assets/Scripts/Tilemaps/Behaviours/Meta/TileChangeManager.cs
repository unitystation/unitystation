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

	private TileChangeList changeList = new TileChangeList();

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
				RemoveTile(entry.Position, entry.LayerType, entry.RemoveAll);
			}
			else
			{
				UpdateTile(entry.Position, entry.TileType, entry.TileName);
			}
		}
	}

	[Server]
	public void NotifyPlayer (GameObject requestedBy)
	{
		if (changeList.List.Count > 0)
		{
			Debug.Log ("Request all updates: " + requestedBy.name);
			string jsondata = JsonUtility.ToJson (changeList);

			TileChangesNewClientSync.Send(gameObject, requestedBy, jsondata);
		}
	}

	[Server]
	public void UpdateTile(Vector3Int position, TileType tileType, string tileName)
	{
		if (IsDifferent(position, tileType, tileName))
		{
			RpcUpdateTile(position, tileType, tileName);

			AddToChangeList(position, tileType:tileType, tileName:tileName);
		}
	}

	[Server]
	public void RemoveTile(Vector3Int position, LayerType layerType)
	{
		if(metaTileMap.HasTile(position, layerType))
		{
			RpcRemoveTile(position, layerType, false);

			AddToChangeList(position, layerType);
		}
	}

	[Server]
	public void RemoveEffect(Vector3Int position, LayerType layerType)
	{
		position.z = -1;

		if (metaTileMap.HasTile(position, layerType))
		{
			RpcRemoveTile(position, layerType, true);

			AddToChangeList(position, layerType, removeAll:true);
		}
	}

	[ClientRpc]
	private void RpcRemoveTile(Vector3 position, LayerType layerType, bool onlyRemoveEffect)
	{
		RemoveTile(position, layerType, onlyRemoveEffect);
	}

	private void RemoveTile(Vector3 position, LayerType layerType, bool onlyRemoveEffect)
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
		UpdateTile(position, tileType, tileName);
	}

	private void UpdateTile(Vector3 position, TileType tileType, string tileName)
	{
		LayerTile layerTile = TileManager.GetTile(tileType, tileName);

		if (tileType == TileType.WindowDamaged)
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

	private bool IsDifferent(Vector3Int position, TileType tileType, string tileName)
	{
		LayerTile layerTile = TileManager.GetTile(tileType, tileName);

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