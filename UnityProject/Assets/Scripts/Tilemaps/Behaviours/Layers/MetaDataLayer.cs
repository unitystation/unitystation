using System.Collections;
using System.Collections.Generic;
using Systems.Atmospherics;
using Chemistry;
using Chemistry.Components;
using HealthV2;
using UnityEngine;
using Objects.Construction;

/// <summary>
/// Holds and provides functionality for all the MetaDataTiles for a given matrix.
/// </summary>
public class MetaDataLayer : MonoBehaviour
{
	private SerializableDictionary<Vector3Int, MetaDataNode> nodes = new SerializableDictionary<Vector3Int, MetaDataNode>();

	private SubsystemManager subsystemManager;
	private ReactionManager reactionManager;
	private Matrix matrix;

	private void Awake()
	{
		subsystemManager = GetComponentInParent<SubsystemManager>();
		reactionManager = GetComponentInParent<ReactionManager>();
		matrix = GetComponent<Matrix>();
	}

	private void OnDestroy()
	{
		//In the case of the matrix remaining in memory after the round ends, this will ensure the MetaDataNodes are GC
		nodes.Clear();
	}

	public MetaDataNode Get(Vector3Int localPosition, bool createIfNotExists = true)
	{
		localPosition.z = 0; //Z Positions are always on 0

		if (nodes.ContainsKey(localPosition) == false)
		{
			if (createIfNotExists)
			{
				nodes[localPosition] = new MetaDataNode(localPosition, reactionManager, matrix);
			}
			else
			{
				return MetaDataNode.None;
			}
		}

		return nodes[localPosition];
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
		var tile = Get(position, false);
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
	/// Release reagents at provided coordinates, making them react with world
	/// </summary>
	public void ReagentReact(ReagentMix reagents, Vector3Int worldPosInt, Vector3Int localPosInt)
	{
		if (MatrixManager.IsTotallyImpassable(worldPosInt, true)) return;

		bool didSplat = false;

		//Find all reagents on this tile (including current reagent)
		var reagentContainersList = MatrixManager.GetAt<ReagentContainer>(worldPosInt, true);

		//If there is more than one Reagent Container, loop through them
		foreach (ReagentContainer chem in reagentContainersList)
		{
			//If the reagent tile is a pool/puddle/splat
			if (chem.ExamineAmount == ReagentContainer.ExamineAmountMode.UNKNOWN_AMOUNT)
			{
				reagents.Add(chem.CurrentReagentMix);
			}
			//TODO: could allow you to add this to other container types like beakers but would need some balance and perhaps knocking over the beaker
		}


		if(reagents.reagents != null && reagents.Total > 1)
		{
			lock (reagents.reagents)
			{
				foreach (var reagent in reagents.reagents.m_dict)
				{
					if (reagent.Value < 1)
					{
						continue;
					}

					switch (reagent.Key.name)
					{
						case "Water":
						{
							matrix.ReactionManager.ExtinguishHotspot(localPosInt);
							foreach (var livingHealthBehaviour in matrix.Get<LivingHealthMasterBase>(localPosInt, true))
							{
								livingHealthBehaviour.Extinguish();
							}
							Paintsplat(worldPosInt, localPosInt, reagents.MixColor, reagents);
							break;
						}
						case "SpaceCleaner":
							Clean(worldPosInt, localPosInt, false);
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
						{
							if (didSplat == false)
							{
								Clean(worldPosInt, localPosInt, false);
								Paintsplat(worldPosInt, localPosInt, reagents.MixColor, reagents);
							}
							didSplat = true;
							break;
						}
					}
				}
			}
		}
	}
	public void Paintsplat(Vector3Int worldPosInt, Vector3Int localPosInt, Color splatColor, ReagentMix reagents)
	{
		switch (ChemistryUtils.GetMixStateDescription(reagents))
		{
			case "powder":
			{
				EffectsFactory.PowderSplat(worldPosInt, splatColor, reagents);
				break;
			}
			case "liquid":
			{
				MakeSlipperyAt(localPosInt);
				EffectsFactory.ChemSplat(worldPosInt, splatColor, reagents);
				break;
			}
			default:
			{
				EffectsFactory.ChemSplat(worldPosInt, splatColor, reagents);
				break;
			}
		}
	}
	public void Clean(Vector3Int worldPosInt, Vector3Int localPosInt, bool makeSlippery)
	{
		Get(localPosInt).IsSlippery = false;

		var floorDecals = MatrixManager.GetAt<FloorDecal>(worldPosInt, isServer: true);

		for (var i = 0; i < floorDecals.Count; i++)
		{
			floorDecals[i].TryClean();
		}

		//check for any moppable overlays
		matrix.TileChangeManager.RemoveFloorWallOverlaysOfType(localPosInt, TileChangeManager.OverlayType.Cleanable);

		if (MatrixManager.IsSpaceAt(worldPosInt, true) == false && makeSlippery)
		{
			// Create a WaterSplat Decal (visible slippery tile)
			EffectsFactory.WaterSplat(worldPosInt);

			// Sets a tile to slippery
			MakeSlipperyAt(localPosInt);
		}
	}

	private IEnumerator DryUp(MetaDataNode tile)
	{
		yield return WaitFor.Seconds(Random.Range(10,21));
		tile.IsSlippery = false;

		var floorDecals = matrix.Get<FloorDecal>(tile.Position, isServer: true);
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
