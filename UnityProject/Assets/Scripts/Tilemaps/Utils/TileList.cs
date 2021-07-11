using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores the RegisterTiles at each tile position.
/// </summary>
public class TileList
{
	private readonly Dictionary<Vector3Int, List<RegisterTile>> _objects = new Dictionary<Vector3Int, List<RegisterTile>>();

	private static readonly List<RegisterTile> emptyList = new List<RegisterTile>();

	//permanent list to avoid the usage of ienumerators
	private readonly List<RegisterTile> TempRegisterTiles = new List<RegisterTile>();

	public List<RegisterTile> AllObjects {
		get {
			var list = new List<RegisterTile>();
			foreach (var x in _objects.Values) {
				foreach (var registerTile in x)
				{
					list.Add(registerTile);
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
			ReorderObjects(position);
		}
	}

	public void Remove(Vector3Int position, RegisterTile obj = null)
	{
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

	private void ReorderObjects(Vector3Int position)
	{
		var offset = 0;
		if (position.x % 2 != 0)
		{
			offset = position.y % 2 != 0 ? 1 : 0;
		}
		else
		{
			offset = position.y % 2 != 0 ? 3 : 2;
		}
		var i = 0;
		foreach (var register in _objects[position])
		{
			if (register.OrNull()?.CurrentsortingGroup == null) continue;
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
		foreach (var x in objectsOut)
		{
			if (x.ObjectType == type) {
				list.Add(x);
			}
		}
		return list;
	}

	public void InvokeOnObjects(IRegisterTileAction action, Vector3Int localPosition)
	{
		TempRegisterTiles.Clear();
		TempRegisterTiles.AddRange(Get(localPosition));

		for (int i = TempRegisterTiles.Count - 1; i >= 0; i--)
		{
			if (TempRegisterTiles[i] != null) //explosions can delete many objects in this tile!
			{
				action.Invoke(TempRegisterTiles[i]);
			}
		}
	}
}