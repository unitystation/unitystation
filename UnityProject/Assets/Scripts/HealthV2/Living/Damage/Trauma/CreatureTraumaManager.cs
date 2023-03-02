using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public class CreatureTraumaManager : MonoBehaviour
	{
		public List<BodyPartTrauma> Traumas { get; private set; } = new List<BodyPartTrauma>();
		[SerializeField] private LivingHealthMasterBase health;


		private void Awake()
		{
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
				if (hasTrauma == null || Traumas.Contains(hasTrauma)) continue;
				Traumas.Add(hasTrauma);
			}
		}

		private BodyPartTrauma CheckBodyPart(BodyPart bodyPart)
		{
			return bodyPart.GetComponentInChildren<BodyPartTrauma>();
		}

		private void OnBodyPartAdded(BodyPart bodyPart)
		{
			var trauma = CheckBodyPart(bodyPart);
			if ( trauma == null ) return;
			Traumas.Add(trauma);
		}

		private void OnBodyPartRemoved(BodyPart bodyPart)
		{
			var trauma = CheckBodyPart(bodyPart);
			if ( trauma == null || Traumas.Contains(trauma) == false ) return;
			Traumas.Remove(trauma);
		}
	}
}