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
		CurrentSlot = Slots["rightHand"];
		OtherSlot = Slots["leftHand"];
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
				if (CurrentSlot != Slots["rightHand"])
				{
					hasSwitchedHands = true;
				}
				CurrentSlot = Slots["rightHand"];
				OtherSlot = Slots["leftHand"];
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand("rightHand");
				PlayerManager.LocalPlayerScript.playerNetworkActions.activeHand = "rightHand";
				selector.SetParent(rightHand, false);
			}
			else
			{
				if (CurrentSlot != Slots["leftHand"])
				{
					hasSwitchedHands = true;
				}
				CurrentSlot = Slots["leftHand"];
				OtherSlot = Slots["rightHand"];
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand("leftHand");
				PlayerManager.LocalPlayerScript.playerNetworkActions.activeHand = "leftHand";
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
				if (!CurrentSlot.IsFull)
				{
					return Swap(CurrentSlot, itemSlot);
				}
				else
				{
					return Swap(itemSlot, CurrentSlot);
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
		if (!CurrentSlot.IsFull)
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
		if(!CurrentSlot.IsFull)
		{
			return;
		}

		//This checks which UI slot the item can be equiped to and swaps it there
		UI_ItemSlot itemSlot = InventorySlotCache.GetSlotByItemType(CurrentSlot.Item);

		if (itemSlot != null)
		{
			// If we couldn't equip item into pocket, let's try the other pocket!
			if (!SwapItem(itemSlot) && itemSlot.eventName == "storage02")
			{
				SwapItem(InventorySlotCache.GetSlotByEvent("storage01"));
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
				Logger.Log("Invalid player, cannot perform action!");
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Swaps the two slots
	/// </summary>
	private bool Swap(UI_ItemSlot slot1, UI_ItemSlot slot2)
	{
		if(isValidPlayer())
		{
			return UIManager.TryUpdateSlot(new UISlotObject(slot1.inventorySlot.UUID, slot2.Item, slot2.inventorySlot.UUID));
		}
		return false;
	}
}