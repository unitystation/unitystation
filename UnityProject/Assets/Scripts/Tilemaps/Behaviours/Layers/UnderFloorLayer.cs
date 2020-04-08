using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using UnityEngine.Tilemaps;

/// <summary>
/// Used for stacking tiles Since thats what happens in the Underfloor stuff
/// </summary>
[ExecuteInEditMode]
public class UnderFloorLayer : Layer
{
	bool Initialised = false;

	public static bool LOk = false;

	//It is assumed that the tiles start at 1 and go down
	public Dictionary<Vector2, List<GenericTile>> TileStore = new Dictionary<Vector2, List<GenericTile>>();

	//public DodgyDictionary dodgyDictionary = new DodgyDictionary();

	private List<GenericTile> StoreList = new List<GenericTile>();

	public bool IsAnyTileHere(Vector2Int position2, GenericTile ToCheckFor = null)
	{
		if (!TileStore.ContainsKey(position2))
		{
			CalculateTile(position2);
		}

		if (TileStore.ContainsKey(position2))
		{
			foreach (var Tile in TileStore[position2])
			{
				if (Tile != null)
				{
					return true;
				}
			}
		}

		return false;
	}

	public override void SetTile(Vector3Int position, GenericTile tile, Matrix4x4 transformMatrix)
	{
		var isServer = false;
		if (CustomNetworkManager.Instance != null)
		{
			isServer = CustomNetworkManager.Instance._isServer;
		}
		else
		{
			if (!Application.isPlaying) isServer = true;
		}

		if (isServer)
		{
			//##d
			Vector2Int position2 = position.To2Int();
			if (!TileStore.ContainsKey(position2))
			{
				CalculateTile(position2);
			}

			Vector3Int position3 = new Vector3Int(position.x, position.y, position.z);
			if (TileStore.ContainsKey(position2))
			{
				//Logger.Log(" contain key");
				if (TileStore[position2].Contains(null)) //for the Cleared entries
				{
					//Logger.Log(" null");
					int Index = TileStore[position2].IndexOf(null);
					position3.z = -Index + 1;
					TileStore[position2][Index] = tile;
				}
				else
				{
					//Logger.Log("no null" + position3) ;
					position3.z = (1 - TileStore[position2].Count);
					TileStore[position2].Add(tile);
					//Logger.Log("no null" + position3);
				}
			}
			else
			{
				//Logger.Log("Not contain key > " + position3);
				TileStore[position2] = new List<GenericTile>();
				TileStore[position2].Add(tile);

				position3.z = 1;
				//Logger.Log("Not contain key > " + position3);
			}

			//Logger.Log("position3 > " + position3 + " tile  > " + tile);
			if (Application.isPlaying)  matrix.TileChangeManager.UpdateTile(position3, tile as BasicTile, false); //##d
			base.SetTile(position3, tile, transformMatrix);
		}
		else
		{
			base.SetTile(position, tile, transformMatrix);
		}
	}

	public void CalculateTile(Vector2Int position2)
	{
		var Vector3 = new Vector3Int(position2.x, position2.y, 0);
		GenericTile genericTile = null;
		for (int i = 0; i < 25 + 1; i++)
		{
			Vector3.z = (-i) + 1;
			genericTile = tilemap.GetTile(Vector3) as GenericTile;
			StoreList.Add(genericTile);
		}

		int LastLocation = 0;
		for (int i = 0; i < StoreList.Count - 1; i++)
		{
			if (StoreList[i] != null)
			{
				LastLocation = i;
			}
		}

		TileStore[position2] = new List<GenericTile>(LastLocation + 1);

		for (int i = 0; i < LastLocation + 1; i++)
		{
			TileStore[position2].Add(StoreList[i]);
		}

		StoreList.Clear();
	}


	public void RemoveSpecifiedTile(Vector2Int position, GenericTile tile)
	{
		if (!TileStore.ContainsKey(position))
		{
			CalculateTile(position);
		}

		if (TileStore.ContainsKey(position))
		{
			if (TileStore[position].Contains(tile))
			{
				int Index = TileStore[position].IndexOf(tile);
				matrix.TileChangeManager.RemoveTile(new Vector3Int(position.x, position.y, (-Index) + 1),
					LayerType.Underfloor,
					false);
				TileStore[position][Index] = null;
			}
		}
		else
		{
			Logger.LogWarning(position + "Was not present in the underfloor layer Trying to remove" + tile);
		}
	}
}