using System.Collections.Generic;
using UnityEngine;

namespace Tilemaps.Utils
{
	public class TileListChunk<T>
	{
		private readonly Dictionary<Vector3Int, List<T>> objects = new Dictionary<Vector3Int, List<T>>();

		private static readonly List<T> EmptyList = new List<T>();

		public void GetAllObjects(List<T> allObjects)
		{
			foreach (var x in objects.Values)
			{
				foreach (var item in x)
				{
					allObjects.Add(item);
				}
			}
		}

		public void Add(Vector3Int position, T obj)
		{
        	if (objects.ContainsKey(position) == false)
        	{
        		objects[position] = new List<T>();
        	}

        	if (objects[position].Contains(obj) == false)
        	{
                objects[position].Add(obj);
        	}
		}

        public void Remove(Vector3Int position, T obj)
        {
        	if (objects.TryGetValue(position, out var objectsOut))
        	{
	            objectsOut.Remove(obj);

                if (objectsOut.Count == 0)
                {
	                objects.Remove(position);
                }
        	}
        }

        public bool HasObjects(Vector3Int localPosition)
        {
	        return objects.ContainsKey(localPosition) && objects[localPosition].Count > 0;
        }

        public List<T> Get(Vector3Int position)
        {
	        return objects.TryGetValue(position, out var objectsOut) ? objectsOut : EmptyList;
        }
	}
}