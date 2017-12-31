using System.Collections.Generic;
using System.Linq;
using Tilemaps.Behaviours.Objects;
using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Tiles;
using Tilemaps.Scripts.Utils;
using UnityEngine;

namespace Tilemaps
{
	public class Matrix : MonoBehaviour
	{
		private MetaTileMap metaTileMap;
		private TileList objects;

		private void Start()
		{
			metaTileMap = GetComponent<MetaTileMap>();
			try
			{
				objects = ((ObjectLayer) metaTileMap.Layers[LayerType.Objects]).Objects;
			}
			catch
			{
				Debug.LogError("CAST ERROR: Make sure everything is in its proper layer type.");
			}
		}

		public bool IsPassableAt(Vector3Int origin, Vector3Int position)
		{
			return metaTileMap.IsPassableAt(origin, position);
		}

		public bool IsPassableAt(Vector3Int position)
		{
			return metaTileMap.IsPassableAt(position);
		}

		public bool IsAtmosPassableAt(Vector3Int position)
		{
			return metaTileMap.IsAtmosPassableAt(position);
		}

		public bool IsSpaceAt(Vector3Int position)
		{
			return metaTileMap.IsSpaceAt(position);
		}

		public bool IsEmptyAt(Vector3Int position)
		{
			return metaTileMap.IsEmptyAt(position);
		}

		public bool IsFloatingAt(Vector3Int position)
		{
			BoundsInt bounds = new BoundsInt(position - new Vector3Int(1, 1, 0), new Vector3Int(3, 3, 1));
			foreach (Vector3Int pos in bounds.allPositionsWithin)
			{
				if (!metaTileMap.IsEmptyAt(pos))
				{
					return false;
				}
			}
			return true;
		}

		public IEnumerable<T> Get<T>(Vector3Int position) where T : MonoBehaviour
		{
			return objects.Get(position).Select(x => x.GetComponent<T>()).Where(x => x != null);
		}

		public T GetFirst<T>(Vector3Int position) where T : MonoBehaviour
		{
			return objects.GetFirst(position)?.GetComponent<T>();
		}

		public IEnumerable<T> Get<T>(Vector3Int position, ObjectType type) where T : MonoBehaviour
		{
			return objects.Get(position, type).Select(x => x.GetComponent<T>()).Where(x => x != null);
		}

		public bool ContainsAt(Vector3Int position, GameObject gameObject)
		{
			RegisterTile registerTile = gameObject.GetComponent<RegisterTile>();

			return registerTile && objects.Get(position).Contains(registerTile);
		}
	}
}