using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HealthV2
{
	public class CreatureTraumaManager : MonoBehaviour
	{
		public Dictionary<BodyPart, BodyPartTrauma> Traumas { get; private set; } = new Dictionary<BodyPart, BodyPartTrauma>();
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
