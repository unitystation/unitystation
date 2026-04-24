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
		// attach to grenades to spawn things at the explosion point. Crater decals? bananas? whatever you desire.
		[Tooltip("select the object you want to spawn.")]
		[SerializeField] private GameObject prefabToSpawn = null;
		[Tooltip("Number of things to spawn.")]
		[SerializeField] private int spawnQuantity = 1;
		[Tooltip("Snaps the object to the nearest grid point. You need to do this for objects (machines, etc.) or they will look strange. Optional for items.")]
		[SerializeField] private bool snapObjectToGrid = false;
		[Tooltip("scatters the objects spawned by the grenade. Negative default (-0.5) uses the tile-aligned scatter behavior like RandomItemSpot's fanOut.")]
		[HideIf("snapObjectToGrid")]
		[SerializeField] private float spawnScatterRadius = -0.5f;

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

				// If the object is being snapped to the grid, this passes null so Spawn.ServerPrefab won't scatter the objects.
				float? scatter = snapObjectToGrid ? null : (float?)spawnScatterRadius;
				_ = Spawn.ServerPrefab(prefabToSpawn, spawnPos, null, null, spawnQuantity, scatter);
			}
		}
	}
}
