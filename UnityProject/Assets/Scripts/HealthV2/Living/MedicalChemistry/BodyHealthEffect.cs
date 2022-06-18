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
	[HideIf("MultiEffect")] public float AttackBodyPartPerOneU = 1;



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



	public override void PossibleReaction(List<BodyPart> senders, ReagentMix reagentMix, float LimitedreactionAmount) //LimitedreactionAmount = 0 to 1
	{
		if (CanOverdose)
		{
			float TotalIn = 0;
			foreach (var reagent in ingredients.m_dict)
			{
				TotalIn += reagentMix[reagent.Key];
			}


			if (TotalIn > ConcentrationBloodOverdose)
			{
				if (MultiEffect)
				{
					//
					foreach (var Effect in Effects)
					{
						foreach (var sender in senders)
						{
							ReagentMetabolism * bloodThroughput * TotalModified

							sender.TakeDamage(null, Effect.EffectPerOne  * LimitedreactionAmount * -OverdoseDamageMultiplier, Effect.AttackType,
								Effect.DamageEffect, DamageSubOrgans: false);
						}

					}

				}
				else
				{
					senders.TakeDamage(null, AttackBodyPartPerOneU * LimitedreactionAmount * -OverdoseDamageMultiplier, AttackType,
						DamageEffect, DamageSubOrgans: false);
				}

				base.PossibleReaction(senders, reagentMix, LimitedreactionAmount);
				return;
			}
		}

		if (MultiEffect)
		{
			foreach (var Effect in Effects)
			{
				senders.TakeDamage(null, Effect.EffectPerOne  * LimitedreactionAmount , Effect.AttackType,
					Effect.DamageEffect, DamageSubOrgans: false);
			}

		}
		else
		{
			senders.TakeDamage(null, AttackBodyPartPerOneU  * LimitedreactionAmount, AttackType,
				DamageEffect, DamageSubOrgans: false);
		}

		base.PossibleReaction(senders, reagentMix, LimitedreactionAmount);
	}
}