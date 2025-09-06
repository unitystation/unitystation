using System.Collections.Generic;
using Chemistry;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using Items.Implants.Organs;
using UnityEngine;

[CreateAssetMenu(fileName = "MetabolismReactionLoseFat",
	menuName = "ScriptableObjects/Chemistry/Reactions/MetabolismReactionLoseFat")]
public class MetabolismReactionLoseFat : BodyHealthEffect
{

	public float Multiplier = 10;

	public override void PossibleReaction(List<MetabolismComponent> senders, ReagentMix reagentMix,
		float reactionMultiple, float BodyReactionAmount, float TotalChemicalsProcessed, ref bool overdose)
	{
		overdose = false;
		var Toloop = senders;
		foreach (var bodyPart in Toloop)
		{
			float fatLeft = 0;
			var Individual = bodyPart.ReagentMetabolism * bodyPart.BloodThroughput * bodyPart.CurrentBloodSaturation;

			var PercentageOfProcess = Individual / BodyReactionAmount;


			var TotalChemicalsProcessedByBodyPart =
				(TotalChemicalsProcessed * ReagentMetabolismMultiplier) * PercentageOfProcess;

			var BodyPart = bodyPart.GetComponentCustom<BodyFat>();
			if (BodyPart != null)
			{

				fatLeft += BodyPart.AbsorbedAmount;
				var Total = BodyPart.AbsorbedAmount;
				Total -= TotalChemicalsProcessedByBodyPart * Multiplier;
				BodyPart.SetAbsorbedAmount(Total);

				if (fatLeft < 0.4)
				{
					processDamageCalculation(overdose, bodyPart, TotalChemicalsProcessedByBodyPart * 5);
				}
				else
				{
					processDamageCalculation(overdose, bodyPart, TotalChemicalsProcessedByBodyPart);
				}
			}
		}

		base.PossibleReaction(senders, reagentMix, reactionMultiple, BodyReactionAmount, TotalChemicalsProcessed, ref overdose);
	}
}
