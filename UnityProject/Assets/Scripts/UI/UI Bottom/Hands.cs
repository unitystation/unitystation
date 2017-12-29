using PlayGroup;
using UnityEngine;

namespace UI
{
	public class Hands : MonoBehaviour
	{
		public Transform selector;
		public UI_ItemSlot CurrentSlot { get; private set; }
		public UI_ItemSlot OtherSlot { get; private set; }
		public bool IsRight { get; private set; }

		private InventorySlotCache Slots => UIManager.InventorySlots;

		private void Start()
		{
			CurrentSlot = Slots.RightHandSlot;
			OtherSlot = Slots.LeftHandSlot;
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
				CurrentSlot = Slots.RightHandSlot;
				OtherSlot = Slots.LeftHandSlot;
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand("right");
			}
			else
			{
				CurrentSlot = Slots.LeftHandSlot;
				OtherSlot = Slots.RightHandSlot;
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand("left");
			}

			IsRight = right;
			selector.position = CurrentSlot.transform.position;
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

			switch (masterType)
			{
				case SpriteType.Clothing:
					UI_ItemSlot slot = Slots.GetSlotByItem(CurrentSlot.Item);
					SwapItem(slot);
					break;
				case SpriteType.Items:
					UI_ItemSlot itemSlot = Slots.GetSlotByItem(CurrentSlot.Item);
					SwapItem(itemSlot);
					break;
				case SpriteType.Guns:
					break;
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

			UIManager.TryUpdateSlot(new UISlotObject(slot1.eventName, slot2.Item));
		}
	}
}