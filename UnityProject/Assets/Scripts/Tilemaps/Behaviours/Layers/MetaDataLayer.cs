using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Systems.Atmospherics;
using Chemistry;
using Chemistry.Components;
using Core.Factories;
using HealthV2;
using Items;
using Logs;
using Messages.Server;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using Objects.Construction;
using TileManagement;
using Random = UnityEngine.Random;

/// <summary>
/// Holds and provides functionality for all the MetaDataTiles for a given matrix.
/// </summary>
public class MetaDataLayer : MonoBehaviour
{

	private Dictionary<Vector3Int, MetaDataNode> nodes = new Dictionary<Vector3Int, MetaDataNode>();


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
	private FloorDecal existingSplat;

	public Dictionary<GameObject, Vector3> InitialObjects = new Dictionary<GameObject, Vector3>();


	public void OnEnable()
	{
		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Add(CallbackType.UPDATE, SynchroniseNodeChanges);
		}
	}

	public void OnDisable()
	{
		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Remove(CallbackType.UPDATE, SynchroniseNodeChanges);
		}
		nodes.Clear();
		ChangedNodes.Clear();
		nodesToUpdate.Clear();
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
			Loggy.LogError("THIS REALLY SHOULDN'T HAPPEN!");
			Loggy.LogError(e.ToString());

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

	public void AddNetworkChange(Vector3Int localPosition, MetaDataNode  node)
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
		return Get(position, false).IsSlippery;
	}

	public void MakeSlipperyAt(Vector3Int position, bool canDryUp = true)
	{
		var tile = Get(position, false,true);
		if (tile == MetaDataNode.None || tile.IsSpace)
		{
			return;
		}
		tile.IsSlippery = true;

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
	///
	public void ReagentReact(ReagentMix reagents, Vector3Int worldPosInt, Vector3Int localPosInt, bool spawnPrefabEffect = true, OrientationEnum direction = OrientationEnum.Up_By0)
	{
		var mobs = MatrixManager.GetAt<LivingHealthMasterBase>(worldPosInt, true);
		reagents.Divide(mobs.Count() + 1);
		foreach (var mob in mobs)
		{
			mob.ApplyReagentsToSurface(reagents, BodyPartType.None);
		}

		if (MatrixManager.IsTotallyImpassable(worldPosInt, true)) return;

		bool didSplat = false;
		bool paintBlood = false;

		//Find all reagents on this tile (including current reagent)
		var reagentContainer = MatrixManager.GetAt<ReagentContainer>(worldPosInt, true);
		var existingSplats = MatrixManager.GetAt<FloorDecal>(worldPosInt, true);

		foreach (var _existingSplat in existingSplats)
		{
			if (_existingSplat.GetComponent<ReagentContainer>())
			{
				existingSplat = _existingSplat;
			}
		}

		//Loop though all reagent containers and add the passed in reagents
		foreach (ReagentContainer chem in reagentContainer)
		{
			//If the reagent tile already has a pool/puddle/splat
			if (chem.ExamineAmount == ReagentContainer.ExamineAmountMode.UNKNOWN_AMOUNT)
			{
				chem.Add(reagents);; //TODO Duplication glitch
			}
			//TODO: could allow you to add this to other container types like beakers but would need some balance and perhaps knocking over the beaker
		}

		if(reagents.Total > 0)
		{
			lock (reagents.reagents)
			{
				foreach (var reagent in reagents.reagents.m_dict)
				{
					switch (reagent.Key.name)
					{
						case "HumanBlood":
						{
							paintBlood = true;
							break;
						}
						case "Water":
						{
							MakeSlipperyAt(localPosInt);
							matrix.ReactionManager.ExtinguishHotspot(localPosInt);
							foreach (var livingHealthBehaviour in matrix.Get<LivingHealthMasterBase>(localPosInt, true))
							{
								livingHealthBehaviour.Extinguish();
							}
							break;
						}
						case "SpaceCleaner":
							Clean(worldPosInt, localPosInt, false);
							didSplat = true;
							break;
						case "SpaceLube":
						{
							// ( ͡° ͜ʖ ͡°)
							if (Get(localPosInt).IsSlippery == false)
							{
								EffectsFactory.WaterSplat(worldPosInt);
								MakeSlipperyAt(localPosInt, false);
							}
							break;
						}
						default:
							break;
					}
				}
			}

			if (spawnPrefabEffect)
			{
				if (didSplat == false)
				{
					if (paintBlood)
					{
						PaintBlood(worldPosInt, reagents);
					}
					else
					{
						Paintsplat(worldPosInt, localPosInt, reagents);
					}
				}
			}
		}
	}

	public void PaintBlood(Vector3Int worldPosInt, ReagentMix reagents)
	{
		EffectsFactory.BloodSplat(worldPosInt, reagents);
		BloodDry(worldPosInt);
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
		Get(localPosInt, updateTileOnClient: true).IsSlippery = false;
		var floorDecals = MatrixManager.GetAt<FloorDecal>(worldPosInt, isServer: true);

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
		yield return WaitFor.Seconds(Random.Range(10,21));
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
}
