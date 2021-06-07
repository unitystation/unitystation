using System.Collections.Generic;
using System.Linq;
using Chemistry;
using Chemistry.Components;
using ScriptableObjects;
using UnityEngine;

namespace HealthV2
{
	public class Liver : BodyPartModification
	{
		/// <summary>
		///ReagentContainer which the liver uses to hold reagents it will process. Reagents like achohol will be broken down into their ethanol reagent via ReactionSet
		/// </summary>
		private ReagentContainer processingContainer;

		/// <summary>
		/// Alchoholic reagents that the liver will process, override to define what the liver will accept to break down
		/// </summary>
		[Tooltip("Alchoholic reagents that the liver will process")]
		[SerializeField] private AlcoholicDrinksSOScript Alchohols;

		/// <summary>
		/// Reagent that 'alchohols' are assumed to be processed into
		/// </summary>
		[Tooltip("Reagent that 'alchohols' are assumed to be processed into")]
		[SerializeField] private Reagent ethanolReagent;

		/// <summary>
		/// Reagents toxic to the race this bodypart relates to.
		/// </summary>
		[Tooltip("Reagents toxic to the race this bodypart relates to.")]
		[SerializeField] private List<Reagent> Toxins;

		/// <summary>
		/// Amount of reagents liver will attempt to process. Affected by bodypart efficiency
		/// </summary>
		[SerializeField] private float processAmount;

		public override void SetUpSystems()
		{
			base.SetUpSystems();
			processingContainer = GetComponent<ReagentContainer>();
		}

		public override void ImplantPeriodicUpdate()
		{
			//take what we are gonna process, out of the blood
			foreach (Reagent reagent in RelatedPart.BloodContainer.CurrentReagentMix.reagents.Keys)
			{
				if (Alchohols.AlcoholicReagents.Contains(reagent) || Toxins.Contains(reagent))
				{
					//add to processingContainer first to avoid intermediate variable
					processingContainer.CurrentReagentMix.Add(reagent, RelatedPart.BloodContainer.CurrentReagentMix[reagent]);

					//remove from bloodstream
					RelatedPart.BloodContainer.CurrentReagentMix.Remove(reagent,
						RelatedPart.BloodContainer.CurrentReagentMix[reagent]);
				}
			}
		}
	}
}