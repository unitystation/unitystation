using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// POCO representing something spawnable, indexed into Lucene to support searching for spawnable items.
/// </summary>
public class DevSpawnerDocument
{
	// string representing the unicloth type in the indexed documents
	public static readonly string UNICLOTH_TYPE = "UniCloth";
	// string representing the prefab type in the indexed documents
	public static readonly string PREFAB_TYPE = "Prefab";

	/// <summary>
	/// Searchable name (if prefab, prefab name without .prefab. If unicloth, cloth name)
	/// </summary>
	public readonly string Name;
	/// <summary>
	/// If unicloth, hier of the cloth. Otherwise empty string
	/// </summary>
	public readonly string Hier;
	/// <summary>
	/// Type of this spawnable.
	/// </summary>
	public string Type => Hier.Length != 0 ? UNICLOTH_TYPE : PREFAB_TYPE;

	private DevSpawnerDocument(string name, string hier = "")
	{

		Name = name;
		Hier = hier;
	}

	/// <summary>
	/// Create a dev spawner document representing this prefab.
	/// </summary>
	/// <param name="prefab"></param>
	public static DevSpawnerDocument ForPrefab(GameObject prefab)
	{
		return new DevSpawnerDocument(prefab.name);
	}

	/// <summary>
	/// Create a dev spawner document representing this prefab.
	/// </summary>
	/// <param name="prefab"></param>
	public static DevSpawnerDocument ForUniCloth(string hier)
	{
		//lookup display name from attributes
		var attrs = UniItemUtils.GetObjectAttributes(hier);
		attrs.TryGetValue("name", out var name);
		if (name == null)
		{
			string[] nodes = hier.Split('/');
			name = nodes[nodes.Length-1];
		}

		return new DevSpawnerDocument(name, hier);
	}
}
