using System.Collections.Generic;
using UnityEngine;

namespace Systems.MobAIs
{
	/// <summary>
	/// Despawns mobs upon death.
	/// Also drops an item at the same time.
	/// </summary>

	public class MobDrop : MonoBehaviour
	{
		private LivingHealthBehaviour mobHealth;

		[SerializeField, Tooltip("Makes the mob despawn upon death if true.")]	
		private bool despawnBody = true;

		[SerializeField, Tooltip("Items dropped upon death.")]
		private List<GameObject> lootDrop = null;

		
		private void Awake()
		{
			mobHealth = GetComponent<LivingHealthBehaviour>();
			mobHealth.OnDeathNotifyEvent += OnMobDeath;
		}

		private void OnMobDeath()
		{
				foreach (var drop in lootDrop)
				{
					Spawn.ServerPrefab(drop, gameObject.RegisterTile().WorldPosition);
				}
				if (despawnBody)
				{
					Despawn.ServerSingle(gameObject);
				}
		}
	}
}