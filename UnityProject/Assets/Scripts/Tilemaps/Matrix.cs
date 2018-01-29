using System.Collections.Generic;
using System.Linq;
using Tilemaps.Behaviours.Objects;
using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Tiles;
using Tilemaps.Scripts.Utils;
using UnityEngine;
using System;

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
			if(origin.z != position.z)
			{
				//Uhhhhhh, error handling goes here?
				return false;
			}
			//Check if it's a diagonal move
			Vector3Int diff = position - origin;
			int diffSum = diff.x + diff.y;
			//Somewhat hacky way to get if it's diagonal, but it works
			if(Math.Abs(diffSum) != 1)
			{
				//This is a DIAGONAL MOVEMENT!  Now we have to do four checks.
				//This is confusing, basically there are two ways to travel diagonally so we have to check both of them
				bool passable = true;
				passable &= metaTileMap.IsPassableAt(origin, new Vector3Int(origin.x + diff.x, origin.y, origin.z));
				passable &= metaTileMap.IsPassableAt(new Vector3Int(origin.x + diff.x, origin.y, origin.z), 
					new Vector3Int(origin.x + diff.x, origin.y + diff.y, origin.z));
				if(passable)
				{
					return passable;
				}
				passable = true;
				passable &= metaTileMap.IsPassableAt(origin, new Vector3Int(origin.x, origin.y + diff.y, origin.z));
				passable &= metaTileMap.IsPassableAt(new Vector3Int(origin.x, origin.y + diff.y, origin.z),
					new Vector3Int(origin.x + diff.x, origin.y + diff.y, origin.z));
				return passable;
			}
			else
			{
				return metaTileMap.IsPassableAt(origin, position);
			}
		}
		//TODO:  This should be removed, due to windows mucking things up, and replaced with origin and position
		public bool IsPassableAt(Vector3Int position)
		{
			return metaTileMap.IsPassableAt(position);
		}
		//TODO:  This should also be removed, due to windows mucking things up, and replaced with origin and position
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