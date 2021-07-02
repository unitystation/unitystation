using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

namespace Clothing
{
	public class ClothingSlots : MonoBehaviour, IDynamicItemSlotS, IServerInventoryMove, IServerDespawn
	{
		public NamedSlotFlagged NamedSlotFlagged;

		private DynamicItemStorage ItemStorage;
		public GameObject GameObject => gameObject;
		public ItemStorage RelatedStorage => relatedStorage;


		[Tooltip("Specify which item storage this uses since, there can be multiple on one game object")]
		[SerializeField]
		private ItemStorage relatedStorage;

		public List<BodyPartUISlots.StorageCharacteristics> Storage => storage;

		[SerializeField] private List<BodyPartUISlots.StorageCharacteristics> storage;

		private ItemSlot ActiveInSlot;

		public void OnInventoryMoveServer(InventoryMove info)
		{
			//Wearing
			if (info.ToSlot?.NamedSlot != null)
			{
				ItemStorage = info.ToRootPlayer?.PlayerScript.DynamicItemStorage;

				if (ItemStorage != null &&
				    NamedSlotFlagged.HasFlag(
					    ItemSlot.GetFlaggedSlot(info.ToSlot.NamedSlot.GetValueOrDefault(NamedSlot.outerwear))))
				{
					AddSelf(ItemStorage);
					return;
				}
			}

			//taking off
			if (info.FromSlot?.NamedSlot != null)
			{
				ItemStorage = info.FromRootPlayer?.PlayerScript.DynamicItemStorage;

				if (ItemStorage != null &&
				    NamedSlotFlagged.HasFlag(
					    ItemSlot.GetFlaggedSlot(info.FromSlot.NamedSlot.GetValueOrDefault(NamedSlot.outerwear))))
				{
					RemoveSelf(ItemStorage);
					return;
				}
			}
		}

		/// <summary>
		/// Removes itself from dynamic storage
		/// </summary>
		/// <param name="dynamicItemStorage"></param>
		public void RemoveSelf(DynamicItemStorage dynamicItemStorage)
		{
			dynamicItemStorage.Remove(this);
		}

		/// <summary>
		/// Adds itself to dynamic storage
		/// </summary>
		/// <param name="dynamicItemStorage"></param>
		public void AddSelf(DynamicItemStorage dynamicItemStorage)
		{
			dynamicItemStorage.Add(this);
		}


		public void OnDespawnServer(DespawnInfo info)
		{
			if (ItemStorage != null)
			{
				RemoveSelf(ItemStorage);
			}
		}
	}
}