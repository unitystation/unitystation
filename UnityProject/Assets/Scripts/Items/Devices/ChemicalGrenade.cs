using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using Chemistry;
using Chemistry.Components;
using Mirror;
using Objects;
using UnityEngine;

public class ChemicalGrenade : NetworkBehaviour, IPredictedCheckedInteractable<HandActivate>, IServerDespawn, ITrapComponent,
	ICheckedInteractable<InventoryApply>
{
	public ItemStorage ContainersStorage;

	public ReagentContainer ReagentContainer1 =>
		ContainersStorage.GetIndexedItemSlot(0)?.Item.OrNull()?.GetComponent<ReagentContainer>();

	public ReagentContainer ReagentContainer2 =>
		ContainersStorage.GetIndexedItemSlot(1)?.Item.OrNull()?.GetComponent<ReagentContainer>();


	public ReagentContainer MixedReagentContainer;

	[SyncVar] public bool IsFullContainers = false;

	[SyncVar] public bool ScrewedClosed = false;

	[RightClickMethod()]
	public void DoSmartFoam()
	{
		SmokeAndFoamManager.StartFoamAt(transform.position, new ReagentMix(), 50, true, true);
	}

	[RightClickMethod()]
	public void DoFoam()
	{
		SmokeAndFoamManager.StartFoamAt(transform.position, new ReagentMix(), 50, true, false);
	}


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

	private void Start()
	{
		registerItem = GetComponent<RegisterItem>();
		objectPhysics = GetComponent<UniversalObjectPhysics>();
		MixedReagentContainer = GetComponent<ReagentContainer>();
		ContainersStorage = GetComponent<ItemStorage>();
		pickupable = GetComponent<Pickupable>();
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
			ReagentContainer1.TransferTo(ReagentContainer1.ReagentMixTotal, MixedReagentContainer);
			ReagentContainer2.TransferTo(ReagentContainer2.ReagentMixTotal, MixedReagentContainer);

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
			var StateText = ScrewedClosed ? "Closed" : "Open";
			Chat.AddActionMsgToChat(interaction, $" you screw the {gameObject.ExpensiveName()} {StateText}",
				$" {interaction.Performer.ExpensiveName()} screws the {gameObject.ExpensiveName()} {StateText}");
		}
		else if (ScrewedClosed == false)
		{
			if (interaction.UsedObject != null)
			{
				if (ContainersStorage.GetIndexedItemSlot(0).Item == null)
				{
					Inventory.ServerTransfer(interaction.FromSlot, ContainersStorage.GetIndexedItemSlot(0));
					return;
				}

				if (ContainersStorage.GetIndexedItemSlot(1).Item == null)
				{
					Inventory.ServerTransfer(interaction.FromSlot, ContainersStorage.GetIndexedItemSlot(1));
					IsFullContainers = true;
					return;
				}
			}
			else
			{
				if (ContainersStorage.GetIndexedItemSlot(1).Item != null)
				{
					Inventory.ServerTransfer(ContainersStorage.GetIndexedItemSlot(1), interaction.FromSlot);
					IsFullContainers = false;
					return;
				}

				if (ContainersStorage.GetIndexedItemSlot(0).Item != null)
				{
					Inventory.ServerTransfer(ContainersStorage.GetIndexedItemSlot(0), interaction.FromSlot);
					return;
				}
			}
		}
	}

	[ContextMenu("Pull a pin")]
	private void PullPin()
	{
		if (ScrewedClosed == false) return;
		StartCoroutine(TimeExplode(gameObject));
	}

	public void TriggerTrap()
	{
		if (ScrewedClosed == false) return;
		PullPin();
	}
}