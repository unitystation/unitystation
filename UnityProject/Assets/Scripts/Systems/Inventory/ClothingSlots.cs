using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

namespace Clothing
{
	public class ClothingSlots : MonoBehaviour, IDynamicItemSlotS, IServerInventoryMove
	{
		public NamedSlotFlagged NamedSlotFlagged;

		private DynamicItemStorage ItemStorage;
		public GameObject GameObject => gameObject;
		public ItemStorage RelatedStorage => relatedStorage;

		[SerializeField] private ItemStorage relatedStorage;

		public List<BodyPartUISlots.StorageCharacteristics> Storage => storage;

		[SerializeField] private List<BodyPartUISlots.StorageCharacteristics> storage;

		private ItemSlot ActiveInSlot;

		public void OnInventoryMoveServer(InventoryMove info)
		{
			//Wearing
			if (info.ToSlot != null & info.ToSlot?.NamedSlot != null)
			{
				ItemStorage = info.ToRootPlayer?.PlayerScript.ItemStorage;

				if (ItemStorage != null &&
				    NamedSlotFlagged.HasFlag(
					    ItemSlot.GetFlaggedSlot(info.ToSlot.NamedSlot.GetValueOrDefault(NamedSlot.outerwear))))
				{
					AddSelf(ItemStorage);
					return;
				}
			}

			//taking off
			if (info.FromSlot != null & info.FromSlot?.NamedSlot != null)
			{
				ItemStorage = info.FromRootPlayer?.PlayerScript.ItemStorage;

				if (ItemStorage != null &&
				    NamedSlotFlagged.HasFlag(
					    ItemSlot.GetFlaggedSlot(info.FromSlot.NamedSlot.GetValueOrDefault(NamedSlot.outerwear))))
				{
					RemoveSelf(ItemStorage);
					return;
				}
			}
		}


		public void RemoveSelf(DynamicItemStorage dynamicItemStorage)
		{
			dynamicItemStorage.Remove(this);
		}

		public void AddSelf(DynamicItemStorage dynamicItemStorage)
		{
			dynamicItemStorage.Add(this);
		}
	}
}