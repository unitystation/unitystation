using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ExternalBodyHealthEffect",
	menuName = "ScriptableObjects/Chemistry/Reactions/ExternalBodyHealthEffect")]
public class ExternalBodyHealthEffect : MetabolismReaction
{
	[HideIf("MultiEffect")] public DamageType DamageEffect;
	[HideIf("MultiEffect")] public AttackType AttackType;

	[FormerlySerializedAs("EffectPerOne")]
	[Tooltip("How much damage or heals If negative per 1u")]
	[HideIf("MultiEffect")]
	public float AttackBodyPartPerOneU = 1;


	public bool CanOverdose = true;

	[ShowIf(nameof(CanOverdose))] public float ConcentrationOverdose = 20f;
	[ShowIf(nameof(CanOverdose))] public float OverdoseDamageMultiplier = 1;

	public bool MultiEffect = false;

	[ShowIf(nameof(MultiEffect))]
	public List<BodyHealthEffect.TypeAndStrength> Effects = new List<BodyHealthEffect.TypeAndStrength>();

	public bool HasInitialTouchCharacteristics = false;

	[ShowIf(nameof(HasInitialTouchCharacteristics))] public List<BodyHealthEffect.TypeAndStrength> InitialTouchCharacteristics = new List<BodyHealthEffect.TypeAndStrength>();

	public override void PossibleReaction(List<MetabolismComponent> senders, ReagentMix reagentMix,
		float reactionMultiple, float BodyReactionAmount,
		float TotalChemicalsProcessed, out bool overdose) //limitedReactionAmountPercentage = 0 to 1
	{

		base.PossibleReaction(senders, reagentMix, reactionMultiple, BodyReactionAmount, TotalChemicalsProcessed, out overdose);

		foreach (var bodyPart in senders)
		{
			if (CanOverdose)
			{
				if (TotalChemicalsProcessed > ConcentrationOverdose)
				{
					if (MultiEffect)
					{
						foreach (var Effect in Effects)
						{
							bodyPart.RelatedPart.TakeDamage(null,
								Effect.EffectPerOne * -OverdoseDamageMultiplier * ReagentMetabolismMultiplier,
								Effect.AttackType,
								Effect.DamageEffect, DamageSubOrgans: false);
						}
					}
					else
					{
						bodyPart.RelatedPart.TakeDamage(null,
							AttackBodyPartPerOneU  *  -OverdoseDamageMultiplier * ReagentMetabolismMultiplier,
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
						bodyPart.RelatedPart.TakeDamage(null, Effect.EffectPerOne  * ReagentMetabolismMultiplier , Effect.AttackType,
							Effect.DamageEffect, DamageSubOrgans: false);
					}
				}
				else
				{
					bodyPart.RelatedPart.TakeDamage(null, AttackBodyPartPerOneU * ReagentMetabolismMultiplier , AttackType,
						DamageEffect, DamageSubOrgans: false);
				}
			}
		}

	}
}
