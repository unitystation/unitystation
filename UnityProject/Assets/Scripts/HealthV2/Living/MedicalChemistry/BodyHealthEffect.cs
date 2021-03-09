using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;
[CreateAssetMenu(fileName = "BodyHealthEffect",
 menuName = "ScriptableObjects/Chemistry/Effect/Body/BodyHealthEffect")]
public class BodyHealthEffect : BodyEffect
{
	public DamageType DamageEffect;

	public float EffectPerOne = 0;
	public override void Apply(BodyPart bodyPart, float amount)
	{
		//Logger.Log("bodyPart > " + bodyPart + " amount >  " + amount) ;
		bodyPart.AffectDamage(EffectPerOne * amount, (int) DamageEffect);
	}
}
