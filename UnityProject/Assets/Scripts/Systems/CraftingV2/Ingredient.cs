using System;
using Items;
using UnityEngine;

namespace Systems.CraftingV2
{
	[Serializable]
	public class Ingredient
	{
		[SerializeField]
		[Min(1)]
		private int requiredAmount = 1;

		public int RequiredAmount => requiredAmount;

		[SerializeField]
		private ItemAttributesV2 requiredItem;

		public ItemAttributesV2 RequiredItem => requiredItem;
	}
}