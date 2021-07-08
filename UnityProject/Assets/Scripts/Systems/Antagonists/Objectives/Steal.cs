using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Antagonists
{
	/// <summary>
	/// An objective to steal items from the station
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/Steal")]
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
		/// Make sure there's at least one item which hasn't been targeted
		/// </summary>
		protected override bool IsPossibleInternal(PlayerScript candidate)
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
				                " Definitely a programming bug. ", Category.Antags);
				return;
			}
			ItemName = itemEntry.Key.Item().InitialName;

			if (string.IsNullOrEmpty(ItemName))
			{
				Logger.LogError($"Objective steal item target failed because the InitialName has not been" +
				                $" set on this objects ItemAttributes. " +
				                $"Item: {itemEntry.Key.Item().gameObject.name}", Category.Antags);
				return;
			}
			Amount = itemEntry.Value;
			AntagManager.Instance.TargetedItems.Add(itemEntry.Key);
			// TODO randomise amount based on range/weightings?
			description = $"Steal {Amount} {ItemName}";
		}

		protected override bool CheckCompletion()
		{
			return CheckStorageFor(ItemName, Amount);
		}
	}
}
