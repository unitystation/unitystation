using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Add your custom SerializableDictionary classes here
/// </summary>

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

[Serializable]
public class PassableDictionary : SerializableDictionary<CollisionType, bool>
{
}

[Serializable]
public class UISwapDictionary : SerializableDictionary<UIType, string>
{
}

[Serializable]
public class ItemDictionary : SerializableDictionary<GameObject, int>
{
}

[Serializable]
public class PropertyDictionary : SerializableDictionary<string, bool>
{
}