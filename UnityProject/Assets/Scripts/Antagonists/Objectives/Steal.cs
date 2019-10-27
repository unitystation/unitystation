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
		private ItemDictionary ItemPool;

		/// <summary>
		/// The item to steal
		/// </summary>
		private string ItemName;

		/// <summary>
		/// The number of items needed to complete the objective
		/// </summary>
		private int Amount;

		/// <summary>
		/// Perform initial setup of the objective if needed
		/// </summary>
		public override void Setup()
		{
			// Select a random item to steal
			int randIndex = Random.Range(0, ItemPool.Count);
			var entry = ItemPool.ElementAt(randIndex);
			ItemName = entry.Key.Item().itemName;
			Amount = entry.Value;
			// TODO randomise amount based on weightings/amount on station?
			description = $"Steal {Amount} {ItemName}";
		}

		public override bool IsComplete()
		{
			int count = 0;
			foreach (var item in Owner.body.playerNetworkActions.Inventory)
			{
				// TODO find better way to determine item types (ScriptableObjects/item IDs could work but would need to refactor all items)
				if (item.Value.ItemAttributes?.itemName == ItemName)
				{
					count++;
				}
			}
			// Check if the count is higher than the specified amount
			return count >= Amount;
		}
	}
}