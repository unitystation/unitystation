using System.Collections.Generic;
using System.Linq;
using UnityEngine;


	public class TileList
	{
		private readonly Dictionary<Vector3Int, List<RegisterTile>> _objects =
			new Dictionary<Vector3Int, List<RegisterTile>>();

		private static readonly IEnumerable<RegisterTile> emptyList = new List<RegisterTile>();

		public List<RegisterTile> AllObjects {
			get {
				List<RegisterTile> list = new List<RegisterTile>();
				foreach ( List<RegisterTile> x in _objects.Values ) {
					for ( var i = 0; i < x.Count; i++ ) {
						list.Add( x[i] );
					}
				}

				return list;
			}
		}

		public void Add(Vector3Int position, RegisterTile obj)
		{
			if (!_objects.ContainsKey(position))
			{
				_objects[position] = new List<RegisterTile>();
			}

			if (!_objects[position].Contains(obj))
			{
				_objects[position].Add(obj);
			}
		}

		public bool TryGet(Vector3Int position, out IEnumerable<RegisterTile> list)
		{
			if ( _objects.ContainsKey(position) )
			{
				list = _objects[position];
				return true;
			}

			list = emptyList;
			return false;
		}
		public bool HasObjects(Vector3Int position)
		{
			return _objects.ContainsKey(position);
		}
		public IEnumerable<RegisterTile> Get(Vector3Int position)
		{
			return _objects.ContainsKey(position) ? _objects[position] : emptyList;
		}

		public IEnumerable<RegisterTile> Get(Vector3Int position, ObjectType type) {
			if ( !TryGet(position, out IEnumerable<RegisterTile> xes) )
			{
				return xes;
			}

			var list = new List<RegisterTile>();
			foreach ( RegisterTile x in xes )
			{
				if ( x.ObjectType == type ) {
					list.Add( x );
				}
			}

			return list;
		}

		public IEnumerable<T> Get<T>(Vector3Int position) where T : RegisterTile {
			List<T> list = new List<T>();
			foreach ( RegisterTile t in Get( position ) )
			{
				T unknown = t as T;
				if ( t != null ) {
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
			if (_objects.ContainsKey(position))
			{
				if (obj == null)
				{
					_objects[position].Clear();
				}
				else
				{
					_objects[position].Remove(obj);
				}
			}
		}
	}
