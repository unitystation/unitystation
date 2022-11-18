using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using Chemistry;
using Chemistry.Components;
using Mirror;
using Objects;
using UnityEngine;
using Items.Weapons;
using ScriptableObjects;
using System.Linq;

public class ChemicalGrenade : NetworkBehaviour, IPredictedCheckedInteractable<HandActivate>, IServerDespawn, ITrapComponent,
	ICheckedInteractable<InventoryApply>
{
	private ItemStorage containerStorage;
	private SpriteHandler spriteHandler;
	private Pickupable pickupable;
	private UniversalObjectPhysics objectPhysics;

	private const int UNLOCKED_SPRITE = 0;
	private const int LOCKED_SPRITE = 1;
	private const int ARMED_SPRITE = 2;

	public ReagentContainer ReagentContainer1 =>
		containerStorage.GetIndexedItemSlot(0)?.Item.OrNull()?.GetComponent<ReagentContainer>();

	public ReagentContainer ReagentContainer2 =>
		containerStorage.GetIndexedItemSlot(1)?.Item.OrNull()?.GetComponent<ReagentContainer>();


	private ReagentContainer mixedReagentContainer;

	[field: SyncVar] public bool ScrewedClosed { get; private set; } = false;

	[Header("Chemical properties:"), Space(10)]

	[SerializeField] private Reaction smokeReaction;
	[SerializeField] private Reaction foamReaction;

	public bool IsFullContainers
	{
		get
		{
			return ReagentContainer1 != null && ReagentContainer2 != null;
		}
	}

	[Header("Explosive properties:"), Space(10)]

	[SerializeField, Tooltip("If the fuse is precise or has a degree of error equal to fuselength / 4")]
	private bool unstableFuse = false;

	[SerializeField, Tooltip("Fuse timer in seconds")]
	private float fuseLength = 3;

	[SerializeField] private AddressableAudioSource armbomb = null;

	private bool hasExploded = false;

	private bool timerRunning = false;

	private const int DEBUG_FOAM_AMOUNT = 50;

	[RightClickMethod()]
	public void DoSmartFoam()
	{
		SmokeAndFoamManager.StartFoamAt(objectPhysics.OfficialPosition, new ReagentMix(), DEBUG_FOAM_AMOUNT, true, true);
	}

	[RightClickMethod()]
	public void DoFoam()
	{
		SmokeAndFoamManager.StartFoamAt(objectPhysics.OfficialPosition, new ReagentMix(), DEBUG_FOAM_AMOUNT, true, false);
	}

	private void Start()
	{
		objectPhysics = GetComponent<UniversalObjectPhysics>();
		mixedReagentContainer = GetComponent<ReagentContainer>();
		containerStorage = GetComponent<ItemStorage>();
		pickupable = GetComponent<Pickupable>();

		// Set grenade to unlocked state by default
		UpdateSprite(UNLOCKED_SPRITE);
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		// Set grenade to unlocked state by default
		UpdateSprite(UNLOCKED_SPRITE);
		// Reset grenade timer
		timerRunning = false;
		UpdateTimer(timerRunning);
		hasExploded = false;
	}

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{

		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (ScrewedClosed == false)
		{
			Chat.AddExamineMsg(interaction.Performer, "You need to screw together the grenade first numpty");
			return false;
		}

		return true;
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
				MixReagents();
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

	public void MixReagents()
	{
		if (hasExploded)
		{
			return;
		}

		hasExploded = true;

		if (isServer)
		{
			var worldPos = objectPhysics.registerTile.WorldPosition;

			BlastData blastData = new BlastData();
		
			ReagentContainer1.TransferTo(ReagentContainer1.ReagentMixTotal, mixedReagentContainer, false); //We use false to ensure the reagents do not react before we can obtain our blast data
			ReagentContainer2.TransferTo(ReagentContainer2.ReagentMixTotal, mixedReagentContainer, false);

			if(smokeReaction.IsReactionValid(mixedReagentContainer.CurrentReagentMix))
			{
				blastData.SmokeAmount = smokeReaction.GetReactionAmount(mixedReagentContainer.CurrentReagentMix);
			}

			if (foamReaction.IsReactionValid(mixedReagentContainer.CurrentReagentMix))
			{
				blastData.FoamAmount = foamReaction.GetReactionAmount(mixedReagentContainer.CurrentReagentMix);
			}

			blastData.reagentMix = mixedReagentContainer.CurrentReagentMix;

			ExplosiveBase.ExplosionEvent.Invoke(worldPos, blastData); 

			mixedReagentContainer.OnReagentMixChanged?.Invoke(); //We disabled this during the transfer to obtain blast data, we must now call the reagent updates manually.
			mixedReagentContainer.ReagentsChanged();

			// Explosion here
			// Despawn grenade
			_ = Despawn.ServerSingle(gameObject);
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

	public virtual bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (interaction.TargetSlot.Item.OrNull()?.gameObject != gameObject) return false;
		if ((ScrewedClosed || IsFullContainers) && interaction.UsedObject != null)
		{
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver) == false) return false;
			return true;
		}
		else if (ScrewedClosed == false)
		{
			if (interaction.UsedObject != null)
			{
				if (Validations.HasComponent<ReagentContainer>(interaction.UsedObject) == false) return false;
			}

			return true;
		}

		return false;
	}

	public virtual void ServerPerformInteraction(InventoryApply interaction)
	{
		if ((ScrewedClosed || IsFullContainers) && interaction.UsedObject != null)
		{
			ScrewedClosed = !ScrewedClosed;

			if (ScrewedClosed)
			{
				spriteHandler.ChangeSprite(LOCKED_SPRITE);
				spriteHandler.ChangeSpriteVariant(0);
			}
			else
			{
				spriteHandler.ChangeSprite(UNLOCKED_SPRITE);
				spriteHandler.ChangeSpriteVariant(2);
			}

			var StateText = ScrewedClosed ? "Closed" : "Open";
			Chat.AddActionMsgToChat(interaction, $" you screw the {gameObject.ExpensiveName()} {StateText}",
				$" {interaction.Performer.ExpensiveName()} screws the {gameObject.ExpensiveName()} {StateText}");
		}
		else if (ScrewedClosed == false)
		{
			if (interaction.UsedObject != null)
			{
				if (containerStorage.GetIndexedItemSlot(0).Item == null)
				{
					Inventory.ServerTransfer(interaction.FromSlot, containerStorage.GetIndexedItemSlot(0));
					spriteHandler.ChangeSpriteVariant(1);
					return;
				}

				if (containerStorage.GetIndexedItemSlot(1).Item == null)
				{
					Inventory.ServerTransfer(interaction.FromSlot, containerStorage.GetIndexedItemSlot(1));
					spriteHandler.ChangeSpriteVariant(2);
					return;
				}
			}
			else
			{
				if (containerStorage.GetIndexedItemSlot(1).Item != null)
				{
					Inventory.ServerTransfer(containerStorage.GetIndexedItemSlot(1), interaction.FromSlot);
					spriteHandler.ChangeSpriteVariant(1);
					return;
				}

				if (containerStorage.GetIndexedItemSlot(0).Item != null)
				{
					Inventory.ServerTransfer(containerStorage.GetIndexedItemSlot(0), interaction.FromSlot);
					spriteHandler.ChangeSpriteVariant(0);
					return;
				}
			}
		}
	}

	[ContextMenu("Pull a pin")]
	private void PullPin()
	{
		if (ScrewedClosed == false) return;
		spriteHandler.ChangeSprite(ARMED_SPRITE);
		StartCoroutine(TimeExplode(gameObject));
	}

	public void TriggerTrap()
	{
		if (ScrewedClosed == false) return;
		PullPin();
	}
}