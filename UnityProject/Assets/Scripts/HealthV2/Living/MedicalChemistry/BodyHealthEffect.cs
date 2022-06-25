using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
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

	[ShowIf(nameof(MultiEffect))] public List<TypeAndStrength> Effects = new List<TypeAndStrength>();


	[System.Serializable]
	public struct TypeAndStrength
	{
		public DamageType DamageEffect;
		public AttackType AttackType;

		[Tooltip("How much damage or heals If negative per 1u")]
		public float EffectPerOne;
	}


	public override void PossibleReaction(List<BodyPart> senders, ReagentMix reagentMix,
		float reactionMultiple, float BodyReactionAmount) //limitedReactionAmountPercentage = 0 to 1
	{

		bool Overdose = false;
		float TotalIn = 0;
		foreach (var reagent in ingredients.m_dict)
		{
			TotalIn += reagent.Value * reactionMultiple ;
		}

		var TotalAppliedHealing = 0f;
		foreach (var bodyPart in senders)
		{

			//TODO Do not waste reagents, Unless they cannot heal anything
			var Individual = bodyPart.ReagentMetabolism * bodyPart.BloodThroughput *bodyPart.currentBloodSaturation * Mathf.Max(0.10f, bodyPart.TotalModified) * ReagentMetabolismMultiplier;

			var PercentageOfProcess = Individual / BodyReactionAmount;


			var TotalChemicalsProcessedByBodyPart = TotalIn * PercentageOfProcess;

			if (CanOverdose)
			{
				if (TotalIn > ConcentrationBloodOverdose)
				{
					Overdose = true;
					if (MultiEffect)
					{
						foreach (var Effect in Effects)
						{
							bodyPart.TakeDamage(null,
								Effect.EffectPerOne * TotalChemicalsProcessedByBodyPart * -OverdoseDamageMultiplier,
								Effect.AttackType,
								Effect.DamageEffect, DamageSubOrgans: false);
						}
					}
					else
					{
						bodyPart.TakeDamage(null,
							AttackBodyPartPerOneU * TotalChemicalsProcessedByBodyPart * -OverdoseDamageMultiplier,
							AttackType,
							DamageEffect, DamageSubOrgans: false);
					}
				}
			}

			if (Overdose == false)
			{
				if (MultiEffect)
				{
					foreach (var Effect in Effects)
					{
						bodyPart.TakeDamage(null, Effect.EffectPerOne * TotalChemicalsProcessedByBodyPart, Effect.AttackType,
							Effect.DamageEffect, DamageSubOrgans: false);
					}
				}
				else
				{
					TotalAppliedHealing += AttackBodyPartPerOneU* TotalChemicalsProcessedByBodyPart;
					bodyPart.TakeDamage(null, AttackBodyPartPerOneU * TotalChemicalsProcessedByBodyPart, AttackType,
						DamageEffect, DamageSubOrgans: false);
				}
			}
		}

		Logger.LogError("TotalAppliedHealing > " + TotalAppliedHealing);
		base.PossibleReaction(senders, reagentMix, reactionMultiple, BodyReactionAmount);
	}
}