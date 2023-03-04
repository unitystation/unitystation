using System;
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
			health.OnBodyPartAdded += OnBodyPartAdded;
			health.OnBodyPartRemoved += OnBodyPartRemoved;
		}


		/// <summary>
		/// Assigns all bodyParts that have trauma to the traumas list. Must only be called after all body parts have been
		/// initialized and spawned from the LivingHealthMasterBase class. Otherwise you'll have missing entries.
		/// </summary>
		public void SetupAfterInitialization()
		{
			Traumas.Clear();
			foreach (var bodyPart in health.BodyPartList)
			{
				var hasTrauma = bodyPart.GetComponentInChildren<BodyPartTrauma>();
				if (hasTrauma == null || Traumas.ContainsValue(hasTrauma)) continue;
				Traumas.Add(bodyPart, hasTrauma);
			}
		}

		public void HealBodyPartTrauma(BodyPart bodyPart, TraumaticDamageTypes traumaToHeal)
		{
			if (bodyPart == null || Traumas.ContainsKey(bodyPart) == false) return;
			Traumas[bodyPart].HealTraumaStage(traumaToHeal);
		}

		private BodyPartTrauma CheckBodyPart(BodyPart bodyPart)
		{
			return bodyPart.GetComponentInChildren<BodyPartTrauma>();
		}

		private void OnBodyPartAdded(BodyPart bodyPart)
		{
			var trauma = CheckBodyPart(bodyPart);
			if ( trauma == null ) return;
			Traumas.Add(bodyPart, trauma);
		}

		private void OnBodyPartRemoved(BodyPart bodyPart)
		{
			var trauma = CheckBodyPart(bodyPart);
			if ( trauma == null || Traumas.ContainsValue(trauma) == false ) return;
			Traumas.Remove(bodyPart);
		}
	}
}