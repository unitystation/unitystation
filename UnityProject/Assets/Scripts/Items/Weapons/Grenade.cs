using System.Collections;
using UnityEngine;
using Mirror;
using Systems.Explosions;
using AddressableReferences;
using Objects;
using UnityEngine.Events;

namespace Items.Weapons
{
	/// <summary>
	/// Generic grenade base.
	/// </summary>
	[RequireComponent(typeof(Pickupable))]
	public class Grenade : NetworkBehaviour, IPredictedInteractable<HandActivate>, IServerDespawn, ITrapComponent
	{
		[Tooltip("Explosion effect prefab, which creates when timer ends")]
		public ExplosionComponent explosionPrefab;

		[TooltipAttribute("If the fuse is precise or has a degree of error equal to fuselength / 4")]
		public bool unstableFuse = false;
		[TooltipAttribute("fuse timer in seconds")]
		public float fuseLength = 3;

		[SerializeField] private AddressableAudioSource armbomb = null;

		[Tooltip("SpriteHandler used for blinking animation")]
		public SpriteHandler spriteHandler;
		[Tooltip("Used for inventory animation")]
		public Pickupable pickupable;

		// Zero and one sprites reserved for left and right hands
		private const int LOCKED_SPRITE = 2;
		private const int ARMED_SPRITE = 3;

		//whether this object has exploded
		private bool hasExploded;

		// is timer finished or was interupted?
		private bool timerRunning = false;

		//this object's registerObject
		private RegisterItem registerItem;
		private UniversalObjectPhysics objectPhysics;

		public UnityEvent OnExpload = new UnityEvent();

		private void Start()
		{
			registerItem = GetComponent<RegisterItem>();
			objectPhysics = GetComponent<UniversalObjectPhysics>();

			// Set grenade to locked state by default
			UpdateSprite(LOCKED_SPRITE);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			// Set grenade to locked state by default
			UpdateSprite(LOCKED_SPRITE);
			// Reset grenade timer
			timerRunning = false;
			UpdateTimer(timerRunning);
			hasExploded = false;
		}

		public void ClientPredictInteraction(HandActivate interaction)
		{
			// Toggle the throw action after activation
			UIManager.Action.Throw();
		}

		public void ServerRollbackClient(HandActivate interaction)
		{
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (timerRunning)
				return;

			// Toggle the throw action after activation
			if (interaction.Performer == PlayerManager.LocalPlayerObject)
			{
				UIManager.Action.Throw();
			}

			// Start timer
			StartCoroutine(TimeExplode(interaction.Performer));
		}

		private IEnumerator TimeExplode(GameObject originator)
		{
			if (!timerRunning)
			{
				timerRunning = true;
				UpdateTimer(timerRunning);
				PlayPinSFX(originator.AssumedWorldPosServer());

				if (unstableFuse)
				{
					float fuseVariation = fuseLength / 4;
					fuseLength = Random.Range(fuseLength - fuseVariation, fuseLength + fuseVariation);
				}

				yield return WaitFor.Seconds(fuseLength);

				// Is timer still running?
				if (timerRunning)
					Explode();
			}
		}

		private void UpdateSprite(int sprite)
		{
			// Update sprite in game
			spriteHandler?.ChangeSprite(sprite);
		}

		/// <summary>
		/// This coroutines make sure that sprite in hands is animated
		/// TODO: replace this with more general aproach for animated icons
		/// </summary>
		/// <returns></returns>
		private IEnumerator AnimateSpriteInHands()
		{
			while (timerRunning && !hasExploded)
			{
				pickupable.RefreshUISlotImage();
				yield return null;
			}
		}

		public void Explode()
		{
			if (hasExploded)
			{
				return;
			}

			hasExploded = true;
			OnExpload?.Invoke();

			if (isServer && explosionPrefab != null)
			{
				// Get data from grenade before despawning
				var explosionMatrix = registerItem.Matrix;
				var worldPos = objectPhysics.registerTile.WorldPosition;

				// Despawn grenade
				_ = Despawn.ServerSingle(gameObject);

				// Explosion here
				var explosionGO = Instantiate(explosionPrefab, explosionMatrix.transform);
				explosionGO.transform.position = worldPos;
				explosionGO.Explode();
			}
		}

		private void PlayPinSFX(Vector3 position)
		{
			_ = SoundManager.PlayNetworkedAtPosAsync(armbomb, position);
		}

		private void UpdateTimer(bool timerRunning)
		{
			this.timerRunning = timerRunning;

			if (timerRunning)
			{
				// Start playing arm animation
				UpdateSprite(ARMED_SPRITE);
				// Update grenade icon in hands
				StartCoroutine(AnimateSpriteInHands());
			}
			else
			{
				// We somehow deactivated bomb
				UpdateSprite(LOCKED_SPRITE);
			}

		}

		[ContextMenu("Pull a pin")]
		private void PullPin()
		{
			StartCoroutine(TimeExplode(gameObject));
		}

		public void TriggerTrap()
		{
			PullPin();
		}
	}
}
