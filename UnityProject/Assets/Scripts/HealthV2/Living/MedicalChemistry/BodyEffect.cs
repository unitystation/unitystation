using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;


// [CreateAssetMenu(fileName = "BodyHealDamageEffect",
// menuName = "ScriptableObjects/Chemistry/Effect/Body/BodyHealDamageEffect")]
[Serializable]
public class BodyEffect : Systems.Chemistry.Effect
{
	public override void Apply(MonoBehaviour sender, float amount)
	{
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