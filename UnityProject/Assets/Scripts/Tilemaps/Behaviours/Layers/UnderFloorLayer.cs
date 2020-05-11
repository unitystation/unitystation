using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used for stacking tiles Since thats what happens in the Underfloor stuff
/// </summary>
[ExecuteInEditMode]
public class UnderFloorLayer : Layer
{
	//It is assumed that the tiles start at 1 and go down
	private Dictionary<Vector2Int, List<LayerTile>> TileStore = new Dictionary<Vector2Int, List<LayerTile>>();

	public void InitialiseUnderFloorUtilities()
	{
		BoundsInt bounds = Tilemap.cellBounds;

		for (int n = bounds.xMin; n < bounds.xMax; n++)
		{
			for (int p = bounds.yMin; p < bounds.yMax; p++)
			{
				Vector3Int localPlace = (new Vector3Int(n, p, 0));

				for (int i = 0; i < 50; i++)
				{
					localPlace.z = -i + 1;
					var getTile = tilemap.GetTile(localPlace) as LayerTile;
					if (getTile != null)
					{
						if (!TileStore.ContainsKey((Vector2Int) localPlace))
						{
							TileStore.Add((Vector2Int) localPlace, new List<LayerTile>());
						}

						TileStore[(Vector2Int)localPlace].Add(getTile);

						var electricalCableTile = getTile as ElectricalCableTile;
						if (getTile != null)
						{
							matrix.AddElectricalNode(new Vector3Int(n, p, localPlace.z), electricalCableTile);
						}
					}
				}
			}
		}
	}

	public override LayerTile GetTile(Vector3Int position)
	{
		if (TileStore.ContainsKey((Vector2Int) position))
		{
			foreach (var Tile in TileStore[(Vector2Int) position])
			{
				if (Tile != null)
				{
					return Tile;
				}
			}
		}

		return null;
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

		//We do not want to duplicate under floor tiles if they are the same:
		if (TileStore.ContainsKey(position.To2Int()))
		{
			foreach (var l in TileStore[position.To2Int()])
			{
				if (l == tile)
				{
					//duplicate found aborting
					return;
				}
			}
		}

		if (isServer)
		{
			Vector2Int position2 = position.To2Int();
			if (!TileStore.ContainsKey(position2))
			{
				TileStore.Add(position2, new List<LayerTile>());
			}

			position.z = 1 - TileStore[position2].Count;
			TileStore[position2].Add((LayerTile)tile);

			if (Application.isPlaying)
			{
				matrix.TileChangeManager.UnderfloorUpdateTile(position, tile as BasicTile);
			}
			base.SetTile(position, tile, transformMatrix);
		}
		else
		{
			base.SetTile(position, tile, transformMatrix);
		}
	}

	public override void RemoveTile(Vector3Int position, bool removeAll = false)
	{
		if (Application.isPlaying)
		{
			base.RemoveTile(position, removeAll);
			return;
		}

		//This is for the erase tool at edit time:
		for (int i = 0; i < 50; i++)
		{
			position.z = -i + 1;
			var getTile = tilemap.GetTile(position);
			if (getTile != null)
			{
				base.RemoveTile(position, removeAll);
			}
		}
	}

	public void RemoveSpecifiedTile(Vector3Int position, LayerTile tile)
	{
		if (!TileStore.ContainsKey((Vector2Int)position)) return;

		if (TileStore.ContainsKey((Vector2Int)position))
		{
			if (TileStore[(Vector2Int)position].Contains(tile))
			{
				int index = TileStore[(Vector2Int)position].IndexOf(tile);
				matrix.TileChangeManager.RemoveTile(new Vector3Int(position.x, position.y, (-index) + 1),
					LayerType.Underfloor,
					false);
				TileStore[(Vector2Int)position][index] = null;
			}
		}
		else
		{
			Logger.LogWarning(position + "Was not present in the underfloor layer Trying to remove" + tile);
		}
	}
}