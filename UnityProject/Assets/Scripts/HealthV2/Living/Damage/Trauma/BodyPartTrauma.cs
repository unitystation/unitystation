using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public class BodyPartTrauma : MonoBehaviour
	{
		[SerializeField] private List<TraumaLogic> traumaTypesOnBodyPart;
		[SerializeField] private BodyPart bodyPart;

		private void Start()
		{
			if (bodyPart != null)
			{
				bodyPart.OnDamageTaken += OnDamageTaken;
				return;
			}
			Logger.LogWarning($"No bodyPart found on {gameObject.name}. Looking for one automatically.");
			bodyPart = GetComponentInParent<BodyPart>();
			if (bodyPart == null)
			{
				Logger.LogError($"No component found on parent. Make sure to put this component on a child of the bodyPart");
				return;
			}

			bodyPart.OnDamageTaken += OnDamageTaken;
		}

		private void OnDestroy()
		{
			bodyPart.OnDamageTaken -= OnDamageTaken;
		}

		private void OnDamageTaken(AttackType attackType, DamageType damageType, float damage, TraumaticDamageTypes traumaticTypes)
		{
			foreach (var logic in traumaTypesOnBodyPart)
			{
				if (traumaticTypes.HasFlag(logic.traumaTypes)) logic.OnTakeDamage(damage, damageType, attackType);
			}
		}
	}
}