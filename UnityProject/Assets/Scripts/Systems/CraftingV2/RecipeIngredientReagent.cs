using System;
using Chemistry;
using UnityEngine;

namespace Systems.CraftingV2
{
	/// <summary>
	/// A pair of values: The reagent itself as Reagent and its volume.
	/// </summary>
	[Serializable]
	public class IngredientReagent
	{
		[SerializeField] [Min(float.MinValue)] [Tooltip("The amount of required reagent.")]
		private float requiredAmount = 1;

		/// <summary>
		/// The amount of required reagent.
		/// </summary>
		public float RequiredAmount => requiredAmount;

		[SerializeField] [Tooltip("The required reagent.")]
		private Reagent requiredReagent;

		/// <summary>
		/// The required reagent
		/// </summary>
		public Reagent RequiredReagent => requiredReagent;
	}
}