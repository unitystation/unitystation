using System.Collections;
using System.Collections.Generic;
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

	public MetaDataNode Get(Vector3Int position, bool createIfNotExists = true)
	{
		if (!nodes.ContainsKey(position))
		{
			if (createIfNotExists)
			{
				nodes[position] = new MetaDataNode(position, reactionManager);
			}
			else
			{
				return MetaDataNode.None;
			}
		}

		return nodes[position];
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

	public void MakeSlipperyAt(Vector3Int position)
	{
		var tile = Get(position);
		tile.IsSlippery = true;
		if (tile.CurrentDrying != null)
		{
			StopCoroutine(tile.CurrentDrying);
		}
		tile.CurrentDrying = DryUp(tile);
		StartCoroutine(tile.CurrentDrying);
	}


	public void ReagentReact(Dictionary<string, float> reagents, Vector3Int worldPosInt, Vector3Int localPosInt)
	{
		foreach (KeyValuePair<string, float> reagent in reagents)
		{
			if(reagent.Value < 1)
			{
				continue;
			}
			if (reagent.Key == "water")
			{
				matrix.ReactionManager.ExtinguishHotspot(localPosInt);
			} else if (reagent.Key == "space_cleaner")
			{
				Clean(worldPosInt, localPosInt, false);
			}
		}

	}

	public void Clean(Vector3Int worldPosInt, Vector3Int localPosInt, bool makeSlippery)
	{
		var floorDecals = MatrixManager.GetAt<FloorDecal>(worldPosInt, isServer: true);

		for (var i = 0; i < floorDecals.Count; i++)
		{
			floorDecals[i].TryClean();
		}

		if (!MatrixManager.IsSpaceAt(worldPosInt, true) && makeSlippery)
		{
			// Create a WaterSplat Decal (visible slippery tile)
			EffectsFactory.Instance.WaterSplat(worldPosInt);

			// Sets a tile to slippery
			MakeSlipperyAt(localPosInt);
		}
	}

	private IEnumerator DryUp(MetaDataNode tile)
	{
		yield return WaitFor.Seconds(15f);
		tile.IsSlippery = false;
	}


	public void UpdateSystemsAt(Vector3Int position)
	{
		subsystemManager.UpdateAt(position);
	}
}