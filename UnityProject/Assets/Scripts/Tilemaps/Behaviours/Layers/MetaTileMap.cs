using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[ExecuteInEditMode]
public class MetaTileMap : MonoBehaviour
{
	/// <summary>
	/// Use this dictionary only if performance isn't critical, otherwise try using arrays below
	/// </summary>
	public Dictionary<LayerType, Layer> Layers { get; private set; }

	//Using arrays for iteration speed
	public LayerType[] LayersKeys { get; private set; }
	public Layer[] LayersValues { get; private set; }
	public ObjectLayer ObjectLayer { get; private set; }


	public List<Layer> ffLayersValues;
	/// <summary>
	/// Array of only layers that can ever contain solid stuff
	/// </summary>
	public Layer[] SolidLayersValues { get; private set; }
	/// <summary>
	/// Layers that contain TilemapDamage
	/// </summary>
	public Layer[] DamageableLayers { get; private set; }

	public float Resistance(Vector3Int cellPos, bool includeObjects = true)
	{
		float resistance = 0;
		foreach ( var damageableLayer in DamageableLayers )
		{
			resistance += damageableLayer.TilemapDamage.Integrity( cellPos );
		}

		if ( includeObjects && ObjectLayer )
		{
			resistance += ObjectLayer.GetObjectResistanceAt( cellPos, true );
		}
		return resistance;
	}

	private void OnEnable()
	{
		Layers = new Dictionary<LayerType, Layer>();
		var layersKeys = new List<LayerType>();
		var layersValues = new List<Layer>();
		var solidLayersValues = new List<Layer>();
		var damageableLayersValues = new List<Layer>();

		foreach (Layer layer in GetComponentsInChildren<Layer>(true))
		{
			var type = layer.LayerType;
			Layers[type] = layer;
			layersKeys.Add(type);
			layersValues.Add(layer);
			if (type != LayerType.Effects
			    && type != LayerType.None)
			{
				solidLayersValues.Add(layer);
			}

			if ( layer.GetComponent<TilemapDamage>() )
			{
				damageableLayersValues.Add( layer );
			}

			if ( layer.LayerType == LayerType.Objects )
			{
				ObjectLayer = layer as ObjectLayer;
			}
		}

		LayersKeys = layersKeys.ToArray();
		LayersValues = layersValues.ToArray();
		SolidLayersValues = solidLayersValues.ToArray();
		damageableLayersValues.Sort(( layerOne, layerTwo ) => layerOne.LayerType.GetOrder().CompareTo( layerTwo.LayerType.GetOrder() ) );
		DamageableLayers = damageableLayersValues.ToArray();
	}

	/// <summary>
	/// Apply damage to damageable layers, top to bottom.
	/// If tile gets destroyed, remaining damage is applied to the layer below
	/// Returns how much damage was absorbed
	/// </summary>
	public float ApplyDamage(Vector3Int cellPos, float damage, Vector3Int worldPos, AttackType attackType = AttackType.Melee)
	{
		float RemainingDamage = damage;
		foreach ( var damageableLayer in DamageableLayers )
		{
			if ( RemainingDamage <= 0f )
			{
				return (damage);
			}
			RemainingDamage -= damageableLayer.TilemapDamage.ApplyDamage(damage, attackType, worldPos);
		}

		if (RemainingDamage > damage)
		{
			return (damage);
		}
		return (damage - RemainingDamage);
	}

	public bool IsPassableAt(Vector3Int position, bool isServer)
	{
		return IsPassableAt(position, position, isServer);
	}

	public bool IsPassableAt(Vector3Int origin, Vector3Int to, bool isServer,
		CollisionType collisionType = CollisionType.Player, bool inclPlayers = true, GameObject context = null,
		List<LayerType> excludeLayers = null, List<TileType> excludeTiles = null)
	{
		Vector3Int toX = new Vector3Int(to.x, origin.y, origin.z);
		Vector3Int toY = new Vector3Int(origin.x, to.y, origin.z);

		return _IsPassableAt(origin, toX, isServer, collisionType, inclPlayers, context, excludeLayers, excludeTiles) &&
			   _IsPassableAt(toX, to, isServer, collisionType, inclPlayers, context, excludeLayers, excludeTiles) ||
			   _IsPassableAt(origin, toY, isServer, collisionType, inclPlayers, context, excludeLayers, excludeTiles) &&
			   _IsPassableAt(toY, to, isServer, collisionType, inclPlayers, context, excludeLayers, excludeTiles);
	}


	private bool _IsPassableAt(Vector3Int origin, Vector3Int to, bool isServer,
		CollisionType collisionType = CollisionType.Player, bool inclPlayers = true, GameObject context = null,
		List<LayerType> excludeLayers = null, List<TileType> excludeTiles = null)
	{
		for (var i = 0; i < SolidLayersValues.Length; i++)
		{
			// Skip floor & base collisions if this is not a shuttle
			if (collisionType != CollisionType.Shuttle &&
				(SolidLayersValues[i].LayerType == LayerType.Floors ||
				 SolidLayersValues[i].LayerType == LayerType.Base))
			{
				continue;
			}

			// Skip if the current tested layer is being excluded.
			if (excludeLayers != null && excludeLayers.Contains(SolidLayersValues[i].LayerType))
			{
				continue;
			}

			if (!SolidLayersValues[i].IsPassableAt(origin, to, isServer, collisionType: collisionType,
				inclPlayers: inclPlayers, context: context, excludeTiles))
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

	public bool IsTileTypeAt(Vector3Int position, bool isServer, TileType tileType)
	{
		for (var i = 0; i < LayersValues.Length; i++)
		{
			LayerTile tile = LayersValues[i].GetTile(position);
			if (tile != null && tile.TileType == tileType)
			{
				return true;
			}
		}

		return false;
	}

	public void SetTile(Vector3Int position, LayerTile tile, Matrix4x4? matrixTransform = null, Color? color = null)
	{
		if (Layers.TryGetValue(tile.LayerType, out var layer))
		{
			layer.SetTile(position, tile,
					matrixTransform.GetValueOrDefault(Matrix4x4.identity),
					color.GetValueOrDefault(Color.white));
		}
		else
		{
			LogMissingLayer(position, tile.LayerType);
		}
	}

	private void LogMissingLayer(Vector3Int position, LayerType layerType)
	{
		Logger.LogErrorFormat("Modifying tile at cellPos {0} for layer type {1} failed because matrix {2} " +
		                      "has no layer of that type. Please add this layer to this matrix in" +
		                      " the scene.", Category.TileMaps, position, layerType, name);
	}

	/// <summary>
	/// Gets the tile with the specified layer type at the specified world position
	/// </summary>
	/// <param name="worldPosition">world position to check</param>
	/// <param name="layerType"></param>
	/// <returns></returns>
	public LayerTile GetTileAtWorldPos(Vector3 worldPosition, LayerType layerType)
	{
		return GetTileAtWorldPos(worldPosition.RoundToInt(), layerType);
	}
	/// <summary>
	/// Gets the tile with the specified layer type at the specified world position
	/// </summary>
	/// <param name="worldPosition">world position to check</param>
	/// <param name="layerType"></param>
	/// <returns></returns>
	public LayerTile GetTileAtWorldPos(Vector3Int worldPosition, LayerType layerType)
	{
		var cellPos = WorldToCell(worldPosition);
		return GetTile(cellPos, layerType);
	}

	/// <summary>
	/// Gets the tile with the specified layer type at the specified cell position
	/// </summary>
	/// <param name="cellPosition">cell position within the tilemap to get the tile of. NOT the same
	/// as world position.</param>
	/// <param name="layerType"></param>
	/// <returns></returns>
	public LayerTile GetTile(Vector3Int cellPosition, LayerType layerType)
	{
		if (Layers.TryGetValue(layerType, out var layer))
		{
			return layer.GetTile(cellPosition);
		}
		else
		{
			LogMissingLayer(cellPosition, layerType);
		}

		return null;
	}


	/// <summary>
	/// used to check if the tiles are same for networking
	/// </summary>
	/// <param name="position"></param>
	/// <param name="layerTile"></param>
	/// <param name="transformMatrix"></param>
	/// <param name="color"></param>
	/// <returns></returns>
	public bool IsDifferent(Vector3Int cellPosition,LayerTile layerTile , LayerType layerType, Matrix4x4? transformMatrix = null,
		Color? color = null)
	{
		if (Layers.TryGetValue(layerType, out var layer))
		{
			return layer.IsDifferent(cellPosition, layerTile, transformMatrix,color );
		}
		else
		{
			LogMissingLayer(cellPosition, layerType);
		}

		return true;
	}

	/// <summary>
	/// Gets the topmost tile at the specified cell position
	/// </summary>
	/// <param name="cellPosition">cell position within the tilemap to get the tile of. NOT the same
	/// as world position.</param>
	/// <returns></returns>
	public LayerTile GetTile(Vector3Int cellPosition, bool ignoreEffectsLayer = false)
	{
		for (var i = 0; i < LayersValues.Length; i++)
		{
			LayerTile tile = LayersValues[i].GetTile(cellPosition);
			if (tile != null)
			{
				if (ignoreEffectsLayer && tile.LayerType == LayerType.Effects) continue;

				return tile;
			}
		}

		return null;
	}

	/// <summary>
	/// Gets the topmost tile at the specified cell position , Whilst ignoring the specified tiles in the ExcludedLayers
	/// </summary>
	/// <param name="cellPosition">cell position within the tilemap to get the tile of. NOT the same
	/// as world position.</param>
	/// <returns></returns>
	public LayerTile GetTile(Vector3Int cellPosition, LayerTypeSelection ExcludedLayers)
	{
		for (var i = 0; i < LayersValues.Length; i++)
		{
			LayerTile tile = LayersValues[i].GetTile(cellPosition);
			if (tile != null)
			{
				if (LTSUtil.IsLayerIn(ExcludedLayers,tile.LayerType)) continue;

				return tile;
			}
		}

		return null;
	}


	/// <summary>
	/// Checks if tile is empty of objects (only solid by default)
	/// </summary>
	public bool IsEmptyAt( Vector3Int position, bool isServer )
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
				foreach (RegisterTile o in isServer
					? ((ObjectLayer) LayersValues[index]).ServerObjects.Get(position)
					: ((ObjectLayer) LayersValues[index]).ClientObjects.Get(position))
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
				foreach (RegisterTile o in isServer
					? ((ObjectLayer) LayersValues[i]).ServerObjects.Get(position)
					: ((ObjectLayer) LayersValues[i]).ClientObjects.Get(position))
				{
					if (o is RegisterObject)
					{
						PushPull pushPull = o.GetComponent<PushPull>();
						if (pushPull == null && !o.IsPassable(isServer))
						{
							return false;
						}

						if (pushPull != null && pushPull.CausesGravity())
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
				foreach (RegisterTile o in isServer
					? ((ObjectLayer) LayersValues[i1]).ServerObjects.Get(position)
					: ((ObjectLayer) LayersValues[i1]).ClientObjects.Get(position))
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
		if (Layers.TryGetValue(layerType, out var layer))
		{
			return layer.HasTile(position, isServer);
		}
		else
		{
			LogMissingLayer(position, layerType);
		}

		return false;
	}

	public void RemoveTile(Vector3Int position, LayerType refLayer)
	{
		for (var i = 0; i < LayersValues.Length; i++)
		{
			Layer layer = LayersValues[i]; //layer.LayerType < refLayer &&
			//TODO: @Bod9001 What is the purpose of this strange conditional logic?
			if (
			    !(refLayer == LayerType.Objects && layer.LayerType == LayerType.Floors) &&
			    refLayer != LayerType.Grills)
			{
				layer.RemoveTile(position);
			}
		}
	}

	public void RemoveTile(Vector3Int position, LayerType refLayer, bool removeAll)
	{
		if (Layers.TryGetValue(refLayer, out var layer))
		{
			layer.RemoveTile(position, removeAll);
		}
		else
		{
			LogMissingLayer(position, refLayer);
		}
	}

	public void ClearAllTiles()
	{
		for (var i = 0; i < LayersValues.Length; i++)
		{
			LayersValues[i].ClearAllTiles();
		}
	}

	public Vector3 LocalToWorld( Vector3 localPos ) => LayersValues[0].LocalToWorld( localPos );
	public Vector3 CellToWorld( Vector3Int cellPos ) => LayersValues[0].CellToWorld( cellPos );
	public Vector3 WorldToLocal( Vector3 worldPos ) => LayersValues[0].WorldToLocal( worldPos );

	public BoundsInt GetWorldBounds()
	{
		var bounds = GetBounds();
		//???
		var min = CellToWorld(bounds.min);
		var max = CellToWorld(bounds.max);

		return new BoundsInt(min.RoundToInt(), (max - min).RoundToInt());
	}

	public BoundsInt GetBounds()
	{
		Vector3Int minPosition = Vector3Int.one * int.MaxValue;
		Vector3Int maxPosition = Vector3Int.one * int.MinValue;

		for (var i = 0; i < LayersValues.Length; i++)
		{
			BoundsInt layerBounds = LayersValues[i].Bounds;
			if (layerBounds.x == 0 && layerBounds.y == 0)
			{
				continue; // Has no tiles
			}

			minPosition = Vector3Int.Min(layerBounds.min, minPosition);
			maxPosition = Vector3Int.Max(layerBounds.max, maxPosition);
		}

		return new BoundsInt(minPosition, maxPosition - minPosition);
	}

	public Vector3Int WorldToCell(Vector3 worldPosition)
	{
		return LayersValues[0].WorldToCell(worldPosition);
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