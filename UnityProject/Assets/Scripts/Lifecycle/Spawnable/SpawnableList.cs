
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pretty straightforward, just a list of things to spawn.
/// Provides capability to spawn them at the indicated destination
/// </summary>
[CreateAssetMenu(fileName = "SpawnableList", menuName = "Spawnable/SpawnableList")]
public class SpawnableList : ScriptableObject, ISpawnable
{
	[Tooltip("Prefabs to spawn.")]
	[SerializeField]
	private List<GameObject> contents;

	/// <summary>
	/// Spawns the things defined in this list at the indicated destination
	/// </summary>
	/// <param name="destination"></param>
	public SpawnableResult SpawnAt(SpawnDestination destination)
	{
		if (!SpawnableUtils.IsValidDestination(destination)) return SpawnableResult.Fail(destination);

		List<GameObject> spawned = new List<GameObject>();
		foreach (var prefab in contents)
		{
			var result = Spawn.ServerPrefab(prefab, destination);
			spawned.AddRange(result.GameObjects);
		}

		return SpawnableResult.Multiple(spawned, destination);
	}
}
