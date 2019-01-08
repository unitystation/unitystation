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

	private InventorySlotCache Slots => UIManager.InventorySlots;

	private void Start()
	{
		CurrentSlot = Slots["rightHand"];
		OtherSlot = Slots["leftHand"];
		IsRight = true;
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
				CurrentSlot = Slots["rightHand"];
				OtherSlot = Slots["leftHand"];
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand("rightHand");
				PlayerManager.LocalPlayerScript.playerNetworkActions.activeHand = "rightHand";
				selector.SetParent(rightHand, false);

			}
			else
			{
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
	public void SwapItem(UI_ItemSlot itemSlot)
	{
		if (isValidPlayer())
		{
			if (CurrentSlot != itemSlot)
			{
				if (!CurrentSlot.IsFull)
				{
					Swap(CurrentSlot, itemSlot);
				}
				else
				{
					Swap(itemSlot, CurrentSlot);
				}
			}
		}
	}

	/// <summary>
	/// General function to activate the item depending on the object.
	/// E.g. eat food, clean floor with mop etc
	/// </summary>
	public void Activate()
	{
		// Is there an item in the active hand?
		if (!CurrentSlot.IsFull)
		{
			return;
		}

		//Is the item edible?
		if (isEdible())
		{
			return;
		}

		// Is the item a weapon?
		if (isWeapon())
		{
			return;
		}
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
		ItemType type = Slots.GetItemType(CurrentSlot.Item);
		SpriteType masterType = Slots.GetItemMasterType(CurrentSlot.Item);
		UI_ItemSlot itemSlot = InventorySlotCache.GetSlotByItemType(CurrentSlot.Item);

		if (itemSlot != null)
		{
			SwapItem(itemSlot);
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
				PlayerManager.LocalPlayerScript.playerMove.isGhost)
			{
				Logger.Log("Invalid player, cannot perform action!");
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Check if the item is edible and eat it (true if it is)
	/// </summary>
	private bool isEdible()
	{
		FoodBehaviour baseFood = CurrentSlot.Item.GetComponent<FoodBehaviour>();
		if (baseFood != null)
		{
			baseFood.TryEat();
			return true;
		}
		return false;
	}

	private bool isWeapon()
	{
		Weapon baseWeapon = CurrentSlot.Item.GetComponent<Weapon>();
		if (baseWeapon != null)
		{
			baseWeapon.TryReload();
			return true;
		}
		return false;
	}

	/// <summary>
	/// Swaps the two slots
	/// </summary>
	private void Swap(UI_ItemSlot slot1, UI_ItemSlot slot2)
	{
		if(isValidPlayer())
		{
			UIManager.TryUpdateSlot(new UISlotObject(slot1.inventorySlot.UUID, slot2.Item, slot2.inventorySlot.UUID));
		}
	}
}