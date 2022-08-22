using System;
using UnityEngine;

namespace Systems.CraftingV2
{
	/// <summary>
	///     A pair of values: The ingredient itself as ItemAttributesV2 and its quantity
	/// </summary>
	[Serializable]
	public class RecipeIngredient : ICloneable
	{
		[SerializeField] [Min(1)] [Tooltip("The amount of required items.")]
		private int requiredAmount = 1;

		[SerializeField] [Tooltip("The required item. Includes all of its children (prefab variants).")]
		private GameObject requiredItem;

		/// <summary>
		///     The amount of required items.
		/// </summary>
		public int RequiredAmount => requiredAmount;

		/// <summary>
		///     The required item. Includes all of its children (prefab variants).
		/// </summary>
		public GameObject RequiredItem => requiredItem;

		public object Clone()
		{
			return new RecipeIngredient {requiredAmount = requiredAmount, requiredItem = requiredItem};
		}
	}
}