using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
	public class FermentingBarrel : MonoBehaviour
	{
		public ReagentContainer container;
		[SerializeField] private AddressableAudioSource closeSound = null;
		[SerializeField] private AddressableAudioSource openSound = null;
		[SerializeField] private AddressableAudioSource fermentedBubble = null;
		private ItemStorage itemStorage;
		public bool Closed => closed;
		private bool closed = true;
		private RegisterTile registerTile;
		private Vector3Int WorldPosition => registerTile.WorldPosition;
		private readonly Dictionary<ItemSlot, Fermentable> storedFermentables = new Dictionary<ItemSlot, Fermentable>();

		[SerializeField] private SpriteHandler barrelSpriteHandler;
		public IEnumerable<ItemSlot> Slots => itemStorage.GetItemSlots();
		public bool HasContents => Slots.Any(Slot => Slot.IsOccupied);
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
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		public void AddItem(ItemSlot fromSlot)
		{
			if (fromSlot == null || fromSlot.IsEmpty || fromSlot.ItemObject.GetComponent<Fermentable>() == null || closed) return;

			ItemSlot storageSlot = itemStorage.GetNextFreeIndexedSlot();
			bool added = false;
			// If there's a stackable component, add one at a time.
			Stackable stack = fromSlot.ItemObject.GetComponent<Stackable>();
			if (stack == null || stack.Amount == 1)
			{
				added = Inventory.ServerTransfer(fromSlot, storageSlot);
			}
			else {
				var item = stack.ServerRemoveOne();
				Inventory.ServerAdd(item, storageSlot);
			}
			if(storageSlot.ItemObject.TryGetComponent(out Fermentable fermentable))
			{
				storedFermentables.Add(storageSlot, fermentable);
			}
		}

		public void OpenClose()
		{
			if(closed)
			{
				UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
				SoundManager.PlayNetworkedAtPos(openSound, WorldPosition, sourceObj: gameObject);
				barrelSpriteHandler.ChangeSprite(0);
				itemStorage.ServerDropAll();
				storedFermentables.Clear();
			}
			else
			{
				UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
				SoundManager.PlayNetworkedAtPos(closeSound, WorldPosition, sourceObj: gameObject);
				barrelSpriteHandler.ChangeSprite(1);
			}
			closed = !closed;
		}

		public void UpdateMe()
		{
			for (int i = storedFermentables.Count - 1; i >= 0; i--)
			{
				var slot = storedFermentables.Keys.ElementAt(i);
				var fermentable = storedFermentables[slot];
				if (slot.IsEmpty == true) continue;
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
				SoundManager.PlayNetworkedAtPos(fermentedBubble, WorldPosition, sourceObj: gameObject);
				var item = slot.ItemObject;
				_ = Despawn.ServerSingle(item);
				storedFermentables.Remove(slot);
			}
		}
	}
}
