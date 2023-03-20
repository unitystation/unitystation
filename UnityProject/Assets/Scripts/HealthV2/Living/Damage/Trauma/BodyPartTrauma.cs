using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public class BodyPartTrauma : BodyPartFunctionality
	{
		[SerializeField] private List<TraumaLogic> traumaTypesOnBodyPart;
		public List<TraumaLogic> TraumaTypesOnBodyPart => traumaTypesOnBodyPart;

		private void Start()
		{
			if (RelatedPart == null)
			{
				Logger.LogError($"No component found on parent. Make sure to put this component on a child of the bodyPart");
				return;
			}

			RelatedPart.OnDamageTaken += OnTakeDamage;
		}

		private void OnDestroy()
		{
			RelatedPart.OnDamageTaken -= OnTakeDamage;
		}

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			var creatureTraumaAPI = livingHealth.GetComponent<CreatureTraumaManager>();
			if (creatureTraumaAPI == null)
			{
				Logger.LogWarning($"[BodyPartTrauma/OnAddBodyPart] - No high level trauma manager detected on creature." +
				                  $"Functionalities like trauma healing may not be available for this body part.");
				return;
			}
			creatureTraumaAPI.Traumas.Add(RelatedPart, this);
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			var creatureTraumaAPI = livingHealth.GetComponent<CreatureTraumaManager>();
			if (creatureTraumaAPI == null) return;
			creatureTraumaAPI.Traumas.Remove(RelatedPart);
		}

		public override void OnTakeDamage(BodyPartDamageData data)
		{
			foreach (var logic in traumaTypesOnBodyPart)
			{
				if (data.TramuticDamageType.HasFlag(logic.traumaTypes)) logic.OnTakeDamage(data);
			}
		}

		public bool HealTraumaStage(TraumaticDamageTypes traumaToHeal)
		{
			var healed = false;
			foreach (var logic in traumaTypesOnBodyPart)
			{
				if (traumaToHeal.HasFlag(logic.traumaTypes))
				{
					logic.HealStage();
					healed = true;
				}
			}

			return healed;
		}
	}
}