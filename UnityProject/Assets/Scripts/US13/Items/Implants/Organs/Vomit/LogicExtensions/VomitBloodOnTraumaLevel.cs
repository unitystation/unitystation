using UnityEngine;
using US13.Core.Factories;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.HealthV2.Living.Damage.Trauma;
using US13.Managers;
using Util;

namespace US13.Items.Implants.Organs.Vomit.LogicExtensions
{
	public class VomitBloodOnTraumaLevel : MonoBehaviour, IVomitExtension
	{

		[SerializeField] private TraumaticDamageTypes traumaTypes = TraumaticDamageTypes.PIERCE;
		[SerializeField] private Vector2 minMaxToTakeFromBlood = new Vector2(2, 12);
		[SerializeField] private int traumaLevelToVomitBloodOn = 2;

		public void OnVomit(float amount, LivingHealthMasterBase health, Stomach stomach)
		{
			if (stomach.RelatedPart.TryGetComponent<BodyPartTrauma>(out var trauma) == false) return;
			foreach (var traumaLogic in trauma.TraumaTypesOnBodyPart)
			{
				if (traumaLogic.traumaTypes.HasFlag(traumaTypes) == false) continue;
				if (traumaLogic.CurrentStage < traumaLevelToVomitBloodOn) continue;
				var bloodReagent = health.reagentPoolSystem.BloodPool.Take(Random.Range(minMaxToTakeFromBlood.x, minMaxToTakeFromBlood.y));
				EffectsFactory.BloodSplat(health.gameObject.AssumedWorldPosServer(),
					bloodReagent, bloodReagent.Total > 6 ? BloodSplatSize.medium : BloodSplatSize.small);
			}
		}
	}
}