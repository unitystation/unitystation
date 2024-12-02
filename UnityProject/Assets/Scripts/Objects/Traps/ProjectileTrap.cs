using UnityEngine;
using System.Collections;
using Shared.Systems.ObjectConnection;
using Weapons.Projectiles;
using AddressableReferences;
using Mirror;

namespace Objects.Traps
{
	public class ProjectileTrap : MonoBehaviour, IGenericTrigger, IMultitoolLinkable
	{
		private const int FIRING_SPRITE_INDEX = 1;
		private const int IDLE_SPRITE_INDEX = 0;

		private const float FIRING_DURATION_LENGTH = 0.4f;

		[SerializeField]
		private SpriteHandler spriteHandler = null;

		[SerializeField]
		private GameObject projectileToFire = null;

		[SerializeField]
		private AddressableAudioSource firingSound = null;

		private Rotatable rotatable;
		private RegisterObject registerObject;

		private bool active = false;

		[field: SerializeField] public bool CanRelink { get; set; } = true;
		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.GenericTrigger;

		[field: SerializeField] public TriggerType TriggerType { get; protected set; }

		public void Awake()
		{
			rotatable = GetComponent<Rotatable>();
			registerObject = GetComponent<RegisterObject>();
		}

		public void OnTrigger()
		{
			if (TriggerType == TriggerType.Toggle) ToggleState();
			else if (active == false)
			{
				StopAllCoroutines();
				StartCoroutine(FireProjectile());
			}
		}

		private void ToggleState()
		{
			if (active == true) active = false;
			else
			{
				StopAllCoroutines();
				StartCoroutine(FireProjectile());
			}
		}

		public void OnTriggerEnd()
		{
			if (TriggerType != TriggerType.Active) return;
			active = false;
		}

		private IEnumerator FireProjectile()
		{
			active = true;

			if (CustomNetworkManager.IsServer == false) yield break;

			spriteHandler.SetCatalogueIndexSprite(FIRING_SPRITE_INDEX);
			SoundManager.PlayNetworkedAtPos(firingSound, registerObject.WorldPosition, sourceObj: gameObject);
			ProjectileManager.InstantiateAndShoot(projectileToFire, rotatable.CurrentDirection.ToLocalVector2Int(), gameObject, null, BodyPartType.Chest);

			yield return new WaitForSeconds(FIRING_DURATION_LENGTH);
			spriteHandler.SetCatalogueIndexSprite(IDLE_SPRITE_INDEX);
		}

	}
}
