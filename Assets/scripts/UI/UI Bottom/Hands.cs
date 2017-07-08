﻿using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UI
{
	public class Hands: MonoBehaviour
	{
		public UI_ItemSlot CurrentSlot { get; private set; }
		public UI_ItemSlot OtherSlot { get; private set; }
		public bool IsRight { get; private set; }
		public Transform selector;

		void Start()
		{
			CurrentSlot = Slots.RightHandSlot;
			OtherSlot = Slots.LeftHandSlot;
			IsRight = true;
		}

		public void Swap()
		{
            if (PlayerManager.LocalPlayerScript != null)
                if (!PlayerManager.LocalPlayerScript.playerMove.allowInput || PlayerManager.LocalPlayerScript.playerMove.isGhost)
                    return;

            SetHand(!IsRight);
		}

		public void SetHand(bool right)
		{
            if (PlayerManager.LocalPlayerScript != null)
                if (!PlayerManager.LocalPlayerScript.playerMove.allowInput || PlayerManager.LocalPlayerScript.playerMove.isGhost)
                    return;

            if (right) {
				CurrentSlot = Slots.RightHandSlot;
				OtherSlot = Slots.LeftHandSlot;
			} else {
				CurrentSlot = Slots.LeftHandSlot;
				OtherSlot = Slots.RightHandSlot;
			}

			IsRight = right;
			selector.position = CurrentSlot.transform.position;
		}

		public void SwapItem(UI_ItemSlot itemSlot)
		{
            if (PlayerManager.LocalPlayerScript != null)
                if (!PlayerManager.LocalPlayerScript.playerMove.allowInput || PlayerManager.LocalPlayerScript.playerMove.isGhost)
                    return;

            if (CurrentSlot != itemSlot) {
				if (!CurrentSlot.IsFull) {
					Swap(CurrentSlot, itemSlot);
				} else {
					Swap(itemSlot, CurrentSlot);
				}
			}
		}

		public void Use()
		{
            if (PlayerManager.LocalPlayerScript != null)
                if (!PlayerManager.LocalPlayerScript.playerMove.allowInput || PlayerManager.LocalPlayerScript.playerMove.isGhost)
                    return;

            if (!CurrentSlot.IsFull)
				return;

			var type = Slots.GetItemType(CurrentSlot.Item);
			var masterType = Slots.GetItemMasterType(CurrentSlot.Item);

			switch (masterType)
			{
				case SpriteType.Clothing:
					var slot = Slots.GetSlotByItem(CurrentSlot.Item);
					SwapItem(slot);
					break;
				case SpriteType.Items:	
				case SpriteType.Guns:	
					break;
			}
			
		}

		private void Swap(UI_ItemSlot slot1, UI_ItemSlot slot2)
		{
            if (PlayerManager.LocalPlayerScript != null)
                if (!PlayerManager.LocalPlayerScript.playerMove.allowInput || PlayerManager.LocalPlayerScript.playerMove.isGhost)
                    return;

            if (slot1.TrySetItem(slot2.Item)) {
				slot2.Clear();
			}
		}

		private InventorySlotCache Slots {
			get {
				return UIManager.InventorySlots;
			}
		}
	}
}