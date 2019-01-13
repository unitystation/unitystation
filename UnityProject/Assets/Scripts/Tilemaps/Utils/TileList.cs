using System.Collections.Generic;
using System.Linq;
using UnityEngine;


	public class TileList
	{
		private readonly Dictionary<Vector3Int, List<RegisterTile>> objects =
			new Dictionary<Vector3Int, List<RegisterTile>>();

		private static readonly List<RegisterTile> emptyList = new List<RegisterTile>();

		public List<RegisterTile> AllObjects {
			get {
				List<RegisterTile> list = new List<RegisterTile>();
				foreach ( List<RegisterTile> x in objects.Values ) {
					for ( var i = 0; i < x.Count; i++ ) {
						list.Add( x[i] );
					}
				}

				return list;
			}
		}

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
			return objects.ContainsKey(position) ? objects[position] : emptyList;
		}

		public List<RegisterTile> Get(Vector3Int position, ObjectType type) {
			List<RegisterTile> list = new List<RegisterTile>();
			List<RegisterTile> xes = Get( position );
			for ( var i = 0; i < xes.Count; i++ ) {
				RegisterTile x = xes[i];
				if ( x.ObjectType == type ) {
					list.Add( x );
				}
			}

			return list;
		}

		public List<T> Get<T>(Vector3Int position) where T : RegisterTile {
			List<T> list = new List<T>();
			List<RegisterTile> tiles = Get( position );
			for ( var i = 0; i < tiles.Count; i++ ) {
				T unknown = tiles[i] as T;
				if ( unknown != null ) {
					list.Add( unknown );
				}
			}

			return list;
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
