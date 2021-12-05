using System.Collections;
using System.Collections.Generic;
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

	private void Awake()
	{
		itemAttributes = GetComponent<ItemAttributesV2>();
		spriteHandler = GetComponentInChildren<SpriteHandler>();
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
			int hiddenHand = DetermineHiddenHand();
			TargetPlayerUIHideHand(PlayerManager.LocalPlayerScript.connectionToClient, false, hiddenHand);
		}
	}

	[TargetRpc]
	private void TargetPlayerUIHideHand(NetworkConnection target, bool HideState, int HandSelection)
	{
		HandsController.Instance.HideHands(HideState, HandSelection);
	}

	private int DetermineHiddenHand()
	{
		int hiddenHand = 0;
		var playerStorage = PlayerManager.LocalPlayerScript.DynamicItemStorage;
		var currentSlot = playerStorage.GetActiveHandSlot();

		var leftHands = playerStorage.GetNamedItemSlots(NamedSlot.leftHand);
		foreach (var leftHand in leftHands)
		{
			if (leftHand != currentSlot && leftHand.ItemObject == null)
			{
				hiddenHand = 1;
			}
		}

		var rightHands = playerStorage.GetNamedItemSlots(NamedSlot.rightHand);
		foreach (var rightHand in rightHands)
		{
			if (rightHand != currentSlot && rightHand.ItemObject == null)
			{
				hiddenHand = 2;
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
		int hiddenHand = DetermineHiddenHand();

		isWielded = !isWielded;

		if (isWielded)
		{
			itemAttributes.ServerHitDamage = damageWielded;
			itemAttributes.SetSprites(Wielded);
			Chat.AddExamineMsgFromServer(interaction.Performer, $"You wield {gameObject.ExpensiveName()} grabbing it with both of your hands.");
			TargetPlayerUIHideHand(PlayerManager.LocalPlayerScript.connectionToClient, true, hiddenHand);
		}
		else
		{
			itemAttributes.ServerHitDamage = damageUnwielded;
			itemAttributes.SetSprites(Unwielded);
			Chat.AddExamineMsgFromServer(interaction.Performer, $"You unwield {gameObject.ExpensiveName()}.");
			TargetPlayerUIHideHand(PlayerManager.LocalPlayerScript.connectionToClient, false, hiddenHand);
		}

		PlayerAppearanceMessage.SendToAll(interaction.Performer, (int)interaction.HandSlot.NamedSlot.GetValueOrDefault(NamedSlot.none), gameObject);
	}
}
