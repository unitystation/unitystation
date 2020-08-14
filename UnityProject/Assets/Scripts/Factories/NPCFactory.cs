using UnityEngine;

/// <summary>
/// Handles spawning of the NPCs
/// </summary>
public class NPCFactory : MonoBehaviourSingleton<NPCFactory>
{
	[SerializeField] private GameObject xenoPrefab = null;

	/// <summary>
	/// Spawns a xenomorph from the server
	/// </summary>
	/// <param name="parent"> Pass the object parent of the matrix to add the npc too</param>
	/// <returns></returns>
	public static GameObject SpawnXenomorph(Vector2 worldPos, Transform parent)
	{
		var npc = Spawn.ServerPrefab(Instance.xenoPrefab, worldPos, parent, Quaternion.identity).GameObject;
		return npc;
	}
}
