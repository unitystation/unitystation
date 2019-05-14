using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class MetaTileMap : MonoBehaviour
{
	/// <summary>
	/// Use this dictionary only if performance isn't critical, otherwise try using arrays below
	/// </summary>
	public Dictionary<LayerType, Layer> Layers { get; private set; }

	//Using arrays for iteration speed
	private LayerType[] LayersKeys { get; set; }
	private Layer[] LayersValues { get; set; }
	/// <summary>
	/// Array of only layers that can ever contain solid stuff
	/// </summary>
	private Layer[] SolidLayersValues { get; set; }

	private void OnEnable()
	{
		Layers = new Dictionary<LayerType, Layer>();
		var layersKeys = new List<LayerType>();
		var layersValues = new List<Layer>();
		var solidLayersValues = new List<Layer>();

		foreach (Layer layer in GetComponentsInChildren<Layer>(true))
		{
			var type = layer.LayerType;
			Layers[type] = layer;
			layersKeys.Add(type);
			layersValues.Add(layer);
			if ( type != LayerType.Effects
			  && type != LayerType.None)
			{
				solidLayersValues.Add(layer);
			}
		}

		LayersKeys = layersKeys.ToArray();
		LayersValues = layersValues.ToArray();
		SolidLayersValues = solidLayersValues.ToArray();
	}

	public bool IsPassableAt(Vector3Int position, bool isServer)
	{
		return IsPassableAt(position, position, isServer);
	}

	public bool IsPassableAt(Vector3Int origin, Vector3Int to, bool isServer,
							 CollisionType collisionType = CollisionType.Player, bool inclPlayers = true, GameObject context = null)
	{
		Vector3Int toX = new Vector3Int(to.x, origin.y, origin.z);
		Vector3Int toY = new Vector3Int(origin.x, to.y, origin.z);

		return _IsPassableAt(origin, toX, isServer, collisionType, inclPlayers, context) && _IsPassableAt(toX, to, isServer, collisionType, inclPlayers, context) ||
		       _IsPassableAt(origin, toY, isServer, collisionType, inclPlayers, context) && _IsPassableAt(toY, to, isServer, collisionType, inclPlayers, context);
	}

	private bool _IsPassableAt(Vector3Int origin, Vector3Int to, bool isServer,
							   CollisionType collisionType = CollisionType.Player, bool inclPlayers = true, GameObject context = null)
	{
		for (var i = 0; i < SolidLayersValues.Length; i++)
		{
			// Skip floor & base collisions if this is not a shuttle
			if (collisionType != CollisionType.Shuttle &&
				(SolidLayersValues[i].LayerType == LayerType.Floors || SolidLayersValues[i].LayerType == LayerType.Base ))
			{
				continue;
			}
			if (!SolidLayersValues[i].IsPassableAt(origin, to, isServer, collisionType: collisionType, inclPlayers: inclPlayers, context: context))
			{
				return false;
			}
		}

		return true;
	}

	public bool IsAtmosPassableAt(Vector3Int position, bool isServer)
	{
		return IsAtmosPassableAt(position, position, isServer);
	}

	public bool IsAtmosPassableAt(Vector3Int origin, Vector3Int to, bool isServer)
	{
		Vector3Int toX = new Vector3Int(to.x, origin.y, origin.z);
		Vector3Int toY = new Vector3Int(origin.x, to.y, origin.z);

		return _IsAtmosPassableAt(origin, toX, isServer) && _IsAtmosPassableAt(toX, to, isServer) ||
		       _IsAtmosPassableAt(origin, toY, isServer) && _IsAtmosPassableAt(toY, to, isServer);
	}

	private bool _IsAtmosPassableAt(Vector3Int origin, Vector3Int to, bool isServer)
	{
		for (var i = 0; i < LayersValues.Length; i++)
		{
			if (!LayersValues[i].IsAtmosPassableAt(origin, to, isServer))
			{
				return false;
			}
		}

		return true;
	}

	public bool IsSpaceAt(Vector3Int position, bool isServer)
	{
		for (var i = 0; i < LayersValues.Length; i++)
		{
			if (!LayersValues[i].IsSpaceAt(position, isServer))
			{
				return false;
			}
		}

		return true;
	}

	public void SetTile(Vector3Int position, LayerTile tile, Matrix4x4 transformMatrix)
	{
		Layers[tile.LayerType].SetTile(position, tile, transformMatrix);
	}

	public void SetTile(Vector3Int position, LayerTile tile)
	{
		Layers[tile.LayerType].SetTile(position, tile, Matrix4x4.identity);
	}

	public LayerTile GetTile(Vector3Int position, LayerType layerType)
	{
		Layer layer = null;
		Layers.TryGetValue(layerType, out layer);
		return layer ? Layers[layerType].GetTile(position) : null;
	}

	public LayerTile GetTile(Vector3Int position)
	{
		for (var i = 0; i < LayersValues.Length; i++)
		{
			LayerTile tile = LayersValues[i].GetTile(position);
			if (tile != null)
			{
				return tile;
			}
		}

		return null;
	}

	public bool IsEmptyAt(Vector3Int position, bool isServer)
	{
		for (var index = 0; index < LayersKeys.Length; index++)
		{
			LayerType layer = LayersKeys[index];
			if (layer != LayerType.Objects && HasTile(position, layer, isServer))
			{
				return false;
			}

			if (layer == LayerType.Objects)
			{
				foreach ( RegisterTile o in isServer ?
					((ObjectLayer) LayersValues[index]).ServerObjects.Get(position)
					: ((ObjectLayer) LayersValues[index]).ClientObjects.Get(position) )
				{
					if (!o.IsPassable(isServer))
					{
						return false;
					}
				}
			}
		}

		return true;
	}

	public bool IsNoGravityAt(Vector3Int position, bool isServer)
	{
		for (var i = 0; i < LayersKeys.Length; i++)
		{
			LayerType layer = LayersKeys[i];
			if (layer != LayerType.Objects && HasTile(position, layer, isServer))
			{
				return false;
			}
			if (layer == LayerType.Objects)
			{
				foreach ( RegisterTile o in isServer ?
					((ObjectLayer) LayersValues[i]).ServerObjects.Get(position)
					: ((ObjectLayer) LayersValues[i]).ClientObjects.Get(position) )
				{
					if ( o is RegisterObject )
					{
						PushPull pushPull = o.GetComponent<PushPull>();
						if ( !pushPull )
						{
							return o.IsPassable( isServer );
						}

						if ( pushPull.isNotPushable )
						{
							return false;
						}
					}
				}
			}
		}

		return true;
	}

	public bool IsEmptyAt(GameObject[] context, Vector3Int position, bool isServer)
	{
		for (var i1 = 0; i1 < LayersKeys.Length; i1++)
		{
			LayerType layer = LayersKeys[i1];
			if (layer != LayerType.Objects && HasTile(position, layer, isServer))
			{
				return false;
			}

			if (layer == LayerType.Objects)
			{
				foreach ( RegisterTile o in isServer ?
					((ObjectLayer) LayersValues[i1]).ServerObjects.Get(position)
					: ((ObjectLayer) LayersValues[i1]).ClientObjects.Get(position) )
				{
					if (!o.IsPassable(isServer))
					{
						bool isExcluded = false;
						for (var index = 0; index < context.Length; index++)
						{
							if (o.gameObject == context[index])
							{
								isExcluded = true;
								break;
							}
						}

						if (!isExcluded)
						{
							return false;
						}
					}
				}
			}
		}

		return true;
	}

	/// <summary>
	/// Cheap method to check if there's a tile
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public bool HasTile(Vector3Int position, bool isServer)
	{
		for (var i = 0; i < LayersValues.Length; i++)
		{
			if (LayersValues[i].HasTile(position, isServer))
			{
				return true;
			}
		}
		return false;
	}
	public bool HasTile(Vector3Int position, LayerType layerType, bool isServer)
	{
		return Layers[layerType].HasTile(position, isServer);
	}

	public void RemoveTile(Vector3Int position, LayerType refLayer)
	{
		for (var i = 0; i < LayersValues.Length; i++)
		{
			Layer layer = LayersValues[i];
			if (layer.LayerType < refLayer &&
			    !(refLayer == LayerType.Objects &&
			      layer.LayerType == LayerType.Floors) &&
			    refLayer != LayerType.Grills)
			{
				layer.RemoveTile(position);
			}
		}
	}

	public void RemoveTile(Vector3Int position, LayerType refLayer, bool removeAll = false)
	{
		Layers[refLayer].RemoveTile(position, removeAll);
	}

	public void ClearAllTiles()
	{
		for (var i = 0; i < LayersValues.Length; i++)
		{
			LayersValues[i].ClearAllTiles();
		}
	}

	public BoundsInt GetBounds()
	{
		Vector3Int minPosition = Vector3Int.one * int.MaxValue;
		Vector3Int maxPosition = Vector3Int.one * int.MinValue;

		for (var i = 0; i < LayersValues.Length; i++)
		{
			BoundsInt layerBounds = LayersValues[i].Bounds;

			minPosition = Vector3Int.Min(layerBounds.min, minPosition);
			maxPosition = Vector3Int.Max(layerBounds.max, maxPosition);
		}

		return new BoundsInt(minPosition, maxPosition - minPosition);
	}

	public Vector3Int WorldToCell(Vector3 worldPosition)
	{
		return Layers.First().Value.WorldToCell(worldPosition);
	}


#if UNITY_EDITOR
	public void SetPreviewTile(Vector3Int position, LayerTile tile, Matrix4x4 transformMatrix)
	{
		for (var i = 0; i < LayersValues.Length; i++)
		{
			Layer layer = LayersValues[i];
			if (layer.LayerType < tile.LayerType)
			{
				Layers[layer.LayerType].SetPreviewTile(position, LayerTile.EmptyTile, Matrix4x4.identity);
			}
		}

		if (!Layers.ContainsKey(tile.LayerType))
		{
			Logger.LogErrorFormat($"LAYER TYPE: {0} not found!", Category.TileMaps, tile.LayerType);
			return;
		}

		Layers[tile.LayerType].SetPreviewTile(position, tile, transformMatrix);
	}

	public void ClearPreview()
	{
		for (var i = 0; i < LayersValues.Length; i++)
		{
			LayersValues[i].ClearPreview();
		}
	}
#endif
}