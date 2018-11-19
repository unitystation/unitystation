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

	public void Swap()
	{
		if (PlayerManager.LocalPlayerScript != null)
		{
			if (!PlayerManager.LocalPlayerScript.playerMove.allowInput ||
				PlayerManager.LocalPlayerScript.playerMove.isGhost)
			{
				return;
			}
		}

		SetHand(!IsRight);
	}

	public void SetHand(bool right)
	{
		if (PlayerManager.LocalPlayerScript != null)
		{
			if (!PlayerManager.LocalPlayerScript.playerMove.allowInput ||
				PlayerManager.LocalPlayerScript.playerMove.isGhost)
			{
				return;
			}
		}

		if (right)
		{
			CurrentSlot = Slots["rightHand"];
			OtherSlot = Slots["leftHand"];
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand("rightHand");
			PlayerManager.LocalPlayerScript.playerNetworkActions.activeHand = "rightHand";
			selector.parent = rightHand;
		}
		else
		{
			CurrentSlot = Slots["leftHand"];
			OtherSlot = Slots["rightHand"];
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand("leftHand");
			PlayerManager.LocalPlayerScript.playerNetworkActions.activeHand = "leftHand";
			selector.parent = leftHand;
		}

		IsRight = right;
		var pos = selectorRect.anchoredPosition;
		pos.x = 0f;
		selectorRect.anchoredPosition = pos;
		selector.SetAsFirstSibling();
	}

	public void SwapItem(UI_ItemSlot itemSlot)
	{
		if (PlayerManager.LocalPlayerScript != null)
		{
			if (!PlayerManager.LocalPlayerScript.playerMove.allowInput ||
				PlayerManager.LocalPlayerScript.playerMove.isGhost)
			{
				return;
			}
		}

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

	public void Use()
	{
		if (PlayerManager.LocalPlayerScript != null)
		{
			if (!PlayerManager.LocalPlayerScript.playerMove.allowInput ||
				PlayerManager.LocalPlayerScript.playerMove.isGhost)
			{
				return;
			}
		}

		if (!CurrentSlot.IsFull)
		{
			return;
		}

		//Is the item edible?
		if (CheckEdible())
		{
			return;
		}

		//This checks which UI slot the item can be equiped too and swaps it there
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

	//Check if the item is edible and eat it
	private bool CheckEdible()
	{
		FoodBehaviour baseFood = CurrentSlot.Item.GetComponent<FoodBehaviour>();
		if (baseFood != null)
		{
			baseFood.TryEat();
			return true;
		}
		return false;
	}

	private void Swap(UI_ItemSlot slot1, UI_ItemSlot slot2)
	{
		if (PlayerManager.LocalPlayerScript != null)
		{
			if (!PlayerManager.LocalPlayerScript.playerMove.allowInput ||
				PlayerManager.LocalPlayerScript.playerMove.isGhost)
			{
				return;
			}
		}
		
		UIManager.TryUpdateSlot(new UISlotObject(slot1.inventorySlot.UUID, slot2.Item, slot2.inventorySlot.UUID));
	}
}