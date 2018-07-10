using System.Security.Permissions;
using Tilemaps.Behaviours.Meta;
using Tilemaps.Utils;
using UnityEngine;

namespace Tilemaps.Behaviours.Layers
{
	public class MetaDataLayer : MonoBehaviour
	{
		private MetaDataDictionary nodes = new MetaDataDictionary();

		public MetaDataNode Get(Vector3Int position, bool createIfNotExists=true)
		{
			if (!nodes.ContainsKey(position))
			{
				if (createIfNotExists)
				{
					nodes[position] = new MetaDataNode();
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
			MetaDataNode node = Get(position, false);

			return node.IsSpace;
		}

		public bool IsRoomAt(Vector3Int position)
		{
			MetaDataNode node = Get(position, false);

			return node.IsRoom;
		}
	}
}