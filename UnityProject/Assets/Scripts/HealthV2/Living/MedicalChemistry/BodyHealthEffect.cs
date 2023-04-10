using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "BodyHealthEffect",
	menuName = "ScriptableObjects/Chemistry/Reactions/BodyHealthEffect")]
public class BodyHealthEffect : MetabolismReaction
{
	[HideIf("MultiEffect")] public DamageType DamageEffect;
	[HideIf("MultiEffect")] public AttackType AttackType;

	[FormerlySerializedAs("EffectPerOne")]
	[Tooltip("How much damage or heals If negative per 1u")]
	[HideIf("MultiEffect")]
	public float AttackBodyPartPerOneU = 1;


	public bool CanOverdose = true;

	[ShowIf(nameof(CanOverdose))] public float ConcentrationBloodOverdose = 20f;
	[ShowIf(nameof(CanOverdose))] public float OverdoseDamageMultiplier = 1;

	public bool MultiEffect = false;

	[ShowIf(nameof(MultiEffect))]public List<TypeAndStrength> Effects = new List<TypeAndStrength>();


	[System.Serializable]
	public struct TypeAndStrength
	{
		public DamageType DamageEffect;
		public AttackType AttackType;

		[Tooltip("How much damage or heals If negative per 1u")]
		public float EffectPerOne;
	}

	[System.NonSerialized]
	public List<MetabolismComponent> DamagedList = new List<MetabolismComponent>(); //Not multithread safe

	public override void PossibleReaction(List<MetabolismComponent> senders, ReagentMix reagentMix,
		float reactionMultiple, float BodyReactionAmount, float TotalChemicalsProcessed, out bool overdose) //limitedReactionAmountPercentage = 0 to 1
	{
		overdose = false;
		DamagedList.Clear(); //Why? So healing medicine is never wasted Is a pain in butt though to work out
		if ((CanOverdose && TotalChemicalsProcessed > ConcentrationBloodOverdose) == false)
		{
			foreach (var bodyPart in senders)
			{
				if (MultiEffect)
				{
					foreach (var Effect in Effects)
					{
						if (Effect.EffectPerOne < 0 && bodyPart.RelatedPart.GetDamage(Effect.DamageEffect) > 0)
						{
							if (DamagedList.Contains(bodyPart) == false)
							{
								DamagedList.Add(bodyPart);
							}
						}
					}
				}
				else
				{
					if (AttackBodyPartPerOneU < 0 && bodyPart.RelatedPart.GetDamage(DamageEffect) > 0)
					{
						DamagedList.Add(bodyPart);
					}
				}
			}
		}

		var Toloop = senders;

		if (DamagedList.Count > 0)
		{
			Toloop = DamagedList;
			float ProcessingAmount = 0;
			foreach (var bodyPart in Toloop)
			{
				ProcessingAmount += bodyPart.ReagentMetabolism * bodyPart.BloodThroughput * bodyPart.CurrentBloodSaturation;
			}

			if (TotalChemicalsProcessed > ProcessingAmount)
			{
				reactionMultiple *= (ProcessingAmount / TotalChemicalsProcessed);
				TotalChemicalsProcessed = 0f;
				foreach (var ingredient in ingredients.m_dict)
				{
					TotalChemicalsProcessed += (ingredient.Value * reactionMultiple);
				}
			}

			BodyReactionAmount = ProcessingAmount;
		}

		foreach (var bodyPart in Toloop)
		{
			var Individual = bodyPart.ReagentMetabolism * bodyPart.BloodThroughput * bodyPart.CurrentBloodSaturation;

			var PercentageOfProcess = Individual / BodyReactionAmount;


			var TotalChemicalsProcessedByBodyPart = (TotalChemicalsProcessed * ReagentMetabolismMultiplier)  * PercentageOfProcess;

			if (CanOverdose)
			{
				if (TotalChemicalsProcessed > ConcentrationBloodOverdose)
				{
					overdose = true;
					if (MultiEffect)
					{
						foreach (var Effect in Effects)
						{
							bodyPart.RelatedPart.TakeDamage(null,
								Effect.EffectPerOne * TotalChemicalsProcessedByBodyPart * -OverdoseDamageMultiplier,
								Effect.AttackType,
								Effect.DamageEffect, DamageSubOrgans: false);
						}
					}
					else
					{
						bodyPart.RelatedPart.TakeDamage(null,
							AttackBodyPartPerOneU * TotalChemicalsProcessedByBodyPart * -OverdoseDamageMultiplier,
							AttackType,
							DamageEffect, DamageSubOrgans: false);
					}
				}
			}

			if (overdose == false)
			{
				if (MultiEffect)
				{
					foreach (var Effect in Effects)
					{
						bodyPart.RelatedPart.TakeDamage(null, Effect.EffectPerOne * TotalChemicalsProcessedByBodyPart, Effect.AttackType,
							Effect.DamageEffect, DamageSubOrgans: false);
					}
				}
				else
				{
					bodyPart.RelatedPart.TakeDamage(null, AttackBodyPartPerOneU * TotalChemicalsProcessedByBodyPart, AttackType,
						DamageEffect, DamageSubOrgans: false);
				}
			}
		}
		base.PossibleReaction(senders, reagentMix, reactionMultiple, BodyReactionAmount, TotalChemicalsProcessed, out overdose);
	}
}
