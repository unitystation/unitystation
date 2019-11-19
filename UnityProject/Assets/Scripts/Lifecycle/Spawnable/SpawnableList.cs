
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pretty straightforward, just a list of things to spawn.
/// Provides capability to spawn them at the indicated destination
/// </summary>
[CreateAssetMenu(fileName = "SpawnableList", menuName = "Spawnable/SpawnableList")]
public class SpawnableList : ScriptableObject
{
	[Tooltip("Things to spawn.")]
	[SerializeField]
	private List<Spawnable> spawnables;

	/// <summary>
	/// Spawns the things defined in this list at the indicated destination
	/// </summary>
	/// <param name="destination"></param>
	public void SpawnAt(SpawnDestination destination)
	{
		foreach (var spawnable in spawnables)
		{
			spawnable.SpawnAt(destination);
		}
	}
}
