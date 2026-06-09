using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Lifecycle;
using US13.Health.Objects;
using US13.HealthV2;
using US13.Managers;
using Util;

namespace US13.Items
{
	public class ItemBreakable : MonoBehaviour
	{
		private Integrity integrity;

		public float damageOnHit;

		public int integrityHealth;

		public GameObject brokenItem;

		[SerializeField] private AddressableAudioSource soundOnBreak = null;

		// Start is called before the first frame update
		private void Awake()
		{
			integrity = GetComponent<Integrity>();
			integrity.OnApplyDamage += OnDamageReceived;
		}

		private void OnDestroy()
		{
			if (integrity) integrity.OnApplyDamage -= OnDamageReceived;
		}

		public void AddDamage()
		{
			integrity.ApplyDamage(damageOnHit, AttackType.Melee, DamageType.Brute);
			if (integrity.integrity <= integrityHealth)
			{
				ChangeState();
			}
		}

		private void ChangeState()
		{
			SoundManager.PlayNetworkedAtPos(soundOnBreak, gameObject.AssumedWorldPosServer(), sourceObj: gameObject);
			Spawn.ServerPrefab(brokenItem, gameObject.AssumedWorldPosServer());
			_ = Despawn.ServerSingle(gameObject);
		}

		private void OnDamageReceived(DamageInfo arg0)
		{
			if (integrity.integrity <= integrityHealth)
			{
				ChangeState();
			}
		}
	}
}
