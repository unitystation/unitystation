using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace US13.HealthV2.Living.BodyParts.Damage.Trauma
{
	public class CreatureTraumaManager : MonoBehaviour
	{
		public Dictionary<BodyPart, BodyPartTrauma> Traumas { get; private set; } = new();
		[SerializeField] private LivingHealthMasterBase health;


		private void Awake()
		{
			if (health == null) health = GetComponent<LivingHealthMasterBase>();
		}

		public bool HealBodyPartTrauma(BodyPart bodyPart, TraumaticDamageTypes traumaToHeal)
		{
			if (bodyPart == null || Traumas.ContainsKey(bodyPart) == false) return false;
			return Traumas[bodyPart].HealTraumaStage(traumaToHeal);
		}

		public bool HasAnyTrauma()
		{
			foreach (var trauma in Traumas.Values)
			{
				if (trauma.TraumaTypesOnBodyPart.Any(logic => logic.CurrentStage > 0))
				{
					return true;
				}
			}
			return false;
		}

		public bool HasAnyTraumaOfType(TraumaticDamageTypes type)
		{
			foreach (var trauma in Traumas.Values)
			{
				if (trauma.TraumaTypesOnBodyPart.Any(logic => logic.traumaTypes.HasFlag(type)))
				{
					return true;
				}
			}
			return false;
		}
	}
}
