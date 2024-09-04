using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems.Explosions;
using AddressableReferences;
using Core;
using Objects;
using UnityEngine.Events;
using NaughtyAttributes;
using UI.Systems.Tooltips.HoverTooltips;
using Random = UnityEngine.Random;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

namespace Items.Weapons
{
	/// <summary>
	/// Generic grenade base.
	/// </summary>
	[RequireComponent(typeof(Pickupable))]
	[RequireComponent(typeof(ItemStorage))]
	public class Grenade : NetworkBehaviour, IPredictedInteractable<HandActivate>, ICheckedInteractable<InventoryApply>, IServerDespawn, ITrapComponent, IExaminable, IHoverTooltip

	{
		[Tooltip("Explosion effect prefab, which creates when timer ends")]
		public ExplosionComponent explosionPrefab;

		[TooltipAttribute("If the fuse is precise or has a degree of error equal to fuselength / 4")]
		public bool unstableFuse = false;
		[TooltipAttribute("fuse timer in seconds")]
		public float fuseLength = 3;
		[SerializeField] public float fuseMinimum = 3;
		[SerializeField] public float fuseMaximum = 5;

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

		[SerializeField] private bool destroyGrenade = true;

		[SerializeField] private bool allowReuse = false;

		//this object's registerObject
		private RegisterItem registerItem;
		private UniversalObjectPhysics objectPhysics;

		public UnityEvent OnExpload = new UnityEvent();

		[ReadOnly] public ItemSlot TriggerSlot;

		private void Start()
		{
			registerItem = GetComponent<RegisterItem>();
			objectPhysics = GetComponent<UniversalObjectPhysics>();
			ItemStorage itemStorage = GetComponent<ItemStorage>();
			TriggerSlot = itemStorage.GetIndexedItemSlot(0);

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
			if (TriggerSlot.IsOccupied)
			{
				// Toggle the throw action after activation
				UIManager.Action.Throw();
			}
		}

		public void ServerRollbackClient(HandActivate interaction)
		{
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (timerRunning)
				return;

			if (TriggerSlot.IsEmpty)
			{
				Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} lacks a trigger.");
				return;
			}

			// Toggle the throw action after activation
			if (interaction.Performer == PlayerManager.LocalPlayerObject)
			{
				UIManager.Action.Throw();
			}

			// Start timer
			StartCoroutine(TimeExplode(interaction.Performer));
		}


		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.TargetObject == gameObject && interaction.IsFromHandSlot)
			{
				if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wirecutter) ||
					Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Multitool) ||
					Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Cable))
				{
					return true;
				}
			}
			return false;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wirecutter) && TriggerSlot.IsOccupied)
			{
				Defuse(interaction.Performer);
				return;
			}

			//Only allow defusal if timer is already running
			if (timerRunning)
				return;

			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Multitool))
			{
				AdjustTimer(interaction.Performer);
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Cable) && TriggerSlot.IsEmpty)
			{
				if (interaction.UsedObject.TryGetComponent<Stackable>(out var stackable) && stackable.Amount > 1)
				{
					Inventory.ServerAdd(stackable.ServerRemoveOne(), TriggerSlot);
				} else
				{
					Inventory.ServerTransfer(interaction.FromSlot, TriggerSlot);
				}
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You add a trigger to the {gameObject.ExpensiveName()}.",
					$"{interaction.Performer.ExpensiveName()} adds a trigger to the {gameObject.ExpensiveName()}.");
			}

		}

		private void Defuse(GameObject performer)
		{
			if (Inventory.ServerDrop(TriggerSlot))
			{
				if (timerRunning)
				{
					UpdateTimer(false);
					StopAllCoroutines();
				}
				Chat.AddActionMsgToChat(performer,
					$"You cut the fuse on the {gameObject.ExpensiveName()}",
					$"{performer.ExpensiveName()} cuts the fuse on the {gameObject.ExpensiveName()}.");

			}
		}

		private void AdjustTimer(GameObject performer)
		{
			if (TriggerSlot.IsOccupied)
			{
				var newtimer = MathF.Round(fuseLength + 0.1f, 1);
				fuseLength = newtimer > fuseMaximum ? fuseMinimum : newtimer;
				Chat.AddExamineMsg(performer, $"You adjust the fuse on the {gameObject.ExpensiveName()} to {fuseLength}s");
			}
			else
			{
				Chat.AddExamineMsg(performer, $"The {gameObject.ExpensiveName()} does not contain an adjustable fuse.");
			}
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
			spriteHandler?.SetCatalogueIndexSprite(sprite);
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

			if (allowReuse == false)
			{
				hasExploded = true;
			}

			UpdateTimer(false);

			OnExpload?.Invoke();

			if (isServer && explosionPrefab != null)
			{
				// Get data from grenade before despawning
				var explosionMatrix = registerItem.Matrix;
				var worldPos = objectPhysics.registerTile.WorldPosition;

				if (destroyGrenade)
				{
					// Despawn grenade
					_ = Despawn.ServerSingle(gameObject);
				}

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
				StopCoroutine(AnimateSpriteInHands());
			}

		}

		private string ExamineText()
		{
			return TriggerSlot.IsOccupied ? $"Uses a {TriggerSlot.ItemObject.ExpensiveName()} trigger." : "It lacks a trigger." ;
		}

		public string HoverTip()
		{
			return ExamineText();
		}

		public string CustomTitle()
		{
			return null;
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

		public List<TextColor> InteractionsStrings()
		{
			return null;
		}

		public string Examine(Vector3 pos)
		{
			return ExamineText();
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
