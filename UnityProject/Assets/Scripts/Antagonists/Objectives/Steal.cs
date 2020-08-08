using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Antagonists
{
	/// <summary>
	/// An objective to steal items from the station
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/Objectives/Steal")]
	public class Steal : Objective
	{
		/// <summary>
		/// The pool of possible items to steal
		/// </summary>
		[SerializeField]
		private ItemDictionary ItemPool = null;

		/// <summary>
		/// The item to steal
		/// </summary>
		private string ItemName;

		/// <summary>
		/// The number of items needed to complete the objective
		/// </summary>
		private int Amount;

		/// <summary>
		/// The amount that has been found by the completion check so far.
		/// </summary>
		private int CheckAmount = 0;

		/// <summary>
		/// Make sure there's at least one item which hasn't been targeted
		/// </summary>
		public override bool IsPossible(PlayerScript candidate)
		{
			// Get all items from the item pool which haven't been targeted already
			int itemCount = ItemPool.Where( itemDict =>
				!AntagManager.Instance.TargetedItems.Contains(itemDict.Key)).Count();
			return (itemCount > 0);
		}

		/// <summary>
		/// Choose a target item from the item pool (no duplicates)
		/// </summary>
		protected override void Setup()
		{
			// Get all items from the item pool which haven't been targeted already
			var possibleItems = ItemPool.Where( itemDict =>
				!AntagManager.Instance.TargetedItems.Contains(itemDict.Key)).ToList();

			if (possibleItems.Count == 0)
			{
				Logger.LogWarning("Unable to find any suitable items to steal! Giving free objective", Category.Antags);
				description = "Free objective";
				Complete = true;
				return;
			}

			// Pick a random item and add it to the targeted list
			var itemEntry = possibleItems.PickRandom();
			if (itemEntry.Key == null)
			{
				Logger.LogError($"Objective steal item target failed because the item chosen is somehow destroyed." +
				                " Definitely a programming bug. ", Category.Round);
				return;
			}
			ItemName = itemEntry.Key.Item().InitialName;

			if (string.IsNullOrEmpty(ItemName))
			{
				Logger.LogError($"Objective steal item target failed because the InitialName has not been" +
				                $" set on this objects ItemAttributes. " +
				                $"Item: {itemEntry.Key.Item().gameObject.name}", Category.Round);
				return;
			}
			Amount = itemEntry.Value;
			AntagManager.Instance.TargetedItems.Add(itemEntry.Key);
			// TODO randomise amount based on range/weightings?
			description = $"Steal {Amount} {ItemName}";
		}

		/// <summary>
		/// Checks through all the storage recursively
		/// </summary>
		protected override bool CheckCompletion()
		{
			return CheckStorage(Owner.body.ItemStorage);
		}

		private bool CheckStorage(ItemStorage itemStorage)
		{
			foreach (var slot in itemStorage.GetItemSlots())
			{
				if (CheckSlot(slot))
				{
					return true;
				}
			}

			return false;
		}

		private bool CheckSlot(ItemSlot slot)
		{
			if (slot.ItemObject == null) return false;

			//Check if current Item is the one we need
			if (slot.ItemObject.GetComponent<ItemAttributesV2>()?.InitialName == ItemName)
			{
				//If stackable count stack
				if (slot.ItemObject.TryGetComponent<Stackable>(out var stackable))
				{
					CheckAmount += stackable.Amount;
				}
				else
				{
					CheckAmount++;
				}

				//Check to see if count has been passed
				if (CheckAmount >= Amount)
				{
					return true;
				}
			}

			//Check to see if this item has storage, and do checks on that
			if (slot.ItemObject.TryGetComponent<ItemStorage>(out var itemStorage))
			{
				if (CheckStorage(itemStorage))
				{
					return true;
				}
			}

			return false;
		}
	}
}