using UnityEngine;
using US13.Core.Lifecycle;

namespace US13.Mapping
{
	public class PrefabSpawner : MonoBehaviour, IServerSpawn
	{
		public GameObject Prefab;


		public void OnSpawnServer(SpawnInfo info)
		{
			_ = Spawn.ServerPrefab(Prefab, transform.position).GameObject;
		}

	}
}
