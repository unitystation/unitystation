using System.Security.Permissions;
using Tilemaps.Behaviours.Meta;
using Tilemaps.Utils;
using UnityEngine;

namespace Tilemaps.Behaviours.Layers
{
	public class MetaDataLayer : MonoBehaviour
	{
		private MetaDataDictionary nodes = new MetaDataDictionary();

		private void Awake()
		{
			foreach (MetaDataNode metaDataNode in nodes.Values)
			{
				metaDataNode.Reset();
			}
		}

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
					return null;
				}
			}

			return nodes[position];
		}

		public bool IsSpaceAt(Vector3Int position)
		{
			var node = Get(position, false);

			return node == null || node.IsSpace();
		}
	}
}