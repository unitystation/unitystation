using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using UnityEngine;


// [CreateAssetMenu(fileName = "BodyHealDamageEffect",
// menuName = "ScriptableObjects/Chemistry/Effect/Body/BodyHealDamageEffect")]
[Serializable]
public class BodyEffect : Chemistry.Effect
{
	public override void Apply(MonoBehaviour sender, ReagentMix ReagentMix, Vector3 WorldPosition , float amount)
	{
		if (sender == null) return;

		var BodyPart = sender.GetComponent<BodyPart>();

		if (BodyPart != null)
		{
			Apply(BodyPart, amount);
		}
	}

	public virtual void Apply(BodyPart bodyPart, float amount)
	{

	}

}