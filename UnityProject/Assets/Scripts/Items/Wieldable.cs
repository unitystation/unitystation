using Items;
using Messages.Server;
using Mirror;
using UnityEngine;

public class Wieldable : NetworkBehaviour, IServerInventoryMove, ICheckedInteractable<HandActivate>
{
	[SerializeField]
	private int damageUnwielded;

	[SerializeField]
	private int damageWielded;

	[SerializeField]
	public ItemsSprites Wielded = new ItemsSprites();
	public ItemsSprites Unwielded = new ItemsSprites();

	[SyncVar(hook = nameof(SyncState))]
	private bool isWielded;

	private SpriteHandler spriteHandler;
	private ItemAttributesV2 itemAttributes;
	private HandsController HandsController;

	private void Awake()
	{
		itemAttributes = GetComponent<ItemAttributesV2>();
		spriteHandler = GetComponentInChildren<SpriteHandler>();
	}

	private void OnEnable()
	{
		HandsController.OnSwapHand.AddListener(OnSwapHands);
	}

	private void OnDisable()
	{
		HandsController.OnSwapHand.RemoveListener(OnSwapHands);
	}

	private void OnSwapHands()
    {
		if (isWielded)
        {
			Chat.AddExamineMsgFromServer(PlayerManager.LocalPlayerScript.gameObject, $"Your other hand is too busy holding {gameObject.ExpensiveName()}!");
			HandsController.OnSwapHand.RemoveListener(OnSwapHands);
			HandsController.SwapHand();
			HandsController.OnSwapHand.AddListener(OnSwapHands);
		}
    }

	private void SyncState(bool oldState, bool newState)
	{
		isWielded = newState;

		if (isWielded)
		{
			itemAttributes.SetSprites(Wielded);
		}
		else
		{
			itemAttributes.SetSprites(Unwielded);
		}
	}

	public void OnInventoryMoveServer(InventoryMove info)
	{
		if (info.InventoryMoveType == InventoryMoveType.Remove)
		{
			isWielded = false;
			ItemSlot hiddenHand = DetermineHiddenHand();

			if (hiddenHand != null)
            {
				int hiddenHandSelection = 0;

				if (hiddenHand.NamedSlot.GetValueOrDefault(NamedSlot.none) == NamedSlot.leftHand)
				{
					hiddenHandSelection = 1;
				}
				else if (hiddenHand.NamedSlot.GetValueOrDefault(NamedSlot.none) == NamedSlot.rightHand)
				{
					hiddenHandSelection = 2;
				}

				HideHand(PlayerManager.LocalPlayerScript.connectionToClient, false, hiddenHandSelection);
			}
		}
	}

	[TargetRpc]
	private void HideHand(NetworkConnection target, bool HideState, int HiddenHandSelection)
	{
		HandsController.Instance.HideHands(HideState, HiddenHandSelection);
	}

	private ItemSlot DetermineHiddenHand()
	{
		ItemSlot hiddenHand = null;
		var playerStorage = PlayerManager.LocalPlayerScript.DynamicItemStorage;
		var currentSlot = playerStorage.GetActiveHandSlot();

		var leftHands = playerStorage.GetNamedItemSlots(NamedSlot.leftHand);
		foreach (var leftHand in leftHands)
		{
			if (leftHand != currentSlot && Validations.HasComponent<Wieldable>(leftHand.ItemObject) == false)
			{
				hiddenHand = leftHand;
			}
		}

		var rightHands = playerStorage.GetNamedItemSlots(NamedSlot.rightHand);
		foreach (var rightHand in rightHands)
		{
			if (rightHand != currentSlot && Validations.HasComponent<Wieldable>(rightHand.ItemObject) == false)
			{
				hiddenHand = rightHand;
			}
		}

		return hiddenHand;
	}

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		return true;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		ItemSlot hiddenHand = DetermineHiddenHand();

		if (hiddenHand != null)
        {
			int hiddenHandSelection = 0;

			if (hiddenHand.NamedSlot.GetValueOrDefault(NamedSlot.none) == NamedSlot.leftHand)
			{
				hiddenHandSelection = 1;
			}
			else if (hiddenHand.NamedSlot.GetValueOrDefault(NamedSlot.none) == NamedSlot.rightHand)
			{
				hiddenHandSelection = 2;
			}

			Inventory.ServerDrop(hiddenHand);

			isWielded = !isWielded;

			if (isWielded)
			{
				itemAttributes.ServerHitDamage = damageWielded;
				itemAttributes.SetSprites(Wielded);
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You wield {gameObject.ExpensiveName()} grabbing it with both of your hands.");
				HideHand(PlayerManager.LocalPlayerScript.connectionToClient, true, hiddenHandSelection);
			}
			else
			{
				itemAttributes.ServerHitDamage = damageUnwielded;
				itemAttributes.SetSprites(Unwielded);
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You unwield {gameObject.ExpensiveName()}.");
				HideHand(PlayerManager.LocalPlayerScript.connectionToClient, false, hiddenHandSelection);
			}

			PlayerAppearanceMessage.SendToAll(interaction.Performer, (int)interaction.HandSlot.NamedSlot.GetValueOrDefault(NamedSlot.none), gameObject);
		}
	}
}