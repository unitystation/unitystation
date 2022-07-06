using System;
using System.Collections.Generic;
using UnityEngine;
using AddressableReferences;
using Chemistry;
using Chemistry.Components;

namespace Objects.Kitchen
{
	/// <summary>
	/// A machine into which players can insert certain food items.
	/// Upon being inserted, they will be ground into another material.
	/// </summary>
	public class AIOGrinder : MonoBehaviour
	{
		public ReagentContainer Container => itemSlot != null && itemSlot.ItemObject != null
			? itemSlot.ItemObject.GetComponent<ReagentContainer>()
			: null;
		[SerializeField] private AddressableAudioSource grindSound = null;
		private ItemSlot itemSlot;
		private ItemStorage itemStorage;
		public bool GrindOrJuice => grindOrJuice;
		private bool grindOrJuice = true;
		private RegisterTile registerTile;
		private Vector3Int WorldPosition => registerTile.WorldPosition;


		private SpriteHandler spriteHandler;

		/// <summary>
		/// Set up the AudioSource.
		/// </summary>
		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			itemStorage = GetComponent<ItemStorage>();
			itemSlot = itemStorage.GetIndexedItemSlot(0);
			spriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		public void EjectContainer()
		{
			foreach (var slot in itemStorage.GetItemSlots())
			{
				Inventory.ServerDrop(itemSlot);
				spriteHandler.ChangeSprite(1);
			}
			return;
		}

		public void AddItem(ItemSlot fromSlot)
		{
			if (fromSlot == null || fromSlot.IsEmpty || ((fromSlot.ItemObject.GetComponent<Grindable>() == null && fromSlot.ItemObject.GetComponent<Juiceable>() == null) && !fromSlot.ItemAttributes.HasTrait(CommonTraits.Instance.Beaker))) return;


			if (itemSlot.IsEmpty)
			{
				if (fromSlot.Item.GetComponent<ReagentContainer>().TransferMode == TransferMode.Normal) // put beaker to slot
				{

					Inventory.ServerTransfer(fromSlot, itemStorage.GetIndexedItemSlot(0));
					spriteHandler.ChangeSprite(0);
					return;
				}
				return;
			}




			// If there's a stackable component, add one at a time.
			Stackable stack = fromSlot.ItemObject.GetComponent<Stackable>();
			if (stack == null || stack.Amount == 1)
			{
				Inventory.ServerTransfer(fromSlot, itemStorage.GetNextFreeIndexedSlot());
			}
			else {
				var item = stack.ServerRemoveOne();
				Inventory.ServerAdd(item, itemStorage.GetNextFreeIndexedSlot());
			}
		}

		public void SwitchMode()
		{
			grindOrJuice = !grindOrJuice;
			//make a message about switching into juice or grind mode
		}

		public void Activate()
		{
			SoundManager.PlayNetworkedAtPos(grindSound, WorldPosition, sourceObj: gameObject);
			foreach (var slot in itemStorage.GetItemSlots())
			{
				if (slot == itemSlot) continue;

				if (slot.IsEmpty == true) break;

				// If, somehow, something unprocessable is in the processor (should be impossible), spit it out.
				if(grindOrJuice){
					var grindable = slot.ItemObject.GetComponent<Grindable>();
					if (grindable == null)
					{
						Inventory.ServerDrop(slot);
						continue;
					}

					foreach(var reagent in grindable.GroundReagents.m_dict)
					{
						Container.Add(new ReagentMix(reagent.Key, reagent.Value, 293));
					}

					var item = slot.ItemObject;
					_ = Despawn.ServerSingle(item);
				}
				else
				{
					var juiceable = slot.ItemObject.GetComponent<Juiceable>();
					if (juiceable == null)
					{
						Inventory.ServerDrop(slot);
						continue;
					}

					foreach(var reagent in juiceable.JuicedReagents.m_dict)
					{
						Container.Add(new ReagentMix(reagent.Key, reagent.Value, 293));
					}

					var item = slot.ItemObject;
					_ = Despawn.ServerSingle(item);
				}
			}
		}
	}
}
