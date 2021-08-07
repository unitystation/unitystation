using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace HealthV2
{
	public class DamageSymptoms : Organ
	{
		public List<SeverityAndSymptoms> severityAndSymptoms = new List<SeverityAndSymptoms>();
		public float TimeBetweenSymptoms;
		private float time;

		public override void Initialisation()
		{
			severityAndSymptoms = severityAndSymptoms.OrderByDescending(x => x.HealthPercentageAndBelow).Reverse().ToList();
		}

		public override void ImplantPeriodicUpdate()
		{
			if (RelatedPart.HealthMaster.IsDead) return;
			if (time > TimeBetweenSymptoms)
			{
				var CurrentHealthPercent = Mathf.Max(RelatedPart.Health, 0) / RelatedPart.MaxHealth;
				foreach (var severity in severityAndSymptoms)
				{
					if (severity.HealthPercentageAndBelow > CurrentHealthPercent)
					{
						Chat.AddExamineMsgFromServer(RelatedPart.HealthMaster.gameObject, severity.Affects.PickRandom());
						break;
					}
				}

				time = 0;
			}
			else
			{
				time += Time.deltaTime;
			}

		}

		[Serializable]
		public struct SeverityAndSymptoms
		{
			public float HealthPercentageAndBelow;
			//Could expand later on
			public List<string> Affects;
		}
	}
}
