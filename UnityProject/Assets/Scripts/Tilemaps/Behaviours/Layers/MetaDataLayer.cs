using System.Collections.Generic;
using UnityEngine;


public class MetaDataLayer : MonoBehaviour
{
	private MetaDataDictionary nodes = new MetaDataDictionary();

	private SubsystemManager subsystemManager;

	private void Awake()
	{
		subsystemManager = GetComponentInParent<SubsystemManager>();
	}

	public MetaDataNode Get(Vector3Int position, bool createIfNotExists = true)
	{
		if (!nodes.ContainsKey(position))
		{
			if (createIfNotExists)
			{
				nodes[position] = new MetaDataNode(position);
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

	public void UpdateSystemsAt(Vector3Int position)
	{
		subsystemManager.UpdateAt(position);
	}
}