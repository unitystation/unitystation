using System;
using System.Collections.Generic;
using Pipes;
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
		if (CustomNetworkManager.Instance._isServer)
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

							TileStore[(Vector2Int) localPlace].Add(getTile);

							var electricalCableTile = getTile as ElectricalCableTile;
							if (electricalCableTile != null)
							{
								matrix.AddElectricalNode(new Vector3Int(n, p, localPlace.z), electricalCableTile);
							}

							var PipeTile = getTile as PipeTile;
							if (PipeTile != null)
							{
								PipeTile.InitialiseNode(new Vector3Int(n, p, localPlace.z), matrix);
							}
						}
					}
				}
			}
		}

		UnderFloorUtilitiesInitialised = true;
	}

	public bool UnderFloorUtilitiesInitialised { get; private set; } = false;

	public T GetFirstTileByType<T>(Vector3Int position) where T : LayerTile
	{
		if (!TileStore.ContainsKey((Vector2Int)position)) return default;

		foreach (LayerTile Tile in TileStore[(Vector2Int)position])
		{
			if (Tile is T) return Tile as T;
		}

		return default;
	}

	public IEnumerable<T> GetAllTilesByType<T>(Vector3Int position) where T : LayerTile
	{
		List<T> tiles = new List<T>();

		if (!TileStore.ContainsKey((Vector2Int)position)) return tiles;

		foreach (LayerTile Tile in TileStore[(Vector2Int)position])
		{
			if (Tile is T) tiles.Add(Tile as T);
		}

		return tiles;
	}


	public override LayerTile GetTile(Vector3Int position)
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			if (TileStore.ContainsKey((Vector2Int) position) == false)
			{
				SetupNode(position);
			}

			foreach (var Tile in TileStore[(Vector2Int) position])
			{
				return Tile;
			}
		}
		else
		{
			for (int i = 0; i < 50; i++)
			{
				var localPlace = position;
				localPlace.z = -i + 1;
				var getTile = tilemap.GetTile(localPlace) as LayerTile;
				if (getTile != null)
				{
					return getTile;
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Get tile using Z position instead of searching through the Z levels 
	/// </summary>
	public LayerTile GetTileUsingZ(Vector3Int position)
	{
		var getTile = tilemap.GetTile(position) as LayerTile;
		if (getTile != null)
		{
			return getTile;
		}

		return null;
	}

	public override void SetTile(Vector3Int position, GenericTile tile, Matrix4x4 transformMatrix, Color color)
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
				SetupNode(position);
			}

			int index = FindFirstEmpty(TileStore[position2]);
			if (index < 0)
			{
				position.z = 1 - TileStore[position2].Count;
				TileStore[position2].Add((LayerTile) tile);
			}
			else
			{
				position.z = 1 - index;
				TileStore[position2][index] = (LayerTile) tile;
			}


			if (Application.isPlaying)
			{
				matrix.TileChangeManager.UnderfloorUpdateTile(position, tile as BasicTile, transformMatrix, color);
			}
			else
			{
				if (position.z < -49)
				{
					Logger.LogError(
						"Tile has reached maximum Meta data system depth This could be from accidental placing of multiple tiles",
						Category.Editor);
					return;
				}
			}

			base.SetTile(position, tile, transformMatrix, color);
		}
		else
		{
			base.SetTile(position, tile, transformMatrix, color);
		}
	}



	private int FindFirstEmpty(List<LayerTile> LookThroughList)
	{
		for (var i = 0; i < LookThroughList.Count; i++)
		{
			if (LookThroughList[i] == null)
			{
				return (i);
			}
		}

		return (-1);
	}

	public override void RemoveTile(Vector3Int position, bool removeAll = false)
	{
		if (Application.isPlaying)
		{
			if (TileStore.ContainsKey((Vector2Int) position))
			{
				if (TileStore[(Vector2Int) position].Count > Math.Abs(position.z - 1))
				{
					TileStore[(Vector2Int) position][Math.Abs(position.z - 1)] = null;
				}
			}

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

		if (TileStore.ContainsKey((Vector2Int) position))
		{
			TileStore[(Vector2Int) position] = new List<LayerTile>();
		}
	}

	public Color GetColour(Vector3Int position, LayerTile tile)
	{
		if (!TileStore.ContainsKey((Vector2Int) position)) return Color.white;
		if (TileStore.ContainsKey((Vector2Int) position))
		{
			if (TileStore[(Vector2Int) position].Contains(tile))
			{
				int index = TileStore[(Vector2Int) position].IndexOf(tile);
				return (tilemap.GetColor(new Vector3Int(position.x, position.y, (-index) + 1)));
			}
		}

		return Color.white;
	}

	public void SetupNode(Vector3Int position)
	{
		Vector2Int position2 = position.To2Int();
		for (int i = 0; i < 50; i++)
		{
			var localPlace = position;
			localPlace.z = -i + 1;
			var getTile = tilemap.GetTile(localPlace) as LayerTile;
			if (getTile != null)
			{
				if (!TileStore.ContainsKey((Vector2Int) localPlace))
				{
					TileStore.Add((Vector2Int) localPlace, new List<LayerTile>());
				}

				TileStore[(Vector2Int) localPlace].Add(getTile);
			}
		}

		if (!TileStore.ContainsKey(position2))
		{
			TileStore[position2] = new List<LayerTile>();
		}
	}

	public Matrix4x4 GetMatrix4x4(Vector3Int position, LayerTile tile)
	{
		if (!TileStore.ContainsKey((Vector2Int) position)) return Matrix4x4.identity;
		if (TileStore.ContainsKey((Vector2Int) position))
		{
			if (TileStore[(Vector2Int) position].Contains(tile))
			{
				int index = TileStore[(Vector2Int) position].IndexOf(tile);
				return (tilemap.GetTransformMatrix(new Vector3Int(position.x, position.y, (-index) + 1)));
			}
		}

		return Matrix4x4.identity;
	}

	public void RemoveSpecifiedTile(Vector3Int position, LayerTile tile)
	{
		if (!TileStore.ContainsKey((Vector2Int) position)) return;

		if (TileStore.ContainsKey((Vector2Int) position))
		{
			if (TileStore[(Vector2Int) position].Contains(tile))
			{
				int index = TileStore[(Vector2Int) position].IndexOf(tile);
				matrix.TileChangeManager.RemoveTile(new Vector3Int(position.x, position.y, (-index) + 1),
					LayerType.Underfloor,
					false);
				TileStore[(Vector2Int) position][index] = null;
			}
		}
		else
		{
			Logger.LogWarning(position + "Was not present in the underfloor layer Trying to remove" + tile);
		}
	}
}
