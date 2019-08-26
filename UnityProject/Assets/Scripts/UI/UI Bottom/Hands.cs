using UnityEngine;

public class Hands : MonoBehaviour
{
	public Transform selector;
	public RectTransform selectorRect;
	public Transform rightHand;
	public Transform leftHand;
	public UI_ItemSlot CurrentSlot { get; private set; }
	public UI_ItemSlot OtherSlot { get; private set; }
	public bool IsRight { get; private set; }
	public bool hasSwitchedHands;

	private InventorySlotCache Slots => UIManager.InventorySlots;

	private void Start()
	{
		CurrentSlot = Slots[EquipSlot.rightHand];
		OtherSlot = Slots[EquipSlot.leftHand];
		IsRight = true;
		hasSwitchedHands = false;
	}

	/// <summary>
	/// Action to swap hands
	/// </summary>
	public void Swap()
	{
		if (isValidPlayer())
		{
			SetHand(!IsRight);
		}
	}

	/// <summary>
	/// Sets the current active hand (true for right, false for left)
	/// </summary>
	public void SetHand(bool right)
	{
		if (isValidPlayer())
		{
			if (right)
			{
				if (CurrentSlot != Slots[EquipSlot.rightHand])
				{
					hasSwitchedHands = true;
				}
				CurrentSlot = Slots[EquipSlot.rightHand];
				OtherSlot = Slots[EquipSlot.leftHand];
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand(EquipSlot.rightHand);
				PlayerManager.LocalPlayerScript.playerNetworkActions.activeHand = EquipSlot.rightHand;
				selector.SetParent(rightHand, false);
			}
			else
			{
				if (CurrentSlot != Slots[EquipSlot.leftHand])
				{
					hasSwitchedHands = true;
				}
				CurrentSlot = Slots[EquipSlot.leftHand];
				OtherSlot = Slots[EquipSlot.rightHand];
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand(EquipSlot.leftHand);
				PlayerManager.LocalPlayerScript.playerNetworkActions.activeHand = EquipSlot.leftHand;
				selector.SetParent(leftHand, false);
			}

			IsRight = right;
			var pos = selectorRect.anchoredPosition;
			pos.x = 0f;
			selectorRect.anchoredPosition = pos;
			selector.SetAsFirstSibling();
		}
	}

	/// <summary>
	/// Swap the item in the current slot to itemSlot
	/// </summary>
	public bool SwapItem(UI_ItemSlot itemSlot)
	{
		if (isValidPlayer())
		{
			if (CurrentSlot != itemSlot)
			{
				var pna = PlayerManager.LocalPlayerScript.playerNetworkActions;

				if (CurrentSlot.Item == null)
				{
					if(itemSlot.Item != null)
					{
						if(itemSlot.inventorySlot.IsUISlot)
						{
							pna.CmdUpdateSlot(CurrentSlot.equipSlot, itemSlot.equipSlot);
						}
						else
						{
							StoreItemMessage.Send(itemSlot.inventorySlot.Owner, PlayerManager.LocalPlayerScript.gameObject, CurrentSlot.equipSlot, false, itemSlot.equipSlot);
						}
						return true;
					}
				}
				else
				{
					if(itemSlot.Item == null)
					{
						if (itemSlot.inventorySlot.IsUISlot)
						{
							pna.CmdUpdateSlot(itemSlot.equipSlot, CurrentSlot.equipSlot);
						}
						else
						{
							StoreItemMessage.Send(itemSlot.inventorySlot.Owner, PlayerManager.LocalPlayerScript.gameObject, CurrentSlot.equipSlot, true);
						}
						return true;
					}
				}
			}
		}
		return false;
	}

	/// <summary>
	/// General function to activate the item's UIInteract
	/// This is the same as clicking the item with the same item's hand
	/// </summary>
	public void Activate()
	{
		// Is there an item in the active hand?
		if (CurrentSlot.Item == null)
		{
			return;
		}
		CurrentSlot.TryItemInteract();
	}

	/// <summary>
	/// General function to try to equip the item in the active hand
	/// </summary>
	public void Equip()
	{
		// Is the player allowed to interact? (not a ghost)
		if(!isValidPlayer())
		{
			return;
		}

		// Is there an item to equip?
		if(CurrentSlot.Item == null)
		{
			Logger.Log("!CurrentSlot.IsFull");
			return;
		}

		//This checks which UI slot the item can be equiped to and swaps it there
		UI_ItemSlot itemSlot = InventorySlotCache.GetSlotByItemType(CurrentSlot.Item);

		if (itemSlot != null)
		{
			//Try to equip the item into the appropriate slot
			if (!SwapItem(itemSlot))
			{
				//If we couldn't equip the item into it's primary slot, try the pockets!
				if(!SwapItem(InventorySlotCache.GetSlotByEvent(EquipSlot.storage01)))
				{
					//We couldn't equip the item in pocket 1. Try pocket2!
					//This swap fails if both pockets are full, do nothing if fail
					SwapItem(InventorySlotCache.GetSlotByEvent(EquipSlot.storage02));
				}
			}
		}

		else
		{
			Logger.LogError("No slot type was found for this object for auto equip", Category.UI);
		}
	}

	/// <summary>
	/// Check if the player is allowed to interact with objects
	/// </summary>
	/// <returns>True if they can, false if they cannot</returns>
	private bool isValidPlayer()
	{
		if (PlayerManager.LocalPlayerScript != null)
		{
			// TODO tidy up this if statement once it's working correctly
			if (!PlayerManager.LocalPlayerScript.playerMove.allowInput ||
				PlayerManager.LocalPlayerScript.IsGhost)
			{
				Logger.Log("Invalid player, cannot perform action!", Category.UI);
				return false;
			}
		}
		return true;
	}

}
