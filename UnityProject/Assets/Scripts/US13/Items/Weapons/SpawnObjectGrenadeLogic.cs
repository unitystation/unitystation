using NaughtyAttributes;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Managers;
using US13.Managers.NetworkManagement;
using Util;

namespace US13.Items.Weapons
{
	[RequireComponent(typeof(Grenade))]
	public class SpawnObjectGrenadeLogic : MonoBehaviour
	{
		// attach to grenades to spawn things at the detonation point. Crater decals? bananas? whatever you desire.
		[Tooltip("select the object you want to spawn.")]
		[SerializeField] private GameObject prefabToSpawn = null;
		[Tooltip("Number of instances to spawn when the grenade detonates.")]
		[SerializeField] private int spawnQuantity = 1;
		[Tooltip("Snaps the object to the nearest grid point. You need to do this for objects (machines, etc.) or they will look strange. Optional for items.")]
		[SerializeField] private bool snapObjectToGrid = false;
		
		public void OnExpload()
		{
			if (prefabToSpawn == null) return;

			if (CustomNetworkManager.IsServer)
			{
				var spawnPos = gameObject.AssumedWorldPosServer();

				if (snapObjectToGrid)
				{
					spawnPos = (Vector3)Vector3Int.RoundToInt(spawnPos);
				}

				_ = Spawn.ServerPrefab(prefabToSpawn, spawnPos, null, null, spawnQuantity);
			}
		}
	}
}
