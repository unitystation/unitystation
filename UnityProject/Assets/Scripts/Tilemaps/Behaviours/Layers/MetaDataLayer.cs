using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using Chemistry;
using UnityEngine;

/// <summary>
/// Holds and provides functionality for all the MetaDataTiles for a given matrix.
/// </summary>
public class MetaDataLayer : MonoBehaviour
{
	private MetaDataDictionary nodes = new MetaDataDictionary();

	private SubsystemManager subsystemManager;
	private ReactionManager reactionManager;
	private Matrix matrix;
	private MetaTileMap metaTileMap;

	private void Awake()
	{
		subsystemManager = GetComponentInParent<SubsystemManager>();
		reactionManager = GetComponentInParent<ReactionManager>();
		matrix = GetComponent<Matrix>();
		metaTileMap = GetComponent<MetaTileMap>();
	}

	public MetaDataNode Get(Vector3Int localPosition, bool createIfNotExists = true)
	{
		if (!nodes.ContainsKey(localPosition))
		{
			if (createIfNotExists)
			{
				nodes[localPosition] = new MetaDataNode(localPosition, reactionManager);
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

	public void MakeSlipperyAt(Vector3Int position, bool canDryUp=true)
	{
		var tile = Get(position, false);
		if (tile == MetaDataNode.None || tile.IsSpace)
		{
			return;
		}
		tile.IsSlippery = true;
		if ( canDryUp )
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
		if (MatrixManager.IsTotallyImpassable(worldPosInt, true))
		{
			return;
		}

		bool didSplat = false;

		foreach (var reagent in reagents)
		{
			if(reagent.Value < 1)
			{
				continue;
			}

			switch (reagent.Key.name)
			{
				case "Water":
				{
					matrix.ReactionManager.ExtinguishHotspot(localPosInt);

					foreach (var livingHealthBehaviour in matrix.Get<LivingHealthBehaviour>(localPosInt, true))
					{
						livingHealthBehaviour.Extinguish();
					}

					Clean(worldPosInt, localPosInt, true);
					break;
				}
				case "SpaceCleaner":
					Clean(worldPosInt, localPosInt, false);
					break;
				case "WeldingFuel":
					//temporary: converting spilled fuel to plasma
					Get(localPosInt).GasMix.AddGas(Gas.Plasma, reagent.Value);
					break;
				case "SpaceLube":
				{
					//( ͡° ͜ʖ ͡°)
					if (!Get(localPosInt).IsSlippery)
					{
						EffectsFactory.WaterSplat(worldPosInt);
						MakeSlipperyAt(localPosInt, false);
					}

					break;
				}
				default:
				{
					//for all other things leave a chem splat
					if (!didSplat)
					{
						EffectsFactory.ChemSplat(worldPosInt, reagents.MixColor);
						didSplat = true;
					}

					break;
				}
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
		matrix.TileChangeManager.RemoveOverlay(localPosInt, LayerType.Floors);

		if (!MatrixManager.IsSpaceAt(worldPosInt, true) && makeSlippery)
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
		foreach ( var decal in floorDecals )
		{
			if ( decal.CanDryUp )
			{
				Despawn.ServerSingle(decal.gameObject);
			}
		}
	}


	public void UpdateSystemsAt(Vector3Int localPosition)
	{
		subsystemManager.UpdateAt(localPosition);
	}
}