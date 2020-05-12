using UnityEngine;
using Mirror;

public class MobSpawnScript : NetworkBehaviour
{
	public GameObject MobToSpawn;

	[SerializeField] private GameObject icon = null;

	private void OnEnable()
	{
		icon.SetActive(false);
	}

	[Server]
	public void SpawnMob()
	{
		if (MobToSpawn == null) return;

		var spawnResult = Spawn.ServerPrefab(MobToSpawn, gameObject.GetComponent<RegisterTile>().WorldPosition, transform.parent);
	}
}
