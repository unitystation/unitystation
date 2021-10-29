using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Objects.Atmospherics;
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

		public Dictionary<Layer, Dictionary<Vector3Int, TileLocation>> PresentTilesNeedsLock => PresentTiles;

		private readonly Dictionary<Layer, Dictionary<Vector3Int, List<TileLocation>>> MultilayerPresentTiles =
			new Dictionary<Layer, Dictionary<Vector3Int, List<TileLocation>>>();

		public Dictionary<Layer, Dictionary<Vector3Int, List<TileLocation>>> MultilayerPresentTilesNeedsLock =>
			MultilayerPresentTiles;

		/// <summary>
		/// Use this dictionary only if performance isn't critical, otherwise try using arrays below
		/// </summary>This
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

		//Determines the maximum amount of overlays allowed on a tile
		private const int OVERLAY_LIMIT = 20;

		public List<Layer> ffLayersValues;

		/// <summary>
		/// Array of only layers that can ever contain solid stuff
		/// </summary>
		public Layer[] SolidLayersValues { get; private set; }

		/// <summary>
		/// Layers that contain TilemapDamage
		/// </summary>
		public Layer[] DamageableLayers { get; private set; }

		private Matrix presentMatrix = null;

		public Matrix PresentMatrix => presentMatrix;

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
				layer.metaTileMap = this;
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

				if (layer.LayerType != LayerType.Underfloor)
				{
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
			}

			lock (PresentTiles)
			{
				PresentTiles[ObjectLayer] = new Dictionary<Vector3Int, TileLocation>();
			}

			layersKeys.Sort((layerOne, layerTwo) =>
				layerOne.GetOrder().CompareTo(layerTwo.GetOrder()));

			LayersKeys = layersKeys.ToArray();
			layersValues.Sort((layerOne, layerTwo) =>
				layerOne.LayerType.GetOrder().CompareTo(layerTwo.LayerType.GetOrder()));

			LayersValues = layersValues.ToArray();

			SolidLayersValues = solidLayersValues.ToArray();
			damageableLayersValues.Sort((layerOne, layerTwo) =>
				layerOne.LayerType.GetOrder().CompareTo(layerTwo.LayerType.GetOrder()));
			DamageableLayers = damageableLayersValues.ToArray();
			presentMatrix = this.GetComponent<Matrix>();
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
					if (QueueTileChange.PresentlyOn.LayerType == LayerType.Underfloor) //TODO Tile map upgrade
					{
						lock (MultilayerPresentTiles)
						{
							var tileLocations = GetTileLocationsNeedLockSurrounding(QueueTileChange.TileCoordinates,
								QueueTileChange.PresentlyOn);
							if (tileLocations != null)
							{
								if (tileLocations.Count > Math.Abs(1 - QueueTileChange.TileCoordinates.z))
								{
									tileLocations[Math.Abs(1 - QueueTileChange.TileCoordinates.z)] = null;
								}
							}
						}
					}
					else
					{
						lock (PresentTiles)
						{
							PresentTiles[QueueTileChange.PresentlyOn][QueueTileChange.TileCoordinates] = null;
						}
					}


					QueueTileChange.PresentlyOn.RemoveTile(QueueTileChange.TileCoordinates);

					// remember update transforms and position and colour when removing On tile map I'm assuming It doesn't clear it?
					// Maybe it sets it to the correct ones when you set a tile idk

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

			foreach (var layer in LayersValues)
			{
				layer.overlayStore.Clear();
			}
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
			//TileLocation TileLcation = null;
			float RemainingDamage = damage;
			foreach (var damageableLayer in DamageableLayers)
			{
				if (RemainingDamage <= 0f)
				{
					return (damage);
				}

				// lock (PresentTiles)
				// {
				// 	PresentTiles[damageableLayer].TryGetValue(cellPos, out TileLcation);
				// }

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

				bool isPassableIfHorizontalFirst = IsPassableAtOrthogonal(origin, toX, isServer, collisionType,
					                                   inclPlayers, context, diagonalExcludes,
					                                   excludeTiles, ignoreObjects, isReach: isReach) &&
				                                   IsPassableAtOrthogonal(toX, to, isServer, collisionType, inclPlayers,
					                                   context, excludeLayers, excludeTiles, ignoreObjects,
					                                   isReach: isReach);

				if (isPassableIfHorizontalFirst) return true;

				bool isPassableIfVerticalFirst = IsPassableAtOrthogonal(origin, toY, isServer, collisionType,
					                                 inclPlayers, context, diagonalExcludes,
					                                 excludeTiles, ignoreObjects, isReach: isReach) &&
				                                 IsPassableAtOrthogonal(toY, to, isServer, collisionType, inclPlayers,
					                                 context, excludeLayers, excludeTiles, ignoreObjects,
					                                 isReach: isReach);

				return isPassableIfVerticalFirst;
			}
		}

		private bool CanLeaveTile(Vector3Int origin, Vector3Int to, bool isServer,
			CollisionType collisionType = CollisionType.Player, bool inclPlayers = true, GameObject context = null,
			List<LayerType> excludeLayers = null, List<TileType> excludeTiles = null, bool ignoreObjects = false,
			bool isReach = false)
		{
			if (ignoreObjects == false &&
			    ObjectLayer.CanLeaveTile(origin, to, isServer, collisionType, inclPlayers, context,
				    excludeTiles, isReach: isReach) == false)
			{
				return false;
			}

			//Tiles don't have a Check for leaving

			return true;
		}

		private bool CanEnterTile(Vector3Int origin, Vector3Int to, bool isServer,
			CollisionType collisionType = CollisionType.Player, bool inclPlayers = true, GameObject context = null,
			List<LayerType> excludeLayers = null, List<TileType> excludeTiles = null, bool ignoreObjects = false,
			bool isReach = false)
		{
			if (ignoreObjects == false &&
			    ObjectLayer.CanEnterTile(origin, to, isServer, collisionType, inclPlayers, context,
				    excludeTiles, isReach: isReach) == false)
			{
				return false;
			}

			TileLocation TileLcation = null;
			for (var i = 0; i < SolidLayersValues.Length; i++)
			{
				var solidLayer = SolidLayersValues[i];

				// Skip floor & base collisions if this is not a shuttle
				if (collisionType != CollisionType.Shuttle)
				{
					if ((solidLayer.LayerType == LayerType.Grills || solidLayer.LayerType == LayerType.Tables ||
					     solidLayer.LayerType == LayerType.Walls || solidLayer.LayerType == LayerType.Windows) == false)
					{
						continue;
					}
				}

				// Skip if the current tested layer is being excluded.
				if (excludeLayers != null && excludeLayers.Contains(solidLayer.LayerType))
				{
					continue;
				}

				TileLcation = GetCorrectTileLocationForLayer(to, solidLayer);

				if (TileLcation?.Tile == null) continue;
				var tile = TileLcation.Tile as BasicTile;

				// Return passable if the tile type is being excluded from checks.
				if (excludeTiles != null && excludeTiles.Contains(tile.TileType))
					continue;

				if (tile.IsPassable(collisionType, origin, this) == false)
				{
					return false;
				}
			}

			return true;
		}

		private bool IsPassableAtOrthogonal(Vector3Int origin, Vector3Int to, bool isServer,
			CollisionType collisionType = CollisionType.Player, bool inclPlayers = true, GameObject context = null,
			List<LayerType> excludeLayers = null, List<TileType> excludeTiles = null, bool ignoreObjects = false,
			bool isReach = false)
		{
			if (ignoreObjects == false &&
			    ObjectLayer.IsPassableAtOnThisLayer(origin, to, isServer, collisionType, inclPlayers, context,
				    excludeTiles, isReach: isReach) == false)
			{
				return false;
			}

			TileLocation TileLcation = null;
			for (var i = 0; i < SolidLayersValues.Length; i++)
			{
				var solidLayer = SolidLayersValues[i];

				// Skip floor & base collisions if this is not a shuttle
				if (collisionType != CollisionType.Shuttle)
				{
					if ((solidLayer.LayerType == LayerType.Grills || solidLayer.LayerType == LayerType.Tables ||
					     solidLayer.LayerType == LayerType.Walls || solidLayer.LayerType == LayerType.Windows) == false)
					{
						continue;
					}
				}

				// Skip if the current tested layer is being excluded.
				if (excludeLayers != null && excludeLayers.Contains(solidLayer.LayerType))
				{
					continue;
				}

				TileLcation = GetCorrectTileLocationForLayer(to, solidLayer);

				if (TileLcation?.Tile == null) continue;
				var tile = TileLcation.Tile as BasicTile;

				// Return passable if the tile type is being excluded from checks.
				if (excludeTiles != null && excludeTiles.Contains(tile.TileType))
					continue;

				if (tile.IsPassable(collisionType, origin, this) == false)
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

		private bool _IsAtmosPassableAt(Vector3Int origin, Vector3Int to, bool isServer) //TODO needs Object Passable
		{
			if (ObjectLayer.IsAtmosPassableAt(origin, to, isServer) == false)
			{
				return false;
			}


			TileLocation TileLcation = null;
			foreach (var layer in LayersValues)
			{
				if (layer.LayerType == LayerType.Walls || layer.LayerType == LayerType.Windows)
				{
					lock (PresentTiles)
					{
						PresentTiles[layer].TryGetValue(to, out TileLcation);
					}

					if (TileLcation?.Tile == null) continue;
					if ((TileLcation.Tile as BasicTile)?.IsAtmosPassable() == false)
					{
						return false;
					}
				}
			}

			return true;
		}

		public bool IsConstructable(Vector3Int position)
		{
			bool canConstruct = false;
			foreach (var layer in LayersValues)
			{
				TileLocation tileLocation = null;
				Dictionary<Vector3Int, TileLocation> tiles;
				if (layer.LayerType == LayerType.Objects)
					continue;

				lock (PresentTiles)
				{
					if (PresentTiles.TryGetValue(layer, out tiles))
					{
						tiles.TryGetValue(position, out tileLocation);
					}
				}

				if (tileLocation?.Tile == null)
					continue;

				if ((tileLocation.Tile as BasicTile)?.constructable == false)
				{
					canConstruct = false;
					break;
				}

				canConstruct = true;
			}

			return canConstruct;
		}


		public bool IsSpaceAt(Vector3Int position, bool isServer, bool UseExactForMultilayer = false)
		{
			TileLocation TileLcation = null;
			foreach (var layer in LayersValues)
			{
				if (layer.LayerType == LayerType.Objects) continue;
				if (layer.LayerType == LayerType.Underfloor) continue;
				if (layer.LayerType == LayerType.Tables) continue;
				if (layer.LayerType == LayerType.Effects) continue;

				TileLcation = GetCorrectTileLocationForLayer(position, layer, UseExactForMultilayer);

				if (TileLcation?.Tile == null) continue;
				if ((TileLcation.Tile as BasicTile)?.IsSpace() == false)
				{
					return false;
				}
			}

			return true;
		}


		public TileLocation GetCorrectTileLocationForLayer(Vector3Int position, Layer layer,
			bool UseExactForMultilayer = false)
		{
			TileLocation TileLcation = null;
			if (layer.LayerType == LayerType.Underfloor) //TODO Tile map upgrade
			{
				if (UseExactForMultilayer)
				{
					TileLcation = GetTileExactLocationMultilayer(position, layer);
				}
				else
				{
					TileLcation = GetTileLocationMultilayer(position, layer);
				}
			}
			else
			{
				lock (PresentTiles)
				{
					PresentTiles[layer].TryGetValue(position, out TileLcation);
				}
			}

			return TileLcation;
		}

		public bool IsTableAt(Vector3Int position)
		{
			if (Layers.TryGetValue(LayerType.Tables, out var layer))
			{
				TileLocation TileLcation = null;
				lock (PresentTiles)
				{
					PresentTiles[layer].TryGetValue(position, out TileLcation);
				}

				return TileLcation?.Tile;
			}

			return false;
		}

		public bool IsTileTypeAt(Vector3Int position, TileType tileType, bool UseExactForMultilayer = false)
		{
			//is Table here, That's all it Used for
			for (var i = 0; i < LayersValues.Length; i++)
			{
				TileLocation TileLcation = null;
				TileLcation = GetCorrectTileLocationForLayer(position, LayersValues[i], UseExactForMultilayer);


				if (TileLcation.Tile != null && TileLcation.Tile.TileType == tileType)
				{
					return true;
				}
			}

			return false;
		}


		//Use TileChangeManager Instead if you want to be networked
		public Vector3Int SetTile(Vector3Int position, LayerTile tile, Matrix4x4? matrixTransform = null,
			Color? color = null,
			bool isPlaying = true)
		{
			if (Layers.TryGetValue(tile.LayerType, out var layer))
			{
				if (isPlaying == false) //is the game playing or is this the levelbrush?
				{
					if (tile.LayerType == LayerType.Underfloor) //TODO Tile map upgrade
					{
						for (int i = 0; i < 50; i++)
						{
							position.z = 1 - i;
							if (layer.GetTile(position) == null)
							{
								layer.SetTile(position, tile,
									matrixTransform.GetValueOrDefault(Matrix4x4.identity),
									color.GetValueOrDefault(Color.white));
								return position;
							}
						}

						Logger.LogError(
							"Tile has reached maximum Meta data system depth This could be from accidental placing of multiple tiles",
							Category.Editor);
						return position;
					}
					else
					{
						layer.SetTile(position, tile,
							matrixTransform.GetValueOrDefault(Matrix4x4.identity),
							color.GetValueOrDefault(Color.white));
					}

					return position;
				}


				TileLocation TileLcation = null;

				if (tile.LayerType == LayerType.Underfloor) //TODO Tile map upgrade
				{
					lock (MultilayerPresentTiles)
					{
						var TileLocations = GetTileLocationsNeedLockSurrounding(position, layer);

						int index = FindFirstEmpty(TileLocations);

						position.z = 1 - index;
						if (TileLocations[index] == null)
						{
							TileLocations[index] = GetPooledTile();
							TileLocations[index].PresentlyOn = layer;
							TileLocations[index].PresentMetaTileMap = this;
							TileLocations[index].TileCoordinates = position;
						}

						TileLcation = TileLocations[index];
					}
				}
				else
				{
					lock (PresentTiles)
					{
						PresentTiles[layer].TryGetValue(position, out TileLcation);
					}

					if (TileLcation == null)
					{
						TileLcation = GetPooledTile();
						TileLcation.PresentlyOn = layer;
						TileLcation.PresentMetaTileMap = this;
						TileLcation.TileCoordinates = position;
						lock (PresentTiles)
						{
							PresentTiles[layer][position] = TileLcation;
						}
					}
				}


				TileLcation.Tile = tile;
				TileLcation.TransformMatrix = matrixTransform.GetValueOrDefault(Matrix4x4.identity);
				TileLcation.Colour = color.GetValueOrDefault(Color.white);
				TileLcation.OnStateChange();
				return position;
			}
			else
			{
				LogMissingLayer(position, tile.LayerType);
			}

			return position;
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
		public LayerTile GetTileAtWorldPos(Vector3 worldPosition, LayerType layerType,
			bool UseExactForMultilayer = false)
		{
			return GetTileAtWorldPos(worldPosition.RoundToInt(), layerType, UseExactForMultilayer);
		}

		/// <summary>
		/// Gets the tile with the specified layer type at the specified world position
		/// </summary>
		/// <param name="worldPosition">world position to check</param>
		/// <param name="layerType"></param>
		/// <returns></returns>
		public LayerTile GetTileAtWorldPos(Vector3Int worldPosition, LayerType layerType,
			bool UseExactForMultilayer = false)
		{
			var cellPos = WorldToCell(worldPosition);
			return GetTile(cellPos, layerType, UseExactForMultilayer: UseExactForMultilayer);
		}

		/// <summary>
		/// Gets the tile with the specified layer type at the specified cell position
		/// </summary>
		/// <param name="cellPosition">cell position within the tilemap to get the tile of. NOT the same
		/// as world position.</param>
		/// <param name="layerType"></param>
		/// <returns></returns>
		public LayerTile GetTile(Vector3Int cellPosition, LayerType layerType, bool UseExactForMultilayer = false)
		{
			if (layerType == LayerType.Objects) return null;
			if (Layers.TryGetValue(layerType, out var layer))
			{
				TileLocation TileLcation = null;
				TileLcation = GetCorrectTileLocationForLayer(cellPosition, layer, UseExactForMultilayer);

				return TileLcation?.Tile;
			}
			else
			{
				LogMissingLayer(cellPosition, layerType);
			}

			return null;
		}


		private TileLocation GetTileExactLocationMultilayer(Vector3Int cellPosition, Layer layer)
		{
			//TODO Tile map upgrade , z Is used as a depth but that needs to be moved to vector4int where it would turn into w
			//This you would just cast to vector3int
			//And use the w for depth Instead of z

			lock (MultilayerPresentTiles)
			{
				var tileLocations = GetTileLocationsNeedLockSurrounding(cellPosition, layer);
				if (tileLocations != null)
				{
					if (tileLocations.Count > Math.Abs(1 - cellPosition.z))
					{
						return tileLocations[Math.Abs(1 - cellPosition.z)];
					}
				}
			}

			return null;
		}

		private List<TileLocation> GetTileLocationsNeedLockSurrounding(Vector3Int cellPosition, Layer layer)
		{
			//TODO Tile map upgrade , z Is used as a depth but that needs to be moved to vector4int where it would turn into w
			var ZZeroposition = cellPosition;
			ZZeroposition.z = 0;
			if (MultilayerPresentTiles.TryGetValue(layer, out var LayerData))
			{
				if (LayerData.TryGetValue(ZZeroposition, out var TileLocations))
				{
					return TileLocations;
				}
				else
				{
					LayerData[ZZeroposition] = new List<TileLocation>();
					return LayerData[ZZeroposition];
				}
			}

			return null;
		}


		private TileLocation GetTileLocationMultilayer(Vector3Int cellPosition, Layer layer)
		{
			//TODO Tile map upgrade , z Is used as a depth but that needs to be moved to vector4int where it would turn into w
			//This you would just cast to vector3int
			//And use the w for depth Instead of z

			lock (MultilayerPresentTiles)
			{
				var tileLocations = GetTileLocationsNeedLockSurrounding(cellPosition, layer);
				if (tileLocations != null)
				{
					foreach (var tileLocation in tileLocations)
					{
						if (tileLocation != null && tileLocation.Tile != null)
						{
							return tileLocation;
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the colour of the tile with the specified layer type at the specified cell position
		/// </summary>
		/// <param name="cellPosition">cell position within the tilemap to get the tile of. NOT the same
		/// as world position.</param>
		/// <param name="layerType"></param>
		/// <returns></returns>
		public Color? GetColour(Vector3Int cellPosition, LayerType layerType, bool UseExactForMultilayer = false)
		{
			if (layerType == LayerType.Objects) return null;
			if (Layers.TryGetValue(layerType, out var layer))
			{
				TileLocation TileLcation = null;
				TileLcation = GetCorrectTileLocationForLayer(cellPosition, layer, UseExactForMultilayer);


				return TileLcation?.Colour;
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
			Color? color = null, bool UseExactForMultilayer = false)
		{
			if (layerType == LayerType.Objects) return true;
			if (Layers.TryGetValue(layerType, out var layer))
			{
				TileLocation TileLcation = null;
				TileLcation = GetCorrectTileLocationForLayer(cellPosition, layer, UseExactForMultilayer);

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
		public LayerTile GetTile(Vector3Int cellPosition, bool ignoreEffectsLayer = false,
			bool UseExactForMultilayer = false)
		{
			TileLocation TileLcation = null;
			foreach (var layer in LayersValues)
			{
				if (layer.LayerType == LayerType.Objects) continue;

				if (ignoreEffectsLayer && layer.LayerType == LayerType.Effects) continue;

				TileLcation = GetCorrectTileLocationForLayer(cellPosition, layer, UseExactForMultilayer);

				if (TileLcation != null && TileLcation.Tile != null)
				{
					break;
				}
			}

			return TileLcation?.Tile;
		}

		/// <summary>
		/// Gets the topmost tile at the specified cell position , Whilst ignoring the specified tiles in the ExcludedLayers
		/// </summary>
		/// <param name="cellPosition">cell position within the tilemap to get the tile of. NOT the same
		/// as world position.</param>
		/// <returns></returns>
		public LayerTile GetTile(Vector3Int cellPosition, LayerTypeSelection ExcludedLayers,
			bool UseExactForMultilayer = false)
		{
			TileLocation TileLcation = null;
			foreach (var layer in LayersValues)
			{
				if (layer.LayerType == LayerType.Objects) continue;

				if (LTSUtil.IsLayerIn(ExcludedLayers, layer.LayerType)) continue;

				TileLcation = GetCorrectTileLocationForLayer(cellPosition, layer, UseExactForMultilayer);

				if (TileLcation != null)
				{
					break;
				}
			}

			return TileLcation?.Tile;
		}


		/// <summary>
		/// Checks if tile is empty of objects (only solid by default)
		/// </summary>
		public bool IsEmptyAt(Vector3Int position, bool isServer)
		{
			for (var index = 0; index < LayersValues.Length; index++)
			{
				var layer = LayersValues[index];
				if (layer.LayerType != LayerType.Objects && HasTile(position, layer))
				{
					return false;
				}

				if (layer.LayerType == LayerType.Objects)
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
		public bool HasTile(Vector3Int position, Layer Layer)
		{
			TileLocation TileLcation = null;

			if (Layer.LayerType == LayerType.Objects) return false;
			if (Layer.LayerType == LayerType.Effects) return false;

			TileLcation = GetCorrectTileLocationForLayer(position, Layer);

			return TileLcation?.Tile;
		}


		/// <summary>
		/// Cheap method to check if there's a tile, Do not use for objects
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public bool HasTile(Vector3Int position, bool UseExactForMultilayer = false)
		{
			TileLocation TileLcation = null;
			foreach (var layer in LayersValues)
			{
				if (layer.LayerType == LayerType.Objects) continue;
				if (layer.LayerType == LayerType.Effects) continue;

				TileLcation = GetCorrectTileLocationForLayer(position, layer, UseExactForMultilayer);

				if (TileLcation != null)
				{
					break;
				}
			}

			return TileLcation?.Tile;
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
				Logger.LogError("Please use get objects instead of get tile", Category.TileMaps);
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

		/// <summary>
		/// Gets the next free overlay position
		/// </summary>
		/// <param name="position"></param>
		/// <param name="layerType"></param>
		/// <param name="overlayName"></param>
		/// <returns></returns>
		public Vector3Int? GetFreeOverlayPos(Vector3Int position, LayerType layerType)
		{
			if (layerType == LayerType.Objects)
			{
				Logger.LogError("Please use get objects instead of get tile");
				return null;
			}

			TileLocation tileLocation = null;
			position.z = 1;

			if (Layers.TryGetValue(layerType, out var layer))
			{
				//Go through overlays under the overlay limit. The first overlay checked will be at z = 1.
				var count = 0;
				while (count < OVERLAY_LIMIT)
				{
					lock (PresentTiles)
					{
						PresentTiles[layer].TryGetValue(position, out tileLocation);
					}

					if ((tileLocation == null || tileLocation.Tile == null) &&
					    layer.overlayStore.Contains(position) == false)
					{
						layer.overlayStore.Add(position);
						return position;
					}

					position.z++;
					count++;
				}
			}
			else
			{
				LogMissingLayer(position, layerType);
			}

			return null;
		}

		/// <summary>
		/// Gets all positions with a specific overlay type
		/// </summary>
		/// <param name="position"></param>
		/// <param name="layerType"></param>
		/// <param name="overlayName"></param>
		/// <returns></returns>
		public List<Vector3Int> GetOverlayPosByType(Vector3Int position, LayerType layerType, OverlayType overlayType)
		{
			if (layerType == LayerType.Objects)
			{
				Logger.LogError("Please use get objects instead of get tile");
				return null;
			}

			TileLocation tileLocation = null;
			OverlayTile overlayTile = null;
			List<Vector3Int> pos = new List<Vector3Int>();
			position.z = 1;

			if (Layers.TryGetValue(layerType, out var layer))
			{
				//Go through overlays under the overlay limit. The first overlay checked will be at z = 1.
				var count = 0;
				while (count < OVERLAY_LIMIT)
				{
					lock (PresentTiles)
					{
						PresentTiles[layer].TryGetValue(position, out tileLocation);
					}

					if (tileLocation != null)
					{
						overlayTile = tileLocation.Tile as OverlayTile;

						if (overlayTile != null && overlayTile.OverlayType == overlayType)
						{
							pos.Add(position);
						}
					}

					position.z++;
					count++;
				}
			}
			else
			{
				LogMissingLayer(position, layerType);
			}

			return pos;
		}

		/// <summary>
		/// Get all overlay positions
		/// </summary>
		/// <param name="position"></param>
		/// <param name="layerType"></param>
		/// <param name="overlayName"></param>
		/// <returns></returns>
		public List<Vector3Int> GetAllOverlayPos(Vector3Int position, LayerType layerType)
		{
			if (layerType == LayerType.Objects)
			{
				Logger.LogError("Please use get objects instead of get tile");
				return null;
			}

			TileLocation tileLocation = null;
			OverlayTile overlayTile = null;
			List<Vector3Int> pos = new List<Vector3Int>();
			position.z = 1;

			if (Layers.TryGetValue(layerType, out var layer))
			{
				//Go through overlays under the overlay limit. The first overlay checked will be at z = 1.
				var count = 0;
				while (count < OVERLAY_LIMIT)
				{
					lock (PresentTiles)
					{
						PresentTiles[layer].TryGetValue(position, out tileLocation);
					}

					if (tileLocation != null)
					{
						overlayTile = tileLocation.Tile as OverlayTile;

						if (overlayTile != null)
						{
							pos.Add(position);
						}
					}

					position.z++;
					count++;
				}
			}
			else
			{
				LogMissingLayer(position, layerType);
			}

			return pos;
		}

		/// <summary>
		/// Gets all OverlayTiles with a specific overlay type at the cell position
		/// </summary>
		/// <param name="position"></param>
		/// <param name="layerType"></param>
		/// <param name="overlayType"></param>
		/// <returns></returns>
		public List<OverlayTile> GetOverlayTilesByType(Vector3Int position, LayerType layerType,
			OverlayType overlayType)
		{
			if (layerType == LayerType.Objects)
			{
				Logger.LogError("Please use get objects instead of get tile");
				return null;
			}

			TileLocation tileLocation = null;
			OverlayTile overlayTile = null;
			List<OverlayTile> overlayTiles = new List<OverlayTile>();
			position.z = 1;

			if (Layers.TryGetValue(layerType, out var layer))
			{
				//Go through overlays under the overlay limit. The first overlay checked will be at z = 1.
				var count = 0;
				while (count < OVERLAY_LIMIT)
				{
					lock (PresentTiles)
					{
						PresentTiles[layer].TryGetValue(position, out tileLocation);
					}

					if (tileLocation != null)
					{
						overlayTile = tileLocation.Tile as OverlayTile;

						if (overlayTile != null && overlayTile.OverlayType == overlayType)
						{
							overlayTiles.Add(overlayTile);
						}
					}

					position.z++;
					count++;
				}
			}
			else
			{
				LogMissingLayer(position, layerType);
			}

			return overlayTiles;
		}

		/// <summary>
		/// Whether a tile has this overlay already
		/// </summary>
		public bool HasOverlay(Vector3Int position, LayerType layerType, OverlayTile overlayTileWanted)
		{
			if (layerType == LayerType.Objects)
			{
				Logger.LogError("Please use get objects instead of get tile");
				return false;
			}

			TileLocation tileLocation = null;
			OverlayTile overlayTile = null;
			position.z = 1;

			if (Layers.TryGetValue(layerType, out var layer))
			{
				//Go through overlays under the overlay limit. The first overlay checked will be at z = 1.
				var count = 0;
				while (count < OVERLAY_LIMIT)
				{
					lock (PresentTiles)
					{
						PresentTiles[layer].TryGetValue(position, out tileLocation);
					}

					if (tileLocation != null)
					{
						overlayTile = tileLocation.Tile as OverlayTile;

						if (overlayTile != null && overlayTile.Equals(overlayTileWanted))
						{
							return true;
						}
					}

					position.z++;
					count++;
				}
			}
			else
			{
				LogMissingLayer(position, layerType);
			}

			return false;
		}

		/// <summary>
		/// Whether a tile has this overlay already
		/// </summary>
		public bool HasOverlayOfType(Vector3Int position, LayerType layerType, OverlayType overlayTypeWanted)
		{
			if (layerType == LayerType.Objects)
			{
				Logger.LogError("Please use get objects instead of get tile");
				return false;
			}

			TileLocation tileLocation = null;
			OverlayTile overlayTile = null;
			position.z = 1;

			if (Layers.TryGetValue(layerType, out var layer))
			{
				//Go through overlays under the overlay limit. The first overlay checked will be at z = 1.
				var count = 0;
				while (count < OVERLAY_LIMIT)
				{
					lock (PresentTiles)
					{
						PresentTiles[layer].TryGetValue(position, out tileLocation);
					}

					if (tileLocation != null)
					{
						overlayTile = tileLocation.Tile as OverlayTile;

						if (overlayTile != null && overlayTile.OverlayType == overlayTypeWanted)
						{
							return true;
						}
					}

					position.z++;
					count++;
				}
			}
			else
			{
				LogMissingLayer(position, layerType);
			}

			return false;
		}

		public void NotifyRegisterTilePotentialMatrixChange(Vector3Int position)
		{
			if (Application.isPlaying && CustomNetworkManager.Instance._isServer)
			{
				foreach (var ServerObject in ObjectLayer.ServerObjects.Get(position))
				{
					ServerObject.customNetTransform.CheckMatrixSwitch();
				}
			}
		}


		//Use TileChangeManager Instead if you want to me networked
		public void RemoveTile(Vector3Int position)
		{
			NotifyRegisterTilePotentialMatrixChange(position);
			TileLocation TileLcation = null;
			foreach (var layer in LayersValues)
			{
				if (layer.LayerType == LayerType.Objects) continue;

				if (Application.isPlaying == false)
				{
					if (layer.LayerType == LayerType.Underfloor)
					{
						//TODO Tile map upgrade , xyz z = is the z The level so We need one more xyzw w = what w Coordinate on the z Coordinate on the layer the tile is
						//so, Upgrade messages and the entire system to use vector4int
						//but For now since the z is left hanging is ok
						//If it was vector4int then Use that directly
						var positionnew = position;
						for (int i = 0; i < 50; i++)
						{
							positionnew.z = 1 - i;
							if (layer.RemoveTile(positionnew))
							{
								return;
							}
						}
					}
					else
					{
						if (layer.RemoveTile(position))
						{
							return;
						}
					}

					continue;
				}


				if (layer.LayerType == LayerType.Underfloor) //TODO Tile map upgrade
				{
					TileLcation = GetTileExactLocationMultilayer(position, layer);
				}
				else
				{
					lock (PresentTiles)
					{
						PresentTiles[layer].TryGetValue(position, out TileLcation);
					}
				}


				if (TileLcation != null)
				{
					TileLcation.Tile = null;
					TileLcation.OnStateChange();
					return;
				}
			}
		}

		//Use TileChangeManager Instead if you want to me networked
		public void RemoveTileWithlayer(Vector3Int position, LayerType refLayer)
		{
			if (refLayer == LayerType.Objects) return;

			if (Layers.TryGetValue(refLayer, out var layer))
			{
				TileLocation TileLcation = null;

				if (layer.LayerType == LayerType.Underfloor) //TODO Tile map upgrade
				{
					TileLcation = GetTileExactLocationMultilayer(position, layer);
				}
				else
				{
					lock (PresentTiles)
					{
						PresentTiles[layer].TryGetValue(position, out TileLcation);
					}
				}

				if (TileLcation != null)
				{
					TileLcation.Tile = null;
					TileLcation.OnStateChange();
				}
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
			if (presentMatrix?.MatrixMove?.inProgressRotation != null)
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
			LayerTypeSelection layerMask, Vector2? To = null,
			LayerTile[] tileNamesToIgnore = null)
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
				gridOffsetx =
					-(-0.5d +
					  Offsetuntouchx); //0.5f  //this is So when you multiply it gives you 0.5 that some tile borders
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
							if (tileNamesToIgnore != null &&
							    tileNamesToIgnore.Any(c => c.name == TileLcation?.Tile.name)) continue;

							Vector2 normal;

							if (LeftFaceHit)
							{
								normal = Vector2.left * stepX;
							}
							else
							{
								normal = Vector2.down * stepY;
							}


							Vector3 AdjustedNormal = ((Vector3) normal).ToWorld(presentMatrix);
							AdjustedNormal = AdjustedNormal - (Vector3.zero.ToWorld(presentMatrix));


							// Debug.DrawLine(wold, wold + AdjustedNormal, Color.cyan, 30);

							return new MatrixManager.CustomPhysicsHit(((Vector3) vec).ToWorld(presentMatrix),
								(vecHit).ToWorld(presentMatrix), AdjustedNormal,
								new Vector2((float) RelativeX, (float) RelativeY).magnitude, TileLcation);
						}
					}
				}
			}

			return null;
		}

		#endregion

		public bool UnderFloorUtilitiesInitialised { get; private set; } = false;

		public void InitialiseUnderFloorUtilities(bool isServer)
		{
			if (Layers.TryGetValue(LayerType.Underfloor, out var layer))
			{
				var ToInsertDictionary = new Dictionary<Vector3Int, List<TileLocation>>();
				BoundsInt bounds = layer.Tilemap.cellBounds;
				TileLocation Tile = null;
				for (int n = bounds.xMin; n < bounds.xMax; n++)
				{
					for (int p = bounds.yMin; p < bounds.yMax; p++)
					{
						Vector3Int localPlace = (new Vector3Int(n, p, 0));
						bool[] PipeDirCheck = new bool[4];

						for (int i = 0; i < 50; i++)
						{
							localPlace.z = 1 - i;
							var getTile = layer.Tilemap.GetTile(localPlace) as LayerTile;
							Tile = null;
							var localPlacezzero = localPlace;
							localPlacezzero.z = 0;
							if (getTile != null)
							{
								Tile = GetPooledTile();
								Tile.TileCoordinates = localPlace;
								Tile.PresentMetaTileMap = this;
								Tile.PresentlyOn = layer;
								Tile.Tile = getTile;
								Tile.Colour = layer.Tilemap.GetColor(localPlace);
								Tile.TransformMatrix = layer.Tilemap.GetTransformMatrix(localPlace);

								if (isServer)
								{
									var electricalCableTile = getTile as ElectricalCableTile;
									if (electricalCableTile != null)
									{
										layer.matrix.AddElectricalNode(new Vector3Int(n, p, localPlace.z),
											electricalCableTile);
									}

									var disposalPipeTile = getTile as Objects.Disposals.DisposalPipe;
									if (disposalPipeTile != null)
									{
										disposalPipeTile.InitialiseNode(localPlace, layer.matrix);
									}

									var pipeTile = getTile as Objects.Atmospherics.PipeTile;
									if (pipeTile != null)
									{
										var matrixStruct =
											layer.matrix.UnderFloorLayer.Tilemap.GetTransformMatrix(localPlace);
										var connection = PipeTile.GetRotatedConnection(pipeTile, matrixStruct);
										var pipeDir = connection.Directions;
										var canInitializePipe = true;
										for (var d = 0; d < pipeDir.Length; d++)
										{
											if (pipeDir[d].Bool)
											{
												if (PipeDirCheck[d])
												{
													canInitializePipe = false;
													Logger.LogWarning(
														$"A pipe is overlapping its connection at ({n}, {p}) in {layer.matrix.gameObject.scene.name} - {layer.matrix.name} with another pipe, removing one",
														Category.Pipes);
													layer.Tilemap.SetTile(localPlace, null);
													break;
												}

												PipeDirCheck[d] = true;
											}
										}

										if (canInitializePipe)
										{
											pipeTile.InitialiseNode(localPlace, layer.matrix);
										}
									}
								}
							}

							if (!ToInsertDictionary.ContainsKey(localPlacezzero))
							{
								ToInsertDictionary[localPlacezzero] = new List<TileLocation>();
							}

							ToInsertDictionary[localPlacezzero].Add(Tile);
						}

						var AlocalPlacezzero = localPlace;
						AlocalPlacezzero.z = 0;
						bool remove = true;
						int LastIndex = 0;
						int L = 0;
						foreach (var TL in ToInsertDictionary[AlocalPlacezzero])
						{
							if (TL != null)
							{
								remove = false;
								LastIndex = L;
							}

							L++;
						}

						if (remove)
						{
							ToInsertDictionary.Remove(AlocalPlacezzero);
						}
						else
						{
							ToInsertDictionary[AlocalPlacezzero].RemoveRange(LastIndex + 1,
								ToInsertDictionary[AlocalPlacezzero].Count - (LastIndex + 1));
						}
					}
				}

				lock (MultilayerPresentTiles)
				{
					MultilayerPresentTiles[layer] = ToInsertDictionary;
				}
			}

			UnderFloorUtilitiesInitialised = true;
		}

		public IEnumerable<T> GetAllTilesByType<T>(Vector3Int position, LayerType LayerType) where T : LayerTile
		{
			List<T> tiles = new List<T>();

			if (Layers.TryGetValue(LayerType, out var layer))
			{
				if (layer.LayerType == LayerType.Underfloor)
				{
					lock (MultilayerPresentTiles)
					{
						var tileLocations = GetTileLocationsNeedLockSurrounding(position, layer);
						if (tileLocations != null)
						{
							foreach (var tileLocation in tileLocations)
							{
								var tile = tileLocation?.Tile;
								if (tile is T) tiles.Add(tile as T);
							}
						}
					}
				}
				else
				{
					TileLocation TileLcation = null;
					lock (PresentTiles)
					{
						PresentTiles[layer].TryGetValue(position, out TileLcation);
					}

					var tile = TileLcation.Tile;
					if (tile is T) tiles.Add(tile as T);
				}
			}

			return tiles;
		}


		private int FindFirstEmpty(List<TileLocation> LookThroughList)
		{
			int NewIndex = LookThroughList.Count;
			for (var i = 0; i < NewIndex; i++)
			{
				if (LookThroughList[i]?.Tile == null)
				{
					return (i);
				}
			}

			LookThroughList.Add(null);
			return (NewIndex);
		}

		public Matrix4x4? GetMatrix4x4(Vector3Int cellPosition, LayerType layerType, bool UseExactForMultilayer = false)
		{
			if (layerType == LayerType.Objects) return null;
			if (Layers.TryGetValue(layerType, out var layer))
			{
				TileLocation TileLcation = null;
				TileLcation = GetCorrectTileLocationForLayer(cellPosition, layer, UseExactForMultilayer);

				return TileLcation?.TransformMatrix;
			}
			else
			{
				LogMissingLayer(cellPosition, layerType);
			}

			return null;
		}
	}


	public enum OverlayType
	{
		//none is used to say there is no overlay, add new category if you need a new type
		None,
		Gas,
		Damage,
		Cleanable,
		Fire,
		Mining,
		KineticAnimation,
		Plasma,
		NO2,
		WaterVapour,
		Miasma,
		Nitryl,
		Tritium,
		Freon,
		FireSparkles,
		FireOverCharged,
		FireFusion,
		FireRainbow
	}
}