using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Chemistry;
using Cysharp.Threading.Tasks;
using Logs;
using Mirror;
using UnityEngine;
using US13.Core.Addressables;
using US13.Core.Factories;
using US13.Core.GameGizmos;
using US13.Core.Lifecycle;
using US13.Core.Transform;
using US13.Core.Utils;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Items.Others;
using US13.Managers;
using US13.Managers.MatrixManager;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.Messages.Server;
using US13.Objects.Construction.FloorDecals;
using US13.ScriptableObjects;
using US13.Systems.ChemistryInMainAssembly;
using US13.Tilemaps.Behaviours.Meta;
using US13.Tilemaps.Behaviours.Meta.Atmospherics;
using US13.Tilemaps.Behaviours.Meta.Atmospherics.Data;
using US13.Tilemaps.Behaviours.Pathfinding;
using US13.Tilemaps.Utils;
using Util;
using Random = UnityEngine.Random;

namespace US13.Tilemaps.Behaviours.Layers
{
	/// <summary>
	/// Holds and provides functionality for all the MetaDataTiles for a given matrix.
	/// </summary>
	public class MetaDataLayer : MonoBehaviour
	{
		private readonly ChunkedTileMap<MetaDataNode> nodes = new ChunkedTileMap<MetaDataNode>();
		public ChunkedTileMap<MetaDataNode> Nodes => nodes;

		public PressurePathfinder Pathfinder = new PressurePathfinder();

		/// <summary>
		/// //Used for networking, Nodes that have changed In terms of network variables
		/// </summary>
		public Dictionary<Vector3Int, MetaDataNode> ChangedNodes = new Dictionary<Vector3Int, MetaDataNode>();

		public List<MetaDataNode> nodesToUpdate = new List<MetaDataNode>();

		private MetaDataSystem MetaDataSystem;

		private MatrixSystemManager subsystemManager;
		private ReactionManager reactionManager;
		private Matrix matrix;
		public Matrix Matrix => matrix;

		public List<EtherealThing> EtherealThings = new List<EtherealThing>();

		private List<MetaDataNode> trackedTilesWithReagentsOnThem = new List<MetaDataNode>();
		public bool DebugGizmos = false;

		private CancellationToken EvaporationCancellationToken = CancellationToken.None;

		private const float REAGENT_LIMIT_PER_CELL = 60f;
		private const float EVAPORATE_TICK_DURATION = 64f;
		private const float MINIMUM_TEMPERATURE_TO_EVAPORATE_CELSIUS = 29f;

		public void OnEnable()
		{
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Add(CallbackType.UPDATE, SynchroniseNodeChanges);
				UpdateManager.Add(EvaporationTick, EVAPORATE_TICK_DURATION);
			}
		}

		public void OnDisable()
		{
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Remove(CallbackType.UPDATE, SynchroniseNodeChanges);
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, EvaporationTick);
			}

			nodes.Clear();
			ChangedNodes.Clear();
			nodesToUpdate.Clear();
			EtherealThings.Clear();
		}

		private void Awake()
		{
			subsystemManager = GetComponentInParent<MatrixSystemManager>();
			reactionManager = GetComponentInParent<ReactionManager>();
			matrix = GetComponent<Matrix>();
			MetaDataSystem = subsystemManager.GetComponent<MetaDataSystem>();
		}

		public void SynchroniseNodeChanges()
		{
			if (nodesToUpdate.Count > 0)
			{
				MetaDataLayerMessage.Send(gameObject, nodesToUpdate);
				nodesToUpdate.Clear();
			}
		}

		public void EvaporationTick()
		{
			if (EvaporationCancellationToken.IsCancellationRequested)
			{
				EvaporationCancellationToken = new CancellationTokenSource().Token;
			}
			_ = EvaporationCheck();
		}

		private async UniTask EvaporationCheck()
		{
			var safeList = trackedTilesWithReagentsOnThem.Shuffle().ToList();
			for (int i = 0; i < safeList.Count - 1; i++)
			{
				await UniTask.WaitForEndOfFrame(cancellationToken: EvaporationCancellationToken);
				if (EvaporationCancellationToken.IsCancellationRequested) return;
				if (safeList.Count <= 0) break;
				if (trackedTilesWithReagentsOnThem == null || trackedTilesWithReagentsOnThem.Count == 0) break;
				var toRemove = safeList[i];
				if (trackedTilesWithReagentsOnThem.Contains(toRemove) == false) continue;
				if (TemperatureUtils.FromKelvin(toRemove.GasMixLocal.Temperature, TemeratureUnits.C) >= MINIMUM_TEMPERATURE_TO_EVAPORATE_CELSIUS)
				{
					if (toRemove.ReagentsOnTile == null) continue;
					if (toRemove.ReagentsOnTile.MixState == ReagentState.Solid) continue;
					if (toRemove.GasMixLocal != null)
					{
						toRemove.GasMixLocal.AddGas(Gas.WaterVapor, toRemove.ReagentsOnTile.Total * 2,
							toRemove.ReagentsOnTile.InternalEnergy / 2);
					}
					matrix.ReactionManager.ExtinguishHotspot(toRemove.LocalPosition);
					RemoveLiquidOnTile(toRemove.LocalPosition, toRemove);
					trackedTilesWithReagentsOnThem.Remove(toRemove);
				}
			}
		}

		[Server]
		public void UpdateNewPlayer(NetworkConnection requestedBy)
		{
			MetaDataLayerMessage.SendTo(gameObject, requestedBy, ChangedNodes);
		}

		private void OnDestroy()
		{
			//In the case of the matrix remaining in memory after the round ends, this will ensure the MetaDataNodes are GC
			nodes.Clear();
		}

		public MetaDataNode Get(Vector3Int localPosition, bool createIfNotExists = true, bool updateTileOnClient = false)
		{
			localPosition.z = 0; //Z Positions are always on 0

			if (nodes.ContainsKey(localPosition) == false)
			{
				if (createIfNotExists)
				{
					nodes[localPosition] = new MetaDataNode(localPosition, reactionManager, matrix, MetaDataSystem);
				}
				else
				{
					return MetaDataNode.None;
				}
			}

			MetaDataNode node;
			try
			{
				node = nodes[localPosition];
			}
			catch (Exception e)
			{
				Loggy.Error("THIS REALLY SHOULDN'T HAPPEN!");
				Loggy.Error(e.ToString());

				if (createIfNotExists)
				{
					nodes[localPosition] = new MetaDataNode(localPosition, reactionManager, matrix, MetaDataSystem);
					node = nodes[localPosition];
				}
				else
				{
					return MetaDataNode.None;
				}
			}

			if (updateTileOnClient)
			{
				AddNetworkChange(localPosition, node);
			}

			return node;
		}

		public void AddNetworkChange(Vector3Int localPosition, MetaDataNode node)
		{
			nodesToUpdate.Add(node);
			ChangedNodes[localPosition] = node;
		}

		public bool IsSpaceAt(Vector3Int position)
		{
			return Get(position, false).IsSpace;
		}

		public bool HasReagentSpatter(Vector3Int localPosInt)
		{
			return Get(localPosInt).ReagentsOnTile is { Total: >= 0.1f };
		}

		public bool IsRoomAt(Vector3Int position)
		{
			return Get(position, false).IsRoom;
		}

		public bool IsEmptyAt(Vector3Int position)
		{
			return !Get(position, false).Exists;
		}

		public bool IsOccupiedAt(Vector3Int position)
		{
			return Get(position, false).IsOccupied;
		}

		public bool ExistsAt(Vector3Int position)
		{
			return Get(position, false).Exists;
		}

		public bool IsSlipperyAt(Vector3Int position)
		{
			var node = Get(position, false);
			return node.Allslippery;
		}

		public bool IsSuperSlipperyAt(Vector3Int position)
		{
			var node = Get(position, false);
			return node.IsSuperSlippery;
		}


		public void MakeSlipperyAt(Vector3Int position, bool canDryUp = true, bool SuperSlippery = false)
		{
			var tile = Get(position, false, true);
			if (tile == MetaDataNode.None || tile.IsSpace)
			{
				return;
			}

			if (SuperSlippery == false)
			{
				tile.IsSlippery = true;
			}
			else
			{
				tile.IsSuperSlippery = true;
			}

			if (canDryUp)
			{
				if (tile.CurrentDrying != null)
				{
					StopCoroutine(tile.CurrentDrying);
				}

				tile.CurrentDrying = DryUp(tile);
				StartCoroutine(tile.CurrentDrying);
			}
		}

		/// <summary>
		/// Release reagents at provided coordinates, making them react with world + decide what it should look like
		/// </summary>
		public void ReagentReact(ReagentMix reagents, Vector3 worldPos, Vector3Int localPosInt,
			bool spawnPrefabEffect = true, OrientationEnum direction = OrientationEnum.Up_By0, bool Scatter = false,
			LivingHealthMasterBase from = null, BodyPartType bodyPartAim = BodyPartType.None)
		{
			var mobs = MatrixManager.GetAt<LivingHealthMasterBase>(worldPos, true);

			if (mobs.Count() > 0)
			{
				mobs = mobs.Where(x => x != from);
			}

			reagents.Divide(mobs.Count() + 1);
			//splashes mobs
			foreach (var mob in mobs)
			{
				mob.ApplyReagentsToSurface(reagents, bodyPartAim);
			}

			Vector3 Position = worldPos;
			Vector3 Offset = Vector3.zero;
			Vector3 PositionLocal = localPosInt;

			if (Scatter)
			{
				//(Max): magic numbers, what do they do?
				//break the code or help it through.
				//constants buried, no name, no face,
				//lurking deep in every place.
				Offset = Offset.GetRandomScatteredDirection() +
				         new Vector3(Random.Range(-0.1875f, 0.1875f), Random.Range(-0.1875f, 0.1875f));
				Position = worldPos + Offset;
				PositionLocal = PositionLocal + Offset;
			}

			//var worldPosInt = Position.RoundToInt();
			localPosInt = PositionLocal.RoundToInt();

			if (MatrixManager.IsTotallyImpassable(Position, true)) return;

			bool didSplat = false;
			bool paintBlood = false;

			var existingSplats = MatrixManager.GetAt<FloorDecal>(Position, true);

			if (reagents.Total > 0)
			{
				try
				{
					HandleSplats(ref reagents, ref didSplat, Position, worldPos,
						localPosInt, existingSplats, spawnPrefabEffect);
				}
				catch (Exception e)
				{
					Loggy.Error(e.ToString());
				}
			}
		}

		private void HandleSplats(ref ReagentMix reagents, ref bool didSplat,
			Vector3 position, Vector3 worldPos, Vector3Int localPosInt, IEnumerable<FloorDecal> ExistingDecals, bool spawnPrefabEffect = true)
		{
			lock (reagents.reagents)
			{
				if (reagents.MajorMixReagent == CommonReagents.Instance.SpaceCleaner)
				{
					this.Clean(worldPos, localPosInt, false);
				}
				else if (reagents.MajorMixReagent == CommonReagents.Instance.SpaceLube)
				{
					// ( ͡° ͜ʖ ͡°)
					if (Get(localPosInt).IsSuperSlippery == false)
					{
						EffectsFactory.WaterSplat(worldPos, wasLube: true);
						MakeSlipperyAt(localPosInt, false, true);
					}
				}
				else
				{
					//(Max): This sucks ass. Stop hardcoding this shit in places that it doesn't belong to, and make it part of the reagent logic itself.
					//TODO: Refactor this part so that Reagents handle their own logic for splats instead of MetaDataLayer.
					if (reagents.MajorMixReagent == CommonReagents.Instance.Water)
					{
						MakeSlipperyAt(localPosInt, false);
						matrix.ReactionManager.ExtinguishHotspot(localPosInt);
						foreach (var livingHealthBehaviour in matrix.Get<LivingHealthMasterBase>(localPosInt, true))
						{
							livingHealthBehaviour.Extinguish();
						}

						didSplat = true;
						EffectsFactory.WaterSplat(worldPos);
					}

					if (reagents.MajorMixReagent == CommonReagents.Instance.Blood)
					{
						var PreExisting = 0f;
						var loop = ExistingDecals.ToList();
						var cell = matrix.GetMetaDataNode(localPosInt);

						PreExisting += cell.ReagentsOnTile.Total;
						foreach (var Container in loop)
						{
							PreExisting += Container.ReagentContainer.ReagentMixTotal;
						}


						if (PreExisting + reagents.Total <= 30)
						{
							if (PreExisting == 0)
							{
								if (spawnPrefabEffect)
								{
									PaintBlood(position, reagents);
									return;
								}
							}
							else
							{
								if (loop.Count > 0)
								{
									loop[0].ReagentContainer.Add(reagents);
								}
							}
						}
						else
						{
							foreach (var Container in loop)
							{
								reagents.Add(Container.ReagentContainer.CurrentReagentMix);
								Container.ReagentContainer.CurrentReagentMix.Clear();
								//_ = Despawn.ServerSingle(Container.gameObject);
							}
						}
					}

					StoreReagentsAtTile(reagents, localPosInt);
				}
			}
		}

		public void PaintBlood(Vector3 worldPos, ReagentMix reagents)
		{
			EffectsFactory.BloodSplat(worldPos, reagents);
			BloodDry(worldPos.RoundToInt());
		}

		public Color GetTileColourMix(ReagentMix reagents)
		{
			//transparent liquids don't need their alpha bumped when their puddles are full.
			if (reagents.MajorMixReagent.color.a <= Reagent.MINIMUM_PUDDLE_OPACITY) return reagents.MixColor;
			float fillRatio = Mathf.Clamp01(reagents.Total / REAGENT_LIMIT_PER_CELL);
			switch (reagents.MixState)
			{
				case ReagentState.Liquid:
					var liquidColor = reagents.MixColor;
					liquidColor.a = Mathf.Lerp(0.35f, 0.95f, fillRatio); //makes sure liquids don't completely hide everything behind it.
					return liquidColor;
				case ReagentState.Gas:
				case ReagentState.Solid:
					var SolidColor = reagents.MixColor;
					SolidColor.a = Mathf.Clamp(SolidColor.a, 0.25f, 1f); //makes sure solids aren't invisible
					return SolidColor;
			}

			return Color.white;
		}

		public (Vector3Int position,  ReagentState TileState) CreateReagentOverlay(Vector3Int localPosInt, ReagentMix reagents)
		{
			switch (reagents.MixState)
			{
				case ReagentState.Liquid:
					var tile = CommonTiles.Instance.Liquid;

					if (reagents.Total > REAGENT_LIMIT_PER_CELL * 0.75f)
					{
						tile = CommonTiles.Instance.LiquidBig;
					}


					var Position = matrix.MetaTileMap.AddOverlay(localPosInt, tile, color: GetTileColourMix(reagents));
					SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Bubbles, localPosInt);
					return (Position, reagents.MixState);
				case ReagentState.Gas:
				case ReagentState.Solid:
					var Position1 = Vector3Int.back;
					if (reagents.MajorMixReagent.name == "TableSalt")
					{
						Position1 = matrix.MetaTileMap.AddOverlay(localPosInt, CommonTiles.Instance.PowderSalt, color: GetTileColourMix(reagents));
					}
					else
					{
						Position1 = matrix.MetaTileMap.AddOverlay(localPosInt, CommonTiles.Instance.Powder, color: GetTileColourMix(reagents));
					}

					return (Position1, ReagentState.Solid);
			}
			return (Vector3Int.zero, ReagentState.Solid);;
		}


		public void RemoveLiquidOnTile(Vector3Int localPosInt, MetaDataNode node)
		{
			if (node.ReagentOverlayTileLocation == null) return;
			matrix.MetaTileMap.RemoveTileWithlayer(node.ReagentOverlayTileLocation.Value, LayerType.UnderObjectsEffects, true);
			node.ReagentsOnTile.Clear();
		}



		public void Paintsplat(Vector3 worldPos, Vector3Int localPosInt, ReagentMix reagents)
		{
			switch (ChemistryUtils.GetMixStateDescription(reagents))
			{
				case "powder":
				{
					EffectsFactory.PowderSplat(worldPos, reagents.MixColor, reagents);
					break;
				}
				case "liquid":
				{
					//TODO: Work out if reagent is "slippery" according to its viscocity (not modeled yet)
					EffectsFactory.ChemSplat(worldPos, reagents.MixColor, reagents);
					break;
				}
				case "gas":
					//TODO: Make gas reagents release into the atmos.
					break;
				default:
				{
					EffectsFactory.ChemSplat(worldPos, reagents.MixColor, reagents);
					break;
				}
			}
		}

		public void Clean(Vector3 worldPos, Vector3Int localPosInt, bool makeSlippery)
		{
			var node = Get(localPosInt, updateTileOnClient: true);
			node.IsSlippery = makeSlippery;
			node.IsSuperSlippery = false;

			var floorDecals = GetFloorDecals(worldPos);

			foreach (var floorDecal in floorDecals)
			{
				floorDecal.TryClean();
			}

			//check for any moppable overlays
			matrix.TileChangeManager.MetaTileMap.RemoveFloorWallOverlaysOfType(localPosInt, OverlayType.Cleanable);
			matrix.MetaDataLayer.RemoveLiquidOnTile(localPosInt, matrix.MetaDataLayer.Matrix.GetMetaDataNode(localPosInt));


			if (MatrixManager.IsSpaceAt(worldPos, true, matrix.MatrixInfo) == false && makeSlippery)
			{
				// Create a WaterSplat Decal (visible slippery tile)
				EffectsFactory.WaterSplat(worldPos);

				// Sets a tile to slippery
				MakeSlipperyAt(localPosInt);
			}
		}

		public void CleanAndMoveToExcess(Vector3Int worldPosInt, Vector3Int localPosInt, ReagentMix reagents)
		{
			//TODO Clean everything except blood hummmmmmmmm

			if (reagents.MixState is ReagentState.Liquid or ReagentState.Gas)
			{
				List<FloorDecal> floorDecals = GetFloorDecals(worldPosInt).ToList();
				if (floorDecals.Count == 0) return;
				foreach (var floorDecal in floorDecals)
				{
					if (floorDecal.ReagentContainer?.IsEmpty == false)
					{
						reagents.Add(floorDecal.ReagentContainer.CurrentReagentMix.Clone());
						floorDecal.ReagentContainer.CurrentReagentMix.Clear();
					}

					if ((floorDecal.isBlood == false || reagents.MixState is not ReagentState.Liquid )  )
					{
						floorDecal.TryClean();
					}

				}

				//check for any moppable overlays
				matrix.TileChangeManager.MetaTileMap.RemoveFloorWallOverlaysOfType(localPosInt, OverlayType.Cleanable);
			}



		}

		private IEnumerable<FloorDecal> GetFloorDecals(Vector3 worldPos)
		{
			return MatrixManager.GetAt<FloorDecal>(worldPos, isServer: true);
		}

		public void BloodDry(Vector3Int position)
		{
			var tile = Get(position, false);
			if (tile == MetaDataNode.None || tile.IsSpace)
			{
				return;
			}

			if (tile.CurrentDrying != null)
			{
				StopCoroutine(tile.CurrentDrying);
			}

			tile.CurrentDrying = BloodDryUp(tile);
			StartCoroutine(tile.CurrentDrying);
		}

		private IEnumerator BloodDryUp(MetaDataNode tile)
		{
			//Blood should take 3 mins to dry (TG STATION)
			yield return WaitFor.Seconds(180);
			tile.IsSlippery = false;

			var floorDecals = matrix.Get<FloorDecal>(tile.LocalPosition, isServer: true);
			foreach (var decal in floorDecals)
			{
				if (decal.isBlood)
				{
					decal.color = new Color(decal.color.r / 2, decal.color.g / 2, decal.color.b / 2, decal.color.a);
					decal.name = $"dried {decal.name}";
				}
			}
		}

		private IEnumerator DryUp(MetaDataNode tile)
		{
			yield return WaitFor.Seconds(Random.Range(10, 21));
			tile.IsSlippery = false;
			tile.ForceUpdateClient();
			var floorDecals = matrix.Get<FloorDecal>(tile.LocalPosition, isServer: true);
			foreach (var decal in floorDecals)
			{
				if (decal.CanDryUp)
				{
					_ = Despawn.ServerSingle(decal.gameObject);
				}
			}
		}

		public void UpdateSystemsAt(Vector3Int localPosition, SystemType ToUpDate = SystemType.All)
		{
			subsystemManager.UpdateAt(localPosition, ToUpDate);
		}

		public void StoreReagentsAtTile(ReagentMix reagents, Vector3Int localPosInt)
		{
			var cell = matrix.GetMetaDataNode(localPosInt);
			bool excess = false;
			ReagentMix reagentsToUse = reagents.Clone();

			cell.ReagentsOnTile.Add(reagentsToUse);

			CleanAndMoveToExcess(localPosInt.ToWorldInt(matrix), localPosInt,cell.ReagentsOnTile);
			if (cell.ReagentsOnTile.Total >= REAGENT_LIMIT_PER_CELL)
			{
				//cell.ReagentsOnTile.Take(cell.ReagentsOnTile.Total - REAGENT_LIMIT_PER_CELL);
				excess = true;
			}

			ChemistryManager.ReagentsChanged(
				null,
				cell.ReagentsOnTile,
				null,
				cell.possibleReactions,
				null,
				cell.WorldPosition);

			SetOrUpdateTile(cell, localPosInt);
			if (excess) DistributeExcessToNearbyCells(localPosInt);
		}

		public void SetOrUpdateTile(MetaDataNode cell, Vector3Int localPosInt)
		{
			if (cell.ReagentOverlayTileLocation == null)
			{
				trackedTilesWithReagentsOnThem.Add(cell);

				var data =  CreateReagentOverlay(localPosInt, cell.ReagentsOnTile);
				cell.ReagentOverlayTileLocation = data.position;
				cell.ReagentStateTile = data.TileState;
				if (DebugGizmos)
				{
					GameGizmomanager.AddNewSquareStaticClient(null, localPosInt.ToWorldInt(matrix), Color.green);
				}
			}
			else
			{
				if (cell.ReagentsOnTile.MixState != cell.ReagentStateTile)
				{
					matrix.MetaTileMap.RemoveTileWithlayer(cell.ReagentOverlayTileLocation.Value, LayerType.UnderObjectsEffects, true);
					var data =  CreateReagentOverlay(localPosInt, cell.ReagentsOnTile);
					cell.ReagentOverlayTileLocation = data.position;
					cell.ReagentStateTile = data.TileState;
				}
				else
				{
					var col = GetTileColourMix(cell.ReagentsOnTile);
					matrix.MetaTileMap.SetColour(cell.ReagentOverlayTileLocation.Value, LayerType.UnderObjectsEffects, col, true);
				}
			}
		}

		private void DistributeExcessToNearbyCells(Vector3Int origin)
		{
			HashSet<Vector3Int> PositionsVisited = new HashSet<Vector3Int>();

			const int MAX_ITERATIONS = 450;
			var iterations = 0;
			var cellsToProcess = new Queue<MetaDataNode>();
			cellsToProcess.Enqueue(matrix.GetMetaDataNode(origin));
			//(Max): This behavior for some reason breaks when the server is running at low FPS or heavily stuttring. (less than 20)
			//I have no clue what's the cause, or how to mitgate this.
			//You can emulate this issue by enabling gizmos in the editor and watching the FPS drop, then testing this.
			while (cellsToProcess.Count > 0 && iterations < MAX_ITERATIONS)
			{
				var currentCell = cellsToProcess.Dequeue();
				PositionsVisited.Add(currentCell.LocalPosition);
				if (currentCell.ReagentsOnTile.Total <= REAGENT_LIMIT_PER_CELL) continue;
				var loop = RNG.GetRandomDirectionLoop();
				int AvailableNeighbours = 0;

				foreach (var NeighbourPosition in loop)
				{

					var neighbor = currentCell.Neighbors[NeighbourPosition];
					if (neighbor == null) continue;
					if (PositionsVisited.Contains(neighbor.LocalPosition)) continue;
					// Skip if the neighbor is not passable
					if (neighbor.IsOccupied) continue;
					AvailableNeighbours++;
				}

				var splitMix = currentCell.ReagentsOnTile.Take(currentCell.ReagentsOnTile.Total - REAGENT_LIMIT_PER_CELL);

				if (AvailableNeighbours == 0) continue;

				splitMix.Divide(AvailableNeighbours);

				foreach (var NeighbourPosition in loop)
				{

					var neighbor = currentCell.Neighbors[NeighbourPosition];

					// Skip neighbour spread if they are null => Implies matrix might not yet be initialised
					if (neighbor == null) continue;
					if (PositionsVisited.Contains(neighbor.LocalPosition)) continue;

					// Skip if the neighbor is not passable
					if (neighbor.IsOccupied) continue;
					// If the neighboring cell is empty, add the excess and break up the amount

					if (currentCell.ReagentsOnTile.Total > 5)
					{
						matrix.ReactionManager.ExtinguishHotspot(neighbor.LocalPosition);
					}

					neighbor.ReagentsOnTile.Add(splitMix);

					CleanAndMoveToExcess(neighbor.LocalPosition.ToWorldInt(matrix), neighbor.LocalPosition,neighbor.ReagentsOnTile);

					ChemistryManager.ReagentsChanged(null, neighbor.ReagentsOnTile, null, neighbor.possibleReactions, null,
						neighbor.WorldPosition);

					SetOrUpdateTile(neighbor, neighbor.LocalPosition);

					cellsToProcess.Enqueue(neighbor);
				}

				iterations++;
			}
		}
	}
}