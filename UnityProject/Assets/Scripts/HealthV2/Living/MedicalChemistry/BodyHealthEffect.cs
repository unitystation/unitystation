using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "BodyHealthEffect",
	menuName = "ScriptableObjects/Chemistry/Reactions/BodyHealthEffect")]
public class BodyHealthEffect : MetabolismReaction
{
	[HideIf("MultiEffect")] public DamageType DamageEffect;
	[HideIf("MultiEffect")] public AttackType AttackType;

	[Tooltip("How much damage or heals If negative per 1u")]
	[HideIf("MultiEffect")] public float EffectPerOne = 1;



	public bool CanOverdose = true;

	[ShowIf("CanOverdose")] public float PercentageBloodOverdose = 0.25f;
	[ShowIf("CanOverdose")] public float OverdoseDamageMultiplier = 1;

	public bool MultiEffect = false;

	[ShowIf("MultiEffect")] public List<TypeAndStrength> Effects = new List<TypeAndStrength>();

	[System.Serializable]
	public struct TypeAndStrength
	{
		public DamageType DamageEffect;
		public AttackType AttackType;

		[Tooltip("How much damage or heals If negative per 1u")]
		public float EffectPerOne;
	}


	public override void PossibleReaction(BodyPart sender, ReagentMix reagentMix, float LimitedreactionAmount)
	{
		if (CanOverdose)
		{
			float TotalIn = 0;
			foreach (var reagent in ingredients.m_dict)
			{
				TotalIn += reagentMix[reagent.Key];
			}

			float Percentage = TotalIn / reagentMix.Total;

			if (Percentage > PercentageBloodOverdose)
			{
				if (MultiEffect)
				{
					foreach (var Effect in Effects)
					{
						sender.TakeDamage(null, Effect.EffectPerOne * 3 * LimitedreactionAmount * -OverdoseDamageMultiplier, Effect.AttackType,
							Effect.DamageEffect, DamageSubOrgans: false);
					}

				}
				else
				{
					sender.TakeDamage(null, EffectPerOne * 3 * LimitedreactionAmount * -OverdoseDamageMultiplier, AttackType,
						DamageEffect, DamageSubOrgans: false);
				}

				base.PossibleReaction(sender, reagentMix, LimitedreactionAmount);
				return;
			}
		}

		if (MultiEffect)
		{
			foreach (var Effect in Effects)
			{
				sender.TakeDamage(null, Effect.EffectPerOne * 3 * LimitedreactionAmount , Effect.AttackType,
					Effect.DamageEffect, DamageSubOrgans: false);
			}

		}
		else
		{
			sender.TakeDamage(null, EffectPerOne * 3 * LimitedreactionAmount, AttackType,
				DamageEffect, DamageSubOrgans: false);
		}

		base.PossibleReaction(sender, reagentMix, LimitedreactionAmount);
	}
}