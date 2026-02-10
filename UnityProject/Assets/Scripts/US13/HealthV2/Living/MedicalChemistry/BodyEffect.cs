using System;
using Chemistry;
using UnityEngine;
using US13.HealthV2.Living.BodyParts;
using US13.HealthV2.Living.CirculatorySystem;


// [CreateAssetMenu(fileName = "BodyHealDamageEffect",
// menuName = "ScriptableObjects/Chemistry/Effect/Body/BodyHealDamageEffect")]
namespace US13.HealthV2.Living.MedicalChemistry
{
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
}