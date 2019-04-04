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

	private void Awake()
	{
		subsystemManager = GetComponentInParent<SubsystemManager>();
		reactionManager = GetComponentInParent<ReactionManager>();
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

	private IEnumerator DryUp(MetaDataNode tile)
	{
		yield return new WaitForSeconds(15f);
		tile.IsSlippery = false;
	}


	public void UpdateSystemsAt(Vector3Int position)
	{
		subsystemManager.UpdateAt(position);
	}
}