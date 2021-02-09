using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

namespace TileManagement
{
	[ExecuteInEditMode]
	public class MetaTileMap : MonoBehaviour
	{
		public int TargetMSpreFrame = 5;

		private Stopwatch stopwatch = new Stopwatch();


		private readonly Dictionary<Layer, Dictionary<Vector3Int, TileLocation>> PresentTiles =
			new Dictionary<Layer, Dictionary<Vector3Int, TileLocation>>();

		/// <summary>
		/// Use this dictionary only if performance isn't critical, otherwise try using arrays below
		/// </summary>
		public Dictionary<LayerType, Layer> Layers { get; private set; }

		private static readonly Stack<TileLocation> PooledTileLocation = new Stack<TileLocation>();

		private static TileLocation GetPooledTile()
		{
			lock (PooledTileLocation)
			{
				return PooledTileLocation.Count > 0 ? PooledTileLocation.Pop() : new TileLocation();
			}
		}


		public Queue<TileLocation> QueuedChanges = new Queue<TileLocation>();

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

		private Matrix PresentMatrix = null;

		public float Resistance(Vector3Int cellPos, bool includeObjects = true)
		{
			float resistance = 0;
			foreach (var damageableLayer in DamageableLayers)
			{
				resistance += damageableLayer.TilemapDamage.Integrity(cellPos);
			}

			if (includeObjects && ObjectLayer)
			{
				resistance += ObjectLayer.GetObjectResistanceAt(cellPos, true);
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

				if (layer.GetComponent<TilemapDamage>())
				{
					damageableLayersValues.Add(layer);
				}

				if (layer.LayerType == LayerType.Objects)
				{
					ObjectLayer = layer as ObjectLayer;
					continue;
				}

				var ToInsertDictionary = new Dictionary<Vector3Int, TileLocation>();
				BoundsInt bounds = layer.Tilemap.cellBounds;
				TileLocation Tile = null;
				for (int n = bounds.xMin; n < bounds.xMax; n++)
				{
					for (int p = bounds.yMin; p < bounds.yMax; p++)
					{
						Vector3Int localPlace = (new Vector3Int(n, p, 0));
						var getTile = layer.Tilemap.GetTile(localPlace) as LayerTile;
						if (getTile != null)
						{
							Tile = GetPooledTile();
							Tile.TileCoordinates = localPlace;
							Tile.PresentMetaTileMap = this;
							Tile.PresentlyOn = layer;
							Tile.Tile = getTile;
							Tile.Colour = layer.Tilemap.GetColor(localPlace);
							Tile.TransformMatrix = layer.Tilemap.GetTransformMatrix(localPlace);
							ToInsertDictionary[localPlace] = Tile;
						}
					}
				}

				lock (PresentTiles)
				{
					PresentTiles[layer] = ToInsertDictionary;
				}
			}

			lock (PresentTiles)
			{
				PresentTiles[ObjectLayer] = new Dictionary<Vector3Int, TileLocation>();
			}

			LayersKeys = layersKeys.ToArray();
			LayersValues = layersValues.ToArray();
			SolidLayersValues = solidLayersValues.ToArray();
			damageableLayersValues.Sort((layerOne, layerTwo) =>
				layerOne.LayerType.GetOrder().CompareTo(layerTwo.LayerType.GetOrder()));
			DamageableLayers = damageableLayersValues.ToArray();
			PresentMatrix = this.GetComponent<Matrix>();
			if (Application.isPlaying == false) return;
			//UpdateManager.Add(CallbackType.UPDATE, ChangeCheck);
		}

		public void OnDisable()
		{
			//Was calling uneven number of Disables and enables on main station Resulting in it being removed from the update manager even though it was enabled idk how
			//UpdateManager.Remove(CallbackType.UPDATE, ChangeCheck);
		}

		public void Update()
		{
			lock (QueuedChanges)
			{
				if (QueuedChanges.Count == 0) return;
			}

			stopwatch.Reset();
			stopwatch.Start();
			TileLocation QueueTileChange = null;
			while (stopwatch.ElapsedMilliseconds < TargetMSpreFrame)
			{
				lock (QueuedChanges)
				{
					if (QueuedChanges.Count == 0) break;
					QueueTileChange = QueuedChanges.Dequeue();
				}


				if (QueueTileChange.Tile == null)
				{
					//Remove before setting
					lock (PresentTiles)
					{
						PresentTiles[QueueTileChange.PresentlyOn][QueueTileChange.TileCoordinates] = null;
					}

					QueueTileChange.PresentlyOn.RemoveTile(QueueTileChange.TileCoordinates);
					// remember update transforms and position and colour when removing On tile map I'm assuming It doesn't clear it?
					QueueTileChange.Clean();
					lock (PooledTileLocation)
					{
						PooledTileLocation.Push(QueueTileChange);
					}
				}
				else
				{
					QueueTileChange.PresentlyOn.SetTile(QueueTileChange.TileCoordinates, QueueTileChange.Tile,
						QueueTileChange.TransformMatrix, QueueTileChange.Colour);
				}

				lock (QueueTileChange)
				{
					QueueTileChange.InQueue = false;
				}
			}

			stopwatch.Stop();
		}

		/// <summary>
		/// Apply damage to damageable layers, top to bottom.
		/// If tile gets destroyed, remaining damage is applied to the layer below
		/// Returns how much damage was absorbed
		/// </summary>
		public float ApplyDamage(Vector3Int cellPos, float damage, Vector3Int worldPos,
			AttackType attackType = AttackType.Melee)
		{
			//still needs to be done
			TileLocation TileLcation = null;
			float RemainingDamage = damage;
			foreach (var damageableLayer in DamageableLayers)
			{
				if (RemainingDamage <= 0f)
				{
					return (damage);
				}

				lock (PresentTiles)
				{
					PresentTiles[damageableLayer].TryGetValue(cellPos, out TileLcation);
				}

				RemainingDamage -= damageableLayer.TilemapDamage.ApplyDamage(damage, attackType, worldPos);
			}

			if (RemainingDamage > damage)
			{
				return (damage);
			}

			return (damage - RemainingDamage);
		}

		public bool IsPassableAtOneTileMap(Vector3Int origin, Vector3Int to, bool isServer,
				CollisionType collisionType = CollisionType.Player, bool inclPlayers = true, GameObject context = null,
				List<LayerType> excludeLayers = null, List<TileType> excludeTiles = null, bool ignoreObjects = false,
				bool isReach = false, bool onlyExcludeLayerOnDestination = false)
		{
			// Simple case: orthogonal travel
			if (origin.x == to.x || origin.y == to.y)
			{
				return IsPassableAtOrthogonal(origin, to, isServer, collisionType, inclPlayers, context, excludeLayers,
					   excludeTiles, ignoreObjects, isReach: isReach);
			}
			else // diagonal travel
			{
				Vector3Int toX = new Vector3Int(to.x, origin.y, origin.z);
				Vector3Int toY = new Vector3Int(origin.x, to.y, origin.z);

				List<LayerType> diagonalExcludes = onlyExcludeLayerOnDestination ? null : excludeLayers;

				bool isPassableIfHorizontalFirst = IsPassableAtOrthogonal(origin, toX, isServer, collisionType, inclPlayers, context, diagonalExcludes,
						   excludeTiles, ignoreObjects, isReach: isReach) &&
					   IsPassableAtOrthogonal(toX, to, isServer, collisionType, inclPlayers, context, excludeLayers, excludeTiles, ignoreObjects, isReach: isReach);

				bool isPassableIfVerticalFirst = IsPassableAtOrthogonal(origin, toY, isServer, collisionType, inclPlayers, context, diagonalExcludes,
						   excludeTiles, ignoreObjects, isReach: isReach) &&
					   IsPassableAtOrthogonal(toY, to, isServer, collisionType, inclPlayers, context, excludeLayers, excludeTiles, ignoreObjects, isReach: isReach);

				return isPassableIfHorizontalFirst || isPassableIfVerticalFirst;
			}

		}

		private bool IsPassableAtOrthogonal(Vector3Int origin, Vector3Int to, bool isServer,
			CollisionType collisionType = CollisionType.Player, bool inclPlayers = true, GameObject context = null,
			List<LayerType> excludeLayers = null, List<TileType> excludeTiles = null, bool ignoreObjects = false, bool isReach = false)
		{
			if (ignoreObjects == false &&
					ObjectLayer.IsPassableAtOnThisLayer(origin, to, isServer, collisionType, inclPlayers, context, excludeTiles, isReach: isReach) == false)
			{
				return false;
			}

			TileLocation TileLcation = null;
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

				lock (PresentTiles)
				{
					PresentTiles[SolidLayersValues[i]].TryGetValue(to, out TileLcation);
				}

				if (TileLcation?.Tile == null) continue;
				var tile = TileLcation.Tile as BasicTile;

				// Return passable if the tile type is being excluded from checks.
				if (excludeTiles != null && excludeTiles.Contains(tile.TileType))
					continue;

				if (tile.IsPassable(collisionType, origin, this) == false)
				{
					return false;
				}

				// if ((TileLcation.Tile as BasicTile).IsAtmosPassable() == false)
				// {
				// return false;
				// }

				// if (!SolidLayersValues[i].IsPassableAt(origin, to, isServer, collisionType: collisionType,
				// inclPlayers: inclPlayers, context: context, excludeTiles))
				// {
				// return false;
				// }
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

		private bool _IsAtmosPassableAt(Vector3Int origin, Vector3Int to, bool isServer) //TODO needs Object Passable
		{
			if (ObjectLayer.IsAtmosPassableAt(origin, to, isServer) == false)
			{
				return false;
			}


			TileLocation TileLcation = null;
			foreach (var layer in LayersValues)
			{
				lock (PresentTiles)
				{
					PresentTiles[layer].TryGetValue(to, out TileLcation);
				}

				if (TileLcation?.Tile == null) continue;
				if ((TileLcation.Tile as BasicTile).IsAtmosPassable() == false)
				{
					return false;
				}
			}

			return true;
			// for (var i = 0; i < LayersValues.Length; i++)
			// {
			// if (!LayersValues[i].IsAtmosPassableAt(origin, to, isServer))
			// {
			// return false;
			// }
			// }

			// return true;
		}

		public bool IsSpaceAt(Vector3Int position, bool isServer)
		{
			TileLocation TileLcation = null;
			foreach (var layer in LayersValues)
			{
				if (layer.LayerType == LayerType.Objects) continue;

				lock (PresentTiles)
				{
					PresentTiles[layer].TryGetValue(position, out TileLcation);
				}

				if (TileLcation?.Tile == null) continue;
				if ((TileLcation.Tile as BasicTile).IsSpace() == false)
				{
					return false;
				}
			}

			return true;

			// for (var i = 0; i < LayersValues.Length; i++)
			// {
			// if (!LayersValues[i].IsSpaceAt(position, isServer))
			// {
			// return false;
			// }
			// }

			// return true;
		}

		public bool IsTableAt(Vector3Int position)
		{
			if (Layers.TryGetValue( LayerType.Tables , out var layer))
			{
				return layer.GetTile(position);
			}

			return false;

			// var ObjectTile = ObjectLayer.GetTile(position);
			// if (ObjectTile == null) return false;
			// return ObjectTile.TileType == TileType.Table;
		}

		public bool IsTileTypeAt(Vector3Int position, TileType tileType)
		{
			//is Table here, That's all it Used for
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

		public void SetTile(Vector3Int position, LayerTile tile, Matrix4x4? matrixTransform = null, Color? color = null, bool isPlaying = true)
		{
			if (Layers.TryGetValue(tile.LayerType, out var layer))
			{
				if (isPlaying == false) //is the game playing or is this the levelbrush?
				{
					layer.SetTile(position, tile,
						matrixTransform.GetValueOrDefault(Matrix4x4.identity),
						color.GetValueOrDefault(Color.white));
				}


				TileLocation TileLcation = null;
				lock (PresentTiles)
				{
					PresentTiles[layer].TryGetValue(position, out TileLcation);
				}

				if (TileLcation != null)
				{
					TileLcation.Tile = tile;
					TileLcation.TransformMatrix = matrixTransform.GetValueOrDefault(Matrix4x4.identity);
					TileLcation.Colour = color.GetValueOrDefault(Color.white);
					TileLcation.OnStateChange();
				}
				else
				{
					TileLcation = GetPooledTile();
					TileLcation.PresentlyOn = layer;
					TileLcation.PresentMetaTileMap = this;
					TileLcation.TileCoordinates = position;

					TileLcation.Tile = tile;
					TileLcation.TransformMatrix = matrixTransform.GetValueOrDefault(Matrix4x4.identity);
					TileLcation.Colour = color.GetValueOrDefault(Color.white);
					lock (PresentTiles)
					{
						PresentTiles[layer][position] = TileLcation;
					}

					TileLcation.OnStateChange();
				}

				// layer.SetTile(position, tile,
				// matrixTransform.GetValueOrDefault(Matrix4x4.identity),
				// color.GetValueOrDefault(Color.white));
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
			if (layerType == LayerType.Objects) return null;
			if (Layers.TryGetValue(layerType, out var layer))
			{
				TileLocation TileLcation = null;
				lock (PresentTiles)
				{
					PresentTiles[layer].TryGetValue(cellPosition, out TileLcation);
				}

				return TileLcation?.Tile;
				//return layer.GetTile(cellPosition);
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
		public bool IsDifferent(Vector3Int cellPosition, LayerTile layerTile, LayerType layerType,
			Matrix4x4? transformMatrix = null,
			Color? color = null)
		{
			if (layerType == LayerType.Objects) return true;
			if (Layers.TryGetValue(layerType, out var layer))
			{
				TileLocation TileLcation = null;
				lock (PresentTiles)
				{
					PresentTiles[layer].TryGetValue(cellPosition, out TileLcation);
				}

				if (TileLcation?.Tile != layerTile) return true;

				if (color != null)
				{
					if (TileLcation.Colour != color.GetValueOrDefault(Color.white)) return true;
				}

				if (transformMatrix != null)
				{
					if (TileLcation.TransformMatrix != transformMatrix.GetValueOrDefault(Matrix4x4.identity))
						return true;
				}

				return false;
				//return layer.IsDifferent(cellPosition, layerTile, transformMatrix, color);
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
			TileLocation TileLcation = null;
			foreach (var layer in LayersValues)
			{
				if (layer.LayerType == LayerType.Objects) continue;

				if (ignoreEffectsLayer && layer.LayerType == LayerType.Effects) continue;

				if (layer.LayerType == LayerType.Underfloor)
				{
					var TTile = layer.GetTile(cellPosition);
					if (TTile != null)
					{
						return TTile;
					}
				}

				lock (PresentTiles)
				{
					PresentTiles[layer].TryGetValue(cellPosition, out TileLcation);
				}

				if (TileLcation != null)
				{
					break;
				}
			}

			return TileLcation?.Tile;
			// for (var i = 0; i < LayersValues.Length; i++)
			// {
			// LayerTile tile = LayersValues[i].GetTile(cellPosition);
			// if (tile != null)
			// {
			// if (ignoreEffectsLayer && tile.LayerType == LayerType.Effects) continue;

			// return tile;
			// }
			// }

			// return null;
		}

		/// <summary>
		/// Gets the topmost tile at the specified cell position , Whilst ignoring the specified tiles in the ExcludedLayers
		/// </summary>
		/// <param name="cellPosition">cell position within the tilemap to get the tile of. NOT the same
		/// as world position.</param>
		/// <returns></returns>
		public LayerTile GetTile(Vector3Int cellPosition, LayerTypeSelection ExcludedLayers)
		{
			TileLocation TileLcation = null;
			foreach (var layer in LayersValues)
			{
				if (layer.LayerType == LayerType.Objects) continue;

				if (LTSUtil.IsLayerIn(ExcludedLayers, layer.LayerType)) continue;

				lock (PresentTiles)
				{
					PresentTiles[layer].TryGetValue(cellPosition, out TileLcation);
				}

				if (TileLcation != null)
				{
					break;
				}
			}

			return TileLcation?.Tile;

			// for (var i = 0; i < LayersValues.Length; i++)
			// {
			// LayerTile tile = LayersValues[i].GetTile(cellPosition);
			// if (tile != null)
			// {
			// if (LTSUtil.IsLayerIn(ExcludedLayers, tile.LayerType)) continue;

			// return tile;
			// }
			// }

			// return null;
		}


		/// <summary>
		/// Checks if tile is empty of objects (only solid by default)
		/// </summary>
		public bool IsEmptyAt(Vector3Int position, bool isServer)
		{
			for (var index = 0; index < LayersKeys.Length; index++)
			{
				LayerType layer = LayersKeys[index];
				if (layer != LayerType.Objects && HasTile(position, layer))
				{
					return false;
				}

				if (layer == LayerType.Objects)
				{
					foreach (RegisterTile o in isServer
						? ((ObjectLayer) LayersValues[index]).ServerObjects.Get(position)
						: ((ObjectLayer) LayersValues[index]).ClientObjects.Get(position))
					{
						if (o.IsPassable(isServer) == false)
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
				if (layer != LayerType.Objects && HasTile(position, layer))
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
							if (pushPull == null && o.IsPassable(isServer) == false)
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
				if (layer != LayerType.Objects && HasTile(position, layer))
				{
					return false;
				}

				if (layer == LayerType.Objects)
				{
					foreach (RegisterTile o in isServer
						? ((ObjectLayer) LayersValues[i1]).ServerObjects.Get(position)
						: ((ObjectLayer) LayersValues[i1]).ClientObjects.Get(position))
					{
						if (o.IsPassable(isServer) == false)
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

							if (isExcluded == false)
							{
								return false;
							}
						}
					}
				}
			}

			return true;
		}

		public bool HasObject(Vector3Int position, bool IsServer)
		{
			return ObjectLayer.HasObject(position, IsServer);
		}

		/// <summary>
		/// Cheap method to check if there's a tile, Do not use for objects
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public bool HasTile(Vector3Int position)
		{
			TileLocation TileLcation = null;
			foreach (var layer in LayersValues)
			{
				if (layer.LayerType == LayerType.Objects) continue;

				lock (PresentTiles)
				{
					PresentTiles[layer].TryGetValue(position, out TileLcation);
				}

				if (TileLcation != null)
				{
					break;
				}
			}

			return TileLcation?.Tile;

			// for (var i = 0; i < LayersValues.Length; i++)
			// {
			// if (LayersValues[i].HasTile(position, isServer))
			// {
			// return true;
			// }
			// }

			// return false;
		}

		/// <summary>
		/// ues has object if you want to search for objects this only finds a tile
		/// </summary>
		/// <param name="position"></param>
		/// <param name="layerType"></param>
		/// <returns></returns>
		public bool HasTile(Vector3Int position, LayerType layerType)
		{
			if (layerType == LayerType.Objects)
			{
				Logger.LogError("Please use get objects instead of get tile");
				return false;
			}

			if (Layers.TryGetValue(layerType, out var layer))
			{
				if (layer.LayerType == LayerType.Underfloor)
				{
					return layer.HasTile(position);
				}

				TileLocation TileLcation = null;
				lock (PresentTiles)
				{
					PresentTiles[layer].TryGetValue(position, out TileLcation);
				}

				if (TileLcation != null)
				{
					return true;
				}

				//return layer.HasTile(position, isServer);
			}
			else
			{
				LogMissingLayer(position, layerType);
			}

			return false;
		}

		public void RemoveTile(Vector3Int position, bool RemoveAll = true)
		{
			TileLocation TileLcation = null;
			foreach (var layer in LayersValues)
			{
				if (layer.LayerType == LayerType.Objects) continue;
				if (Application.isPlaying == false)
				{
					if (layer.RemoveTile(position))
					{
						if (RemoveAll == false)
						{
							return;
						}
					}
					continue;
				}

				lock (PresentTiles)
				{
					PresentTiles[layer].TryGetValue(position, out TileLcation);
				}

				if (TileLcation != null)
				{
					TileLcation.Tile = null;
					TileLcation.OnStateChange();
					if (RemoveAll == false)
					{
						return;
					}
				}
			}

			// for (var i = 0; i < LayersValues.Length; i++)
			// {
			// Layer layer = LayersValues[i]; //layer.LayerType < refLayer &&
			// TODO: @Bod9001 What is the purpose of this strange conditional logic?
			// idk - Bod9001
			// if (
			// !(refLayer == LayerType.Objects && layer.LayerType == LayerType.Floors) &&
			// refLayer != LayerType.Grills)
			// {
			// if (layer.RemoveTile(position) && RemoveAll == false)
			// {
			// return;
			// }
			// }
			// }
		}

		public void RemoveTileWithlayer(Vector3Int position, LayerType refLayer)
		{
			if (refLayer == LayerType.Objects) return;

			if (Layers.TryGetValue(refLayer, out var layer))
			{
				if (layer.LayerType == LayerType.Underfloor)
				{
					(layer as UnderFloorLayer).RemoveSpecifiedTile(position, null, true);
				}


				TileLocation TileLcation = null;
				lock (PresentTiles)
				{
					PresentTiles[layer].TryGetValue(position, out TileLcation);
				}

				if (TileLcation != null)
				{
					TileLcation.Tile = null;
					TileLcation.OnStateChange();
				}

				//layer.RemoveTile(position);
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
				lock (PresentTiles)
				{
					foreach (var keypar in PresentTiles[LayersValues[i]])
					{
						keypar.Value.Clean();
						PooledTileLocation.Push(keypar.Value);
					}

					PresentTiles[LayersValues[i]].Clear();
				}

				LayersValues[i].ClearAllTiles();
			}
		}

		public Vector3 LocalToWorld(Vector3 localPos) => LayersValues[0].LocalToWorld(localPos);
		public Vector3 CellToWorld(Vector3Int cellPos) => LayersValues[0].CellToWorld(cellPos);
		public Vector3 WorldToLocal(Vector3 worldPos) => LayersValues[0].WorldToLocal(worldPos);

		public BoundsInt GetWorldBounds()
		{
			var bounds = GetBounds();
			//???
			var min = CellToWorld(bounds.min);
			var max = CellToWorld(bounds.max);
			if (PresentMatrix?.MatrixMove?.inProgressRotation != null)
			{
				Vector3Int TopRightMax = bounds.max;
				Vector3Int BottomLeftMin = bounds.min;
				Vector3Int BottomRight = new Vector3Int(TopRightMax.x, BottomLeftMin.y, 0);
				Vector3Int TopLeft = new Vector3Int(BottomLeftMin.x, TopRightMax.y, 0);
				var TopRightMaxI = CellToWorld(TopRightMax);
				var BottomLeftMinI = CellToWorld(BottomLeftMin);
				var BottomRightI = CellToWorld(BottomRight);
				var TopLeftI = CellToWorld(TopLeft);
				MaxMinCheck(ref min, ref max, TopRightMaxI);
				MaxMinCheck(ref min, ref max, BottomLeftMinI);
				MaxMinCheck(ref min, ref max, BottomRightI);
				MaxMinCheck(ref min, ref max, TopLeftI);
			}


			return new BoundsInt(min.RoundToInt(), (max - min).RoundToInt());
		}

		public void MaxMinCheck(ref Vector3 min, ref Vector3 max, Vector3 ToCompare)
		{
			if (ToCompare.x > max.x)
			{
				max.x = ToCompare.x;
			}
			else if (min.x > ToCompare.x)
			{
				min.x = ToCompare.x;
			}

			if (ToCompare.y > max.y)
			{
				max.y = ToCompare.y;
			}
			else if (min.y > ToCompare.y)
			{
				min.y = ToCompare.y;
			}
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
					if (layer.LayerType == LayerType.Objects) continue;
					Layers[layer.LayerType].SetPreviewTile(position, LayerTile.EmptyTile, Matrix4x4.identity);
				}
			}

			if (Layers.ContainsKey(tile.LayerType) == false)
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
				if (LayersValues[i].LayerType == LayerType.Objects) continue;
				LayersValues[i].ClearPreview();
			}
		}
#endif

		#region Physics

		//Gets the first hit
		public MatrixManager.CustomPhysicsHit? Raycast(
			Vector2 origin,
			Vector2 direction,
			float distance,
			LayerTypeSelection layerMask, Vector2? To = null)
		{
			if (To == null)
			{
				To = direction.normalized * distance;
			}

			if (direction.x == 0 && direction.y == 0)
			{
				direction = (To.Value - origin).normalized;
				distance = (To.Value - origin).magnitude;
			}

			// var Beginning = (new Vector3((float) origin.x, (float) origin.y, 0).ToWorld(PresentMatrix));
			// Debug.DrawLine(Beginning + (Vector3.right * 0.09f), Beginning + (Vector3.left * 0.09f), Color.yellow, 30);
			// Debug.DrawLine(Beginning + (Vector3.up * 0.09f), Beginning + (Vector3.down * 0.09f), Color.yellow, 30);

			// var end = (new Vector3((float) To.Value.x, (float) To.Value.y, 0).ToWorld(PresentMatrix));
			// Debug.DrawLine(end + (Vector3.right * 0.09f), end + (Vector3.left * 0.09f), Color.red, 30);
			// Debug.DrawLine(end + (Vector3.up * 0.09f), end + (Vector3.down * 0.09f), Color.red, 30);

			// Debug.DrawLine(Beginning, end, Color.magenta, 30);


			Vector2 Relativetarget = To.Value - origin;
			//custom code on tile to ask if it aptly got hit, if you want custom geometry
//
//What needs to be returned
//What it hit, game object/tile
//Normal of hit
//Can be done quite easily since we know if me moving left or right

//Static manager
//Calculate if Within bounds of matrix?
//End and start World pos
//
//Calculate offset to then put onto positions, Maybe each position gets its own Calculation

			double RelativeX = 0;
			double RelativeY = 0;


			double gridOffsetx = 0;
			double gridOffsety = 0;

			int xSteps = 0;
			int ySteps = 0;

			int stepX = 0;
			int stepY = 0;

			double Offsetuntouchx = (origin.x - Math.Round(origin.x));
			double Offsetuntouchy = (origin.y - Math.Round(origin.y));

			if (direction.x < 0)
			{
				gridOffsetx = -(-0.5d + Offsetuntouchx); //0.5f  //this is So when you multiply it gives you 0.5 that some tile borders
				stepX = -1; //For detecting which Tile it hits
			}
			else
			{
				gridOffsetx = -0.5d - Offsetuntouchx; //-0.5f
				stepX = 1;
				//sideDistX = (mapX + 1.0 - posX) * deltaDistX;
			}

			if (direction.y < 0)
			{
				gridOffsety = -(-0.5d + Offsetuntouchy); // 0.5f
				stepY = -1;
				//sideDistY = (posY - mapY) * deltaDistY;
			}
			else
			{
				gridOffsety = -0.5d - Offsetuntouchy; //-0.5f
				stepY = 1;
				//sideDistY = (mapY + 1.0 - posY) * deltaDistY;
			}


			var vec = Vector3Int.zero; //Tile it hit Local  Coordinates
			var vecHit = Vector3.zero; //Coordinates of Edge tile hit
			TileLocation TileLcation = null;

			var vexinvX = (1d / (direction.x)); //Editions need to be done here for Working offset
			var vexinvY = (1d / (direction.y)); //Needs to be conditional

			double calculationFloat = 0;

			bool LeftFaceHit = true;



			while (Math.Abs((xSteps + gridOffsetx + stepX) * vexinvX) < distance ||
			       Math.Abs((ySteps + gridOffsety + stepY) * vexinvY) < distance)
				//for (int Ai = 0; Ai < 6; Ai++)
			{
				//if (xBuildUp > yBuildUp)
				if ((xSteps + gridOffsetx + stepX) * vexinvX < (ySteps + gridOffsety + stepY) * vexinvY
				) // which one has a lesser multiplication factor since that will give a less Magnitude
				{
					xSteps += stepX;

					calculationFloat = ((xSteps + gridOffsetx) * vexinvX);

					RelativeX = direction.x * calculationFloat; //Remove offset here maybe?
					RelativeY = direction.y * calculationFloat;

					LeftFaceHit = true;
				}
				//else if (xBuildUp < yBuildUp)
				else //if (xBuildUp < yBuildUp)
				{
					ySteps += stepY;
					calculationFloat = ((ySteps + gridOffsety) * vexinvY);

					RelativeX = direction.x * calculationFloat;
					RelativeY = direction.y * calculationFloat;

					LeftFaceHit = false;
				}


				vec.x = (int) Mathf.Round(origin.x) + xSteps;
				vec.y = (int) Mathf.Round(origin.y) + ySteps;

				vecHit.x = origin.x + (float) RelativeX; //+ offsetX;
				vecHit.y = origin.y + (float) RelativeY; // + offsetY;
				//Check point here

				if (LeftFaceHit)
				{
					// float TestX = ((vecHit.x - 0.5f) - Mathf.Floor(vecHit.x));

					// if (0.05f < Math.Abs(TestX))
					// {
						// Logger.Log("Offsetuntouchx = " + Offsetuntouchx + "\n" + "directionx = " + direction.x + "\n" +
						           // "Step = " + xSteps + "\n" + "vexinv = " + vexinvX + "\n" + "offset = " + offsetX +
						           // "\n" + "\n" + "Test =" + TestX + "\n" + "Relative =" + (RelativeX)
						           // + "\n" + "\n"
						           // + " direction.x = " +  direction.x + " calculationFloat " + calculationFloat
						           // + "\n" + " xSteps " + xSteps + " gridOffsetx " + gridOffsetx  +"  vexinvX " + vexinvX);


					// }
				}
				else
				{
					// float Testy = ((vecHit.y - 0.5f) - Mathf.Floor(vecHit.y));
					// if (0.05f < Math.Abs(Testy))
					// {
						// Logger.Log("Offsetuntouchx = " + Offsetuntouchy + "\n" + "directionx = " + direction.y + "\n" +
						           // "Step = " + ySteps + "\n" + "vexinv = " + vexinvY + "\n" + "offset = " + offsetY +
						           // "\n" + "\n" + "Test =" + Testy + "\n" + "Relative =" + (RelativeY)
						           // + "\n" + "\n"
						           // + " direction.y = " +  direction.y + " calculationFloat " + calculationFloat
						           // + "\n" + " ySteps " + ySteps + " gridOffsety " + gridOffsety  +"  vexinvY " + vexinvY);
					// }
				}

				for (var i = 0; i < LayersValues.Length; i++)
				{
					if (LayersValues[i].LayerType == LayerType.Objects) continue;
					if (LTSUtil.IsLayerIn(layerMask, LayersValues[i].LayerType))
					{
						lock (PresentTiles)
						{
							PresentTiles[LayersValues[i]].TryGetValue(vec, out TileLcation);
						}

						// var wold = (vecHit.ToWorld(PresentMatrix));
						// Debug.DrawLine(wold + (Vector3.right * 0.09f), wold + (Vector3.left * 0.09f), Color.green, 30);
						// Debug.DrawLine(wold + (Vector3.up * 0.09f), wold + (Vector3.down * 0.09f), Color.green, 30);


						// if (LeftFaceHit)
						// {
							// Debug.DrawLine(wold + (Vector3.up * 4f), wold + (Vector3.down * 4), Color.blue, 30);
						// }
						// else
						// {
							// Debug.DrawLine(wold + (Vector3.right * 4), wold + (Vector3.left * 4), Color.blue, 30);
						// }

						// ColorUtility.TryParseHtmlString("#ea9335", out var Orange);
						// var map = ((Vector3) vec).ToWorld(PresentMatrix);
						// Debug.DrawLine(map + (Vector3.right * 0.09f), map + (Vector3.left * 0.09f), Orange, 30);
						// Debug.DrawLine(map + (Vector3.up * 0.09f), map + (Vector3.down * 0.09f), Orange, 30);

						if (TileLcation != null)
						{
							Vector2 normal;

							if (LeftFaceHit)
							{
								normal = Vector2.left * stepX;
							}
							else
							{
								normal = Vector2.down * stepY;
							}


							Vector3 AdjustedNormal = ((Vector3) normal).ToWorld(PresentMatrix);
							AdjustedNormal = AdjustedNormal - (Vector3.zero.ToWorld(PresentMatrix));


							// Debug.DrawLine(wold, wold + AdjustedNormal, Color.cyan, 30);

							return new MatrixManager.CustomPhysicsHit(((Vector3) vec).ToWorld(PresentMatrix),
								(vecHit).ToWorld(PresentMatrix), AdjustedNormal,
								new Vector2((float) RelativeX, (float) RelativeY).magnitude, TileLcation);
						}
					}
				}
			}
			return null;
		}


		#endregion
	}
}