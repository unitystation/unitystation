using System;
using System.Collections.Generic;
using Atmospherics;
using Tilemaps.Behaviours.Meta;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
{
	private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

	[SerializeField] public List<TKey> keys = new List<TKey>();
	[SerializeField] public List<TValue> values = new List<TValue>();

	public Dictionary<TKey, TValue>.KeyCollection Keys => dictionary.Keys;
	public Dictionary<TKey, TValue>.ValueCollection Values => dictionary.Values;

	public void OnBeforeSerialize()
	{
		keys.Clear();
		values.Clear();

		foreach (KeyValuePair<TKey, TValue> pair in dictionary)
		{
			keys.Add(pair.Key);
			values.Add(pair.Value);
		}
	}

	public void OnAfterDeserialize()
	{
		dictionary.Clear();

		for (int i = 0; i < keys.Count; i++)
			dictionary.Add(keys[i], values[i]);
	}

	public void Add(TKey key, TValue value)
	{
		dictionary.Add(key, value);
	}

	public TValue this[TKey key]
	{
		get { return dictionary[key]; }
		set { dictionary[key] = value; }
	}

	public int Count
	{
		get { return dictionary.Count; }
	}

	public bool ContainsKey(TKey key)
	{
		return dictionary.ContainsKey(key);
	}

	public void Remove(TKey key)
	{
		dictionary.Remove(key);
	}

	public void Clear()
	{
		dictionary.Clear();
	}
}

[Serializable]
public class GridDictionary<TValue> : SerializableDictionary<long, TValue>
{
	public TValue this[int x, int y]
	{
		get { return this[CalculateKey(x, y)]; }
		set { this[CalculateKey(x, y)] = value; }
	}

	public bool ContainsKey(int x, int y)
	{
		return ContainsKey(CalculateKey(x, y));
	}

	public void Remove(int x, int y)
	{
		Remove(CalculateKey(x, y));
	}

	private static long CalculateKey(int x, int y)
	{
		return ((long) x << 32) + y;
	}
}

[Serializable]
public class NodeDictionary : GridDictionary<MetaDataNode>
{
}

[Serializable]
public class EventDictionary : GridDictionary<UnityEvent>
{
}

[Serializable]
public class MetaDataDictionary : SerializableDictionary<Vector3Int, MetaDataNode>
{
}