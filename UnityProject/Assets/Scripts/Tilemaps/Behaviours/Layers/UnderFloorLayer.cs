using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used for stacking tiles Since thats what happens in the Underfloor stuff
/// </summary>
[ExecuteInEditMode]
public class UnderFloorLayer : Layer
{
	public Dictionary<Vector2Int, List<GenericTile>> TileStore = new Dictionary<Vector2Int, List<GenericTile>>();
	//It is assumed that the tiles start at 1 and go down

	public override void SetTile(Vector3Int position, GenericTile tile, Matrix4x4 transformMatrix)
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			Vector2Int position2 = position.To2Int();
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
			matrix.TileChangeManager.UpdateTile(position3, tile as BasicTile, false);
			base.SetTile(position3, tile, transformMatrix);
		}
		else
		{
			base.SetTile(position, tile, transformMatrix);
		}
	}

	public void RemoveSpecifiedTile(Vector2Int position, GenericTile tile)
	{
		if (TileStore.ContainsKey(position))
		{
			if (TileStore[position].Contains(tile))
			{
				int Index = TileStore[position].IndexOf(tile);
				matrix.TileChangeManager.RemoveTile(new Vector3Int(position.x, position.y, (-Index) + 1),
					LayerType.Underfloor,
					false);
				//RemoveTile(new Vector3Int(position.x, position.y, (-Index) + 1));
				TileStore[position][Index] = null;
			}
		}
		else
		{
			Logger.LogWarning(position + "Was not present in the underfloor layer Trying to remove" + tile);
		}
	}
}