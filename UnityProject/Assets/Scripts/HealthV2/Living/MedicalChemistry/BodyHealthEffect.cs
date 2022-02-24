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

	[ShowIf(nameof(CanOverdose))] public float PercentageBloodOverdose = 0.25f;
	[ShowIf(nameof(CanOverdose))] public float OverdoseDamageMultiplier = 1;

	public bool MultiEffect = false;

	[ShowIf(nameof(MultiEffect))] public List<TypeAndStrength> Effects = new List<TypeAndStrength>();


	public const int MagicNumber = 15; // This balance is about right with 1 u ingested 1 * effect it about does one damage

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
						sender.TakeDamage(null, Effect.EffectPerOne * MagicNumber * LimitedreactionAmount * -OverdoseDamageMultiplier, Effect.AttackType,
							Effect.DamageEffect, DamageSubOrgans: false);
					}

				}
				else
				{
					sender.TakeDamage(null, AttackBodyPartPerOneU * MagicNumber* LimitedreactionAmount * -OverdoseDamageMultiplier, AttackType,
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
				sender.TakeDamage(null, Effect.EffectPerOne * MagicNumber * LimitedreactionAmount , Effect.AttackType,
					Effect.DamageEffect, DamageSubOrgans: false);
			}

		}
		else
		{
			sender.TakeDamage(null, AttackBodyPartPerOneU * MagicNumber * LimitedreactionAmount, AttackType,
				DamageEffect, DamageSubOrgans: false);
		}

		base.PossibleReaction(sender, reagentMix, LimitedreactionAmount);
	}
}