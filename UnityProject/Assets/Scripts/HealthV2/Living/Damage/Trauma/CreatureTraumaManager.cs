using System.Collections.Generic;
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

		public void HealBodyPartTrauma(BodyPart bodyPart, TraumaticDamageTypes traumaToHeal)
		{
			if (bodyPart == null || Traumas.ContainsKey(bodyPart) == false) return;
			Traumas[bodyPart].HealTraumaStage(traumaToHeal);
		}

		public BodyPartTrauma CheckBodyPart(BodyPart bodyPart)
		{
			return bodyPart.GetComponentInChildren<BodyPartTrauma>();
		}
	}
}
