using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;


/// <summary>
/// Stores the RegisterTiles at each tile position.
///
/// Note that it has a locking mechanism for locking modifications to a particular position's RegisterTiles  -
/// when a modification is performed while locked it actually goes into a queue and
/// isn't applied until unlocked.
/// </summary>
public class TileList
{
	private readonly Dictionary<Vector3Int, List<RegisterTile>> _objects =
		new Dictionary<Vector3Int, List<RegisterTile>>();

	private static readonly List<RegisterTile> emptyList = new List<RegisterTile>();

	//position that is currently locked from modifications, thus will queue up any modifications.
	//note this is not a thread safety mechanism, it is only there to allow modifications to the list
	//withing the ForEachSafe action.
	//Also it's only possible to lock a single position at a time.
	private Vector3Int? lockedPosition = null;
	//queued operations to dequeue once iteration is complete.
	private readonly List<QueuedOp> queuedOps = new List<QueuedOp>();

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


		//queue for later if it's locked
		if (lockedPosition == position)
		{
			queuedOps.Add(new QueuedOp(false, position, obj));
			return;
		}


		if (!_objects.ContainsKey(position))
		{
			_objects[position] = new List<RegisterTile>();
		}



		if (!_objects[position].Contains(obj))
		{
			_objects[position].Add(obj);
			ReorderObjects(position);
		}
	}

	private void ReorderObjects(Vector3Int position)
	{
		int offset = 0;
		if (position.x % 2 != 0)
		{
			offset = position.y % 2 != 0 ? 1 : 0;
		}
		else
		{
			offset = position.y % 2 != 0 ? 3 : 2;
		}
		int i = 0;
		foreach (var register in _objects[position])
		{
			if (register?.CurrentsortingGroup == null) continue;
			register.CurrentsortingGroup.sortingOrder = (i * 4) + offset;
			i++;
		}

	}

	public bool HasObjects(Vector3Int position)
	{
		return _objects.ContainsKey(position) && _objects[position].Count > 0;
	}
	public List<RegisterTile> Get(Vector3Int position)
	{
		return _objects.TryGetValue(position, out var objectsOut) ? objectsOut : emptyList;
	}

	public IEnumerable<RegisterTile> Get(Vector3Int position, ObjectType type)
	{
		if (_objects.TryGetValue(position, out var objectsOut) == false)
		{
			return emptyList;
		}

		var list = new List<RegisterTile>();
		foreach ( RegisterTile x in objectsOut )
		{
			if ( x.ObjectType == type ) {
				list.Add( x );
			}
		}

		return list;
	}

	public IEnumerable<T> Get<T>(Vector3Int position) where T : RegisterTile
	{
		if (_objects.TryGetValue(position, out var objectsOut) == false)
		{
			return Enumerable.Empty<T>();
		}

		var list = new List<T>();
		foreach ( RegisterTile t in objectsOut )
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
		//queue for later if it's locked
		if (position == lockedPosition)
		{
			queuedOps.Add(new QueuedOp(true, position, obj));
			return;
		}

		if (_objects.TryGetValue(position, out var objectsOut))
		{
			if (obj == null)
			{
				objectsOut.Clear();
			}
			else
			{
				objectsOut.Remove(obj);
			}
		}
	}


	/// <summary>
	/// Efficient way of iterating through the register tiles at a particular position which
	/// also is safe against modifications made to the list of tiles while the action is running.
	/// The limitation compared to Get<> is it can only get RegisterTiles, but the benefit is it avoids
	/// GetComponent so there's no GC. The OTHER benefit is that normally iterating through these
	/// would throw an exception if the RegisterTiles at this position were modified, such as
	/// being destroyed are created within the specified action. This method uses a locking mechanism to avoid
	/// such issues - it's safe to add / remove register tiles.
	/// </summary>
	/// <param name="localPosition"></param>
	/// <returns></returns>
	public void ForEachSafe(IRegisterTileAction action, Vector3Int localPosition)
	{
		if (lockedPosition != null)
		{
			Logger.LogErrorFormat("Tried to lock tile at position {0} while position {1} is currently locked." +
			                      " TileList only supports locking one position at a time. Please add this locking capability" +
			                      " to TileList if it is really necessary. Action will be skipped", Category.Matrix, localPosition, lockedPosition);
			return;
		}

		lockedPosition = localPosition;
		foreach (var registerTile in Get((Vector3Int)lockedPosition))
		{
			action.Invoke(registerTile);
		}
		lockedPosition = null;

		foreach (var queuedOp in queuedOps)
		{
			if (queuedOp.Remove)
			{
				Remove(queuedOp.Position, queuedOp.RegisterTile);
			}
			else
			{
				Add(queuedOp.Position, queuedOp.RegisterTile);
			}
		}

		queuedOps.Clear();
	}

	private class QueuedOp
	{
		//if not remove, it's an add
		public readonly bool Remove;
		public readonly Vector3Int Position;
		//to add or remove (or null to clear all)
		public readonly RegisterTile RegisterTile;

		public QueuedOp(bool remove, Vector3Int position, RegisterTile registerTile)
		{
			this.Remove = remove;
			this.Position = position;
			this.RegisterTile = registerTile;
		}
	}
}
