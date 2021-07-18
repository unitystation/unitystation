using System;
using Chemistry;
using UnityEngine;

namespace Systems.CraftingV2
{
	/// <summary>
	///     A pair of values: The reagent itself as Reagent and its volume.
	/// </summary>
	[Serializable]
	public class RecipeIngredientReagent
	{
		[SerializeField] [Min(float.MinValue)] [Tooltip("The amount of required reagent.")]
		private float requiredAmount = 1;

		[SerializeField] [Tooltip("The required reagent.")]
		private Reagent requiredReagent;

		/// <summary>
		///     The amount of required reagent.
		/// </summary>
		public float RequiredAmount => requiredAmount;

		/// <summary>
		///     The required reagent
		/// </summary>
		public Reagent RequiredReagent => requiredReagent;
	}
}