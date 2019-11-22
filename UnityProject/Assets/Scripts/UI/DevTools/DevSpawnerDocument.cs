using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// POCO representing something spawnable, indexed into Lucene to support searching for spawnable items.
/// </summary>
public class DevSpawnerDocument
{
	/// <summary>
	/// Searchable name (if prefab, prefab name without .prefab. If unicloth, cloth name)
	/// </summary>
	public readonly string Name;

	public readonly DevSpawnableType Type;

	private DevSpawnerDocument(string name, DevSpawnableType type)
	{

		Name = name;
		this.Type = type;
	}

	/// <summary>
	/// Create a dev spawner document representing this prefab.
	/// </summary>
	/// <param name="prefab"></param>
	public static DevSpawnerDocument ForPrefab(GameObject prefab)
	{
		return new DevSpawnerDocument(prefab.name, DevSpawnableType.Prefab);
	}


	/// <summary>
	/// Create a dev spawner document representing the specified clothing
	/// </summary>
	/// <param name="data"></param>
	public static DevSpawnerDocument ForClothing(BaseClothData data)
	{
		return new DevSpawnerDocument(data.name, DevSpawnableType.Cloth);
	}
}

public enum DevSpawnableType
{
	Prefab,
	Cloth
}

