using UnityEngine;


	public class MetaDataLayer : MonoBehaviour
	{
		private MetaDataDictionary nodes = new MetaDataDictionary();

		private MetaTileMap metaTileMap;

		private void Awake()
		{
			metaTileMap = GetComponent<MetaTileMap>();
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
	}
