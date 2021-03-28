using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using UnityEngine;

[CreateAssetMenu(fileName = "BodyHealthEffect",
	menuName = "ScriptableObjects/Chemistry/Effect/Body/BodyHealthEffect")]
public class BodyHealthEffect : MetabolismReaction
{
	public DamageType DamageEffect;

	public float EffectPerOne = 0;


	public float PercentageBloodOverdose;



	public override void PossibleReaction(BodyPart sender, ReagentMix reagentMix, float LimitedreactionAmount)
	{
		foreach (var Mix in reagentMix)
		{

		}

		if (reagentMix[])
		//Logger.Log("bodyPart > " + bodyPart + " amount >  " + amount) ;
		sender.AffectDamage(EffectPerOne * LimitedreactionAmount, (int) DamageEffect);
		base.PossibleReaction(sender,reagentMix, LimitedreactionAmount );
	}
}