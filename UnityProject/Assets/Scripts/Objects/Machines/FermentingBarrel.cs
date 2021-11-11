using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using AddressableReferences;
using Effects;
using Items;
using Items.Food;
using Systems.Botany;
using Chemistry;
using Chemistry.Components;

namespace Objects.Kitchen
{
	/// <summary>
	/// A barrel into which players can insert certain food items.
	/// Upon being inserted, they will be fermented into certain reagents.
	/// </summary>
	public class FermentingBarrel : NetworkBehaviour
	{
		public ReagentContainer container;
		[SerializeField] private AddressableAudioSource closeSound = null;
		[SerializeField] private AddressableAudioSource openSound = null;
		private ItemStorage itemStorage;
		public bool Closed => closed;
		private bool closed = true;
		private RegisterTile registerTile;
		private Vector3Int WorldPosition => registerTile.WorldPosition;

		[SerializeField] private SpriteHandler barrelSpriteHandler;

		/// <summary>
		/// Set up the AudioSource.
		/// </summary>
		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			container = GetComponent<ReagentContainer>();
			itemStorage = GetComponent<ItemStorage>();
		}
		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, Update);
		}

		public void AddItem(ItemSlot fromSlot)
		{
			if (fromSlot == null || fromSlot.IsEmpty || fromSlot.ItemObject.GetComponent<Fermentable>() == null || closed) return;

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

		public void OpenClose()
		{
			if(closed)
			{
				UpdateManager.Remove(CallbackType.UPDATE, Update);
				SoundManager.PlayNetworkedAtPos(openSound, WorldPosition, sourceObj: gameObject);
				barrelSpriteHandler.ChangeSprite(0);
				foreach (var slot in itemStorage.GetItemSlots())
				{
					Inventory.ServerDrop(slot);
				}
			}
			else
			{
				UpdateManager.Add(CallbackType.UPDATE, Update);
				SoundManager.PlayNetworkedAtPos(closeSound, WorldPosition, sourceObj: gameObject);
				barrelSpriteHandler.ChangeSprite(1);
			}
			closed = !closed;
		}

		public void Update()
		{
			foreach (var slot in itemStorage.GetItemSlots())
			{
				if (slot.IsEmpty == true) break;

				if(closed){
					var fermentable = slot.ItemObject.GetComponent<Fermentable>();
					if (fermentable == null)
					{
						Inventory.ServerDrop(slot);
						continue;
					}

					if(fermentable.AddFermentingTime(Time.deltaTime) == false) continue;

					var grownItem = slot.ItemObject.GetComponent<GrownFood>();
					if (grownItem == null)
					{
						foreach(var reagent in fermentable.FermentedReagents)
						{
							container.Add(new ReagentMix(reagent.Key, reagent.Value, 293));
						}
					}
					else
					{
						var amount = grownItem.GetPlantData().Potency/4;
						foreach(var reagent in fermentable.FermentedReagents)
						{
							container.Add(new ReagentMix(reagent.Key, amount, 293));
						}
					}
					var item = slot.ItemObject;
					_ = Despawn.ServerSingle(item);
				}
			}
		}
	}
}
