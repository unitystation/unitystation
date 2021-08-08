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
		[SerializeField] [Min(float.Epsilon)] [Tooltip("The amount of required reagent.")]
		private float requiredAmount;

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

		public RecipeIngredientReagent(Reagent reagent, float amount)
		{
			requiredReagent = reagent;
			requiredAmount = amount;
		}
	}
}