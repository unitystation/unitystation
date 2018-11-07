using System.Collections.Generic;
using System.Linq;
using UnityEngine;


	public class TileList
	{
		private readonly Dictionary<Vector3Int, List<RegisterTile>> objects =
			new Dictionary<Vector3Int, List<RegisterTile>>();

		public IEnumerable<RegisterTile> AllObjects => objects.Values.SelectMany(x => x).ToList();

		public void Add(Vector3Int position, RegisterTile obj)
		{
			if (!objects.ContainsKey(position))
			{
				objects[position] = new List<RegisterTile>();
			}

			if (!objects[position].Contains(obj))
			{
				objects[position].Add(obj);
			}
		}

		public List<RegisterTile> Get(Vector3Int position)
		{
			return objects.ContainsKey(position) ? objects[position] : new List<RegisterTile>();
		}

		public IEnumerable<RegisterTile> Get(Vector3Int position, ObjectType type)
		{
			return Get(position).Where(x => x.ObjectType == type).ToList();
		}

		public List<T> Get<T>(Vector3Int position) where T : RegisterTile
		{
			return Get(position).OfType<T>().ToList();
		}

		public RegisterTile GetFirst(Vector3Int position)
		{
			return Get(position).FirstOrDefault();
		}

		public T GetFirst<T>(Vector3Int position) where T : RegisterTile
		{
			return Get(position).OfType<T>().FirstOrDefault();
		}

		public void Remove(Vector3Int position, RegisterTile obj = null)
		{
			if (objects.ContainsKey(position))
			{
				if (obj == null)
				{
					objects[position].Clear();
				}
				else
				{
					objects[position].Remove(obj);
				}
			}
		}
	}
