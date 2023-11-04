
using System.Collections.Generic;
using Logs;
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
	private List<GameObject> contents = null;
	public List<GameObject> Contents => contents;

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
			if (!result.Successful)
			{
				Loggy.LogWarningFormat("An item in SpawnableList {0} is missing, please fix prefab reference.", Category.ItemSpawn,
					name);
			}
			else
			{
				spawned.AddRange(result.GameObjects);
			}
		}

		return SpawnableResult.Multiple(spawned, destination);
	}
}
