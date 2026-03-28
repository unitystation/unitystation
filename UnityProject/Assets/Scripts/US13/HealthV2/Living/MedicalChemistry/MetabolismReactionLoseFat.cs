using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;
using US13.Items.Implants.Organs;
using Util;

namespace US13.HealthV2.Living.MedicalChemistry
{
	[CreateAssetMenu(fileName = "MetabolismReactionLoseFat",
		menuName = "ScriptableObjects/Chemistry/Reactions/MetabolismReactionLoseFat")]
	public class MetabolismReactionLoseFat : BodyHealthEffect
	{

		public float Multiplyer = 10;

		public override void PossibleReaction(List<MetabolismComponent> senders, ReagentMix reagentMix,
			float reactionMultiple, float BodyReactionAmount, float TotalChemicalsProcessed, float UntouchedMultiple, ref bool overdose)
		{
			overdose = false;
			bool lowFat = true;
			var Toloop = senders;
			foreach (var bodyPart in Toloop)
			{
				var BodyPart = bodyPart.GetCachedComponent<BodyFat>();
				if (BodyPart != null)
				{
					if (BodyPart.AbsorbedAmount > 0.4)
					{
						lowFat = false;
					}
				}
			}
			reactionMultiple = Mathf.Min(reactionMultiple * ReagentMetabolismMultiplier, UntouchedMultiple);
			foreach (var bodyPart in Toloop)
			{

				var Individual = bodyPart.ReagentMetabolism * bodyPart.BloodThroughput * bodyPart.CurrentBloodSaturation;

				var PercentageOfProcess = Individual / BodyReactionAmount;

				var TotalChemicalsProcessedByBodyPart =
					(TotalChemicalsProcessed * ReagentMetabolismMultiplier) * PercentageOfProcess;

				if (lowFat)
				{
					processDamageCalculation(overdose, bodyPart, TotalChemicalsProcessedByBodyPart * 5);
				}
				else
				{
					processDamageCalculation(overdose, bodyPart, TotalChemicalsProcessedByBodyPart);
				}

				var BodyPart = bodyPart.GetCachedComponent<BodyFat>();
				if (BodyPart != null)
				{
					var Total = BodyPart.AbsorbedAmount;
					Total -= TotalChemicalsProcessed * Multiplyer;
					BodyPart.SetAbsorbedAmount(Total);
				}
			}

			base.PossibleReaction(senders, reagentMix, reactionMultiple, BodyReactionAmount, TotalChemicalsProcessed, UntouchedMultiple, ref overdose);
		}
	}
}
