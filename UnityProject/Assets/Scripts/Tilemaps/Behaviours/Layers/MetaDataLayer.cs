using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Systems.Atmospherics;
using Chemistry;
using Chemistry.Components;
using Core.Factories;
using Doors;
using HealthV2;
using InGameGizmos;
using Items;
using Logs;
using Messages.Server;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using Objects.Construction;
using TileManagement;
using Tilemaps.Behaviours.Pathfinding;
using Random = UnityEngine.Random;

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

	private const float REAGENT_LIMIT_PER_CELL = 10f;
	private const float EVAPORATE_TICK_DURATION = 64f;

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
		StartCoroutine(EvaporateCheck());
	}

	private IEnumerator EvaporateCheck()
	{
		var safeList = trackedTilesWithReagentsOnThem.Shuffle().ToList();
		for (int i = 0; i < safeList.Count - 1; i++)
		{
			yield return WaitFor.EndOfFrame;
			if (safeList.Count <= 0) break;
			if (trackedTilesWithReagentsOnThem == null || trackedTilesWithReagentsOnThem.Count == 0) break;
			var toRemove = safeList[i];
			if (trackedTilesWithReagentsOnThem.Contains(toRemove) == false) continue;
			if (TemperatureUtils.FromKelvin(toRemove.GasMixLocal.Temperature, TemeratureUnits.C) >= 32f)
			{
				toRemove.GasMixLocal.AddGas(CommonGasses.Instance.WaterVapor, toRemove.ReagentsOnTile.Total * 2,
					toRemove.ReagentsOnTile.InternalEnergy / 2);
				RemoveLiquidOnTile(toRemove.LocalPosition, toRemove);
				continue;
			}

			TimeSpan timeDifference = DateTime.UtcNow - toRemove.ReagentsOnTile.LastModificationTime;
			if (timeDifference.TotalSeconds + toRemove.ReagentsOnTile.reagents.Count <= 35) continue;
			RemoveLiquidOnTile(toRemove.LocalPosition, toRemove);
			yield return WaitFor.EndOfFrame;
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
	public void ReagentReact(ReagentMix reagents, Vector3Int worldPosInt, Vector3Int localPosInt,
		bool spawnPrefabEffect = true, OrientationEnum direction = OrientationEnum.Up_By0, bool Scatter = false,
		LivingHealthMasterBase from = null)
	{
		var mobs = MatrixManager.GetAt<LivingHealthMasterBase>(worldPosInt, true);

		if (mobs.Count() > 0)
		{
			mobs = mobs.Where(x => x != from);
		}

		reagents.Divide(mobs.Count() + 1);
		//splashes mobs
		foreach (var mob in mobs)
		{
			mob.ApplyReagentsToSurface(reagents, BodyPartType.None);
		}

		Vector3 Position = worldPosInt;
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
			Position = worldPosInt + Offset;
			PositionLocal = PositionLocal + Offset;
		}

		worldPosInt = Position.RoundToInt();
		localPosInt = PositionLocal.RoundToInt();

		if (MatrixManager.IsTotallyImpassable(worldPosInt, true)) return;

		bool didSplat = false;
		bool paintBlood = false;

		//Find all reagents on this tile (including current reagent)
		var reagentContainer = MatrixManager.GetAt<ReagentContainer>(worldPosInt, true)
			.Where(x => x.ExamineAmount == ReagentContainer.ExamineAmountMode.UNKNOWN_AMOUNT);

		var existingSplats = MatrixManager.GetAt<FloorDecal>(worldPosInt, true);
		bool existingSplat = false;

		foreach (var _existingSplat in existingSplats)
		{
			if (_existingSplat.GetComponent<ReagentContainer>() == false) continue;
			existingSplat = true;
		}

		var reagentClone = reagents.Clone();
		reagentClone.Divide(reagentContainer.Count());

		//Loop though all reagent containers and add the passed in reagents
		foreach (ReagentContainer chem in reagentContainer)
		{
			//If the reagent tile already has a pool/puddle/splat
			if (chem.ExamineAmount == ReagentContainer.ExamineAmountMode.UNKNOWN_AMOUNT)
			{
				chem.Add(reagentClone); //TODO Duplication glitch
				existingSplat = true;
			}
			//TODO: could allow you to add this to other container types like beakers but would need some balance and perhaps knocking over the beaker
		}

		if (reagents.Total > 0)
		{
			try
			{
				HandleSplats(ref reagents, ref paintBlood, ref didSplat, ref existingSplat, Position, worldPosInt,
					localPosInt, spawnPrefabEffect);
			}
			catch (Exception e)
			{
				Loggy.Error(e.ToString());
			}
		}
	}

	private void HandleSplats(ref ReagentMix reagents, ref bool paintBlood, ref bool didSplat, ref bool existingSplat,
		Vector3 position, Vector3Int worldPosInt, Vector3Int localPosInt, bool spawnPrefabEffect = true)
	{
		lock (reagents.reagents)
		{
			//reagents.MajorMixReagent

			if (reagents.MajorMixReagent.state == ReagentState.Liquid && reagents.Total > 20)
			{
				if (reagents.reagents.ContainsKey(CommonReagents.Instance.SmokePowder) == false &&
				    reagents.reagents.ContainsKey(CommonReagents.Instance.Fluorosurfactant) == false)
				{
					StoreReagentsAtTile(reagents, localPosInt);
				}
			}

			//(Max): Whoever hardcoded this, I hope you step on lego.
			//TODO: Move this beavior on regeants using interface selectors.

			if (reagents.MajorMixReagent == CommonReagents.Instance.Blood)
			{
				paintBlood = true;
			}
			else if (reagents.MajorMixReagent == CommonReagents.Instance.Water)
			{
				MakeSlipperyAt(localPosInt, false);
				matrix.ReactionManager.ExtinguishHotspot(localPosInt);
				foreach (var livingHealthBehaviour in matrix.Get<LivingHealthMasterBase>(localPosInt, true))
				{
					livingHealthBehaviour.Extinguish();
				}

				didSplat = true;
				EffectsFactory.WaterSplat(worldPosInt);
			}
			else if (reagents.MajorMixReagent == CommonReagents.Instance.SpaceCleaner)
			{
				Clean(worldPosInt, localPosInt, false);
				didSplat = true;
			}
			else if (reagents.MajorMixReagent == CommonReagents.Instance.SpaceLube)
			{
				// ( ͡° ͜ʖ ͡°)
				if (Get(localPosInt).IsSuperSlippery == false)
				{
					didSplat = true;
					EffectsFactory.WaterSplat(worldPosInt, wasLube : true);
					MakeSlipperyAt(localPosInt, false, true);
				}
			}
		}

		if (spawnPrefabEffect && existingSplat == false)
		{
			if (didSplat == false)
			{
				if (paintBlood)
				{
					PaintBlood(position, reagents);
				}
				else
				{
					Paintsplat(worldPosInt, localPosInt, reagents);
				}
			}
		}
	}

	public void PaintBlood(Vector3 worldPos, ReagentMix reagents)
	{
		EffectsFactory.BloodSplat(worldPos, reagents);
		BloodDry(worldPos.RoundToInt());
	}

	public void CreateLiquidOverlay(Vector3Int localPosInt, ReagentMix reagents)
	{
		var liquidColor = reagents.MixColor;
		liquidColor.a =
			Mathf.Clamp(liquidColor.a, 0, 0.65f); //makes sure liquids don't completely hide everything behind it.
		matrix.MetaTileMap.AddOverlay(localPosInt, TileType.UnderObjectsEffects, "BaseLiquid", color: liquidColor);
		SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Bubbles, localPosInt);
	}

	public void RemoveLiquidOverlayOnTile(Vector3Int localPosInt)
	{
		matrix.MetaTileMap.RemoveOverlaysOfType(localPosInt, LayerType.UnderObjectsEffects, OverlayType.Liquid);
	}

	public void RemoveLiquidOnTile(Vector3Int localPosInt, MetaDataNode node)
	{
		RemoveLiquidOverlayOnTile(localPosInt);
		node.ReagentsOnTile.Clear();
	}

	public void Paintsplat(Vector3Int worldPosInt, Vector3Int localPosInt, ReagentMix reagents)
	{
		switch (ChemistryUtils.GetMixStateDescription(reagents))
		{
			case "powder":
			{
				EffectsFactory.PowderSplat(worldPosInt, reagents.MixColor, reagents);
				break;
			}
			case "liquid":
			{
				//TODO: Work out if reagent is "slippery" according to its viscocity (not modeled yet)
				EffectsFactory.ChemSplat(worldPosInt, reagents.MixColor, reagents);
				break;
			}
			case "gas":
				//TODO: Make gas reagents release into the atmos.
				break;
			default:
			{
				EffectsFactory.ChemSplat(worldPosInt, reagents.MixColor, reagents);
				break;
			}
		}
	}

	public void Clean(Vector3Int worldPosInt, Vector3Int localPosInt, bool makeSlippery)
	{
		var node = Get(localPosInt, updateTileOnClient: true);
		node.IsSlippery = makeSlippery;
		node.IsSuperSlippery = false;

		var floorDecals = GetFloorDecals(worldPosInt);

		foreach (var floorDecal in floorDecals)
		{
			floorDecal.TryClean();
		}

		//check for any moppable overlays
		matrix.TileChangeManager.MetaTileMap.RemoveFloorWallOverlaysOfType(localPosInt, OverlayType.Cleanable);

		if (MatrixManager.IsSpaceAt(worldPosInt, true, matrix.MatrixInfo) == false && makeSlippery)
		{
			// Create a WaterSplat Decal (visible slippery tile)
			EffectsFactory.WaterSplat(worldPosInt);

			// Sets a tile to slippery
			MakeSlipperyAt(localPosInt);
		}
	}

	public void CleanAndMoveToExcess(Vector3Int worldPosInt, Vector3Int localPosInt, bool makeSlippery)
	{
		List<FloorDecal> floorDecals = GetFloorDecals(worldPosInt).ToList();
		if (floorDecals.Count == 0) return;
		Get(localPosInt, updateTileOnClient: true).IsSlippery = false;
		MetaDataNode node = matrix.GetMetaDataNode(localPosInt);
		foreach (var floorDecal in floorDecals)
		{
			if (floorDecal.ReagentContainer?.IsEmpty == false)
			{
				node.ReagentsOnTile.Add(floorDecal.ReagentContainer.CurrentReagentMix.Clone());
				//update overlay to show color change.
				matrix.MetaTileMap.RemoveOverlaysOfType(localPosInt, LayerType.UnderObjectsEffects, OverlayType.Liquid);
				CreateLiquidOverlay(localPosInt, node.ReagentsOnTile);
			}

			floorDecal.TryClean();
		}

		//check for any moppable overlays
		matrix.TileChangeManager.MetaTileMap.RemoveFloorWallOverlaysOfType(localPosInt, OverlayType.Cleanable);
		if (MatrixManager.IsSpaceAt(worldPosInt, true, matrix.MatrixInfo) == false && makeSlippery)
		{
			// Create a WaterSplat Decal (visible slippery tile)
			EffectsFactory.WaterSplat(worldPosInt);
			// Sets a tile to slippery
			MakeSlipperyAt(localPosInt);
		}
	}

	private IEnumerable<FloorDecal> GetFloorDecals(Vector3Int worldPosInt)
	{
		return MatrixManager.GetAt<FloorDecal>(worldPosInt, isServer: true);
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
		ReagentMix excess = null;
		ReagentMix reagentsToUse = reagents.Clone();
		if (cell.ReagentsOnTile.Total <= 0.1f)
		{
			reagentsToUse.TransferTo(cell.ReagentsOnTile, reagentsToUse.Total);
			cell.ReagentsOnTile.Add(reagentsToUse);
			trackedTilesWithReagentsOnThem.Add(cell);
			if (cell.ReagentsOnTile.Total >= REAGENT_LIMIT_PER_CELL)
			{
				excess = cell.ReagentsOnTile.Split(reagents.Total / 2);
			}

			CreateLiquidOverlay(localPosInt, cell.ReagentsOnTile);
			if (DebugGizmos)
			{
				GameGizmomanager.AddNewSquareStaticClient(null, localPosInt.ToWorldInt(matrix), Color.green);
			}
		}
		else
		{
			if (cell.ReagentsOnTile.Total >= REAGENT_LIMIT_PER_CELL)
			{
				excess = reagentsToUse.Split(reagentsToUse.Total / 2);
			}

			cell.ReagentsOnTile.Add(reagentsToUse);
		}

		if (excess != null) DistributeExcessToNearbyCells(excess, localPosInt);
	}

	private void DistributeExcessToNearbyCells(ReagentMix excess, Vector3Int origin)
	{
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
			foreach (var neighbor in currentCell.Neighbors)
			{
				if (excess.Total <= 0) return;

				// Skip if the neighbor is not passable
				if (neighbor.IsOccupied) continue;
				// If the neighboring cell is empty, add the excess and break up the amount
				if (neighbor.ReagentsOnTile.Total <= 0)
				{
					neighbor.ReagentsOnTile.Add(excess.Split(excess.Total / 2));
					CreateLiquidOverlay(neighbor.LocalPosition, neighbor.ReagentsOnTile);
					trackedTilesWithReagentsOnThem.Add(neighbor);
					if (DebugGizmos)
					{
						GameGizmomanager.AddNewLineStaticClient(null,
							currentCell.LocalPosition.ToWorldInt(matrix), null,
							neighbor.LocalPosition.ToWorldInt(matrix), Color.red);
					}

					// Add this neighbor to the queue for further distribution if needed
					cellsToProcess.Enqueue(neighbor);
					CleanAndMoveToExcess(neighbor.LocalPosition.ToWorldInt(matrix), neighbor.LocalPosition, false);
					if (excess.Total > 5)
					{
						matrix.ReactionManager.ExtinguishHotspot(neighbor.LocalPosition);
					}

					continue;
				}
				else
				{
					var availableSpace = REAGENT_LIMIT_PER_CELL - neighbor.ReagentsOnTile.Total;
					var transferMix = excess.Split(Math.Min(excess.Total, availableSpace));
					neighbor.ReagentsOnTile.Add(transferMix);
					CleanAndMoveToExcess(neighbor.LocalPosition.ToWorldInt(matrix), neighbor.LocalPosition, false);
				}

				cellsToProcess.Enqueue(neighbor);
			}

			iterations++;
		}
	}
}