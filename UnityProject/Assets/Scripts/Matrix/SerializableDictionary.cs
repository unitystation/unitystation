using Matrix;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class SerializableDictionary<TKey, TValue>: ISerializationCallbackReceiver {

    private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

    [SerializeField]
    public List<TKey> keys = new List<TKey>();

    [SerializeField]
    public List<TValue> values = new List<TValue>();

    public Dictionary<TKey, TValue>.KeyCollection Keys { get { return dictionary.Keys; } }
    public Dictionary<TKey, TValue>.ValueCollection Values { get { return dictionary.Values; } }

    public void OnBeforeSerialize() {
        keys.Clear();
        values.Clear();

        foreach(KeyValuePair<TKey, TValue> pair in dictionary) {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    public void OnAfterDeserialize() {
        dictionary.Clear();

        for(int i = 0; i < keys.Count; i++)
            dictionary.Add(keys[i], values[i]);
    }

    public void Add(TKey key, TValue value) {
        dictionary.Add(key, value);
    }

    public TValue this[TKey key] {
        get { return dictionary[key]; }
        set { dictionary[key] = value; }
    }

    public int Count { get { return dictionary.Count; } }

    public bool ContainsKey(TKey key) { return dictionary.ContainsKey(key); }

    public void Remove(TKey key) { dictionary.Remove(key); }
}

[Serializable]
public class GridDictionary<TValue>: SerializableDictionary<long, TValue> {
    public TValue this[int x, int y] {
        get { return this[calculateKey(x, y)]; }
        set { this[calculateKey(x, y)] = value; }
    }

    private long calculateKey(int x, int y) {
        return ((long) x << 32) + y;
    }

    public bool ContainsKey(int x, int y) { return ContainsKey(calculateKey(x, y)); }

    public void Remove(int x, int y) { Remove(calculateKey(x, y)); }
}

[Serializable]
public class NodeDictionary: GridDictionary<MatrixNode> { }

[Serializable]
public class EventDictionary: GridDictionary<UnityEvent> { }
