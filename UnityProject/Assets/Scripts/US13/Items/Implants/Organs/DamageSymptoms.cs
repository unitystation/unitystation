using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using US13.Core.Chat;
using US13.HealthV2.Living;
using Util;

namespace US13.Items.Implants.Organs
{
	public class DamageSymptoms : BodyPartFunctionality
	{
		[SerializeField,FormerlySerializedAs("severityAndSymptoms")] private List<SeverityAndSymptoms> InitialseverityAndSymptoms = new List<SeverityAndSymptoms>();
		private List<SeverityAndSymptoms> severityAndSymptoms = new List<SeverityAndSymptoms>();
		public float TimeBetweenSymptoms;
		private float time;

		public void Awake()
		{
			severityAndSymptoms = InitialseverityAndSymptoms.OrderByDescending(x => x.HealthPercentageAndBelow).Reverse().ToList();
		}

		public override void ImplantPeriodicUpdate()
		{
			if (RelatedPart.HealthMaster.IsDead) return;
			if (time > TimeBetweenSymptoms)
			{
				var CurrentHealthPercent = Mathf.Max((float)RelatedPart.Health, 0) / RelatedPart.MaxHealth;
				foreach (var severity in severityAndSymptoms)
				{
					if (severity.HealthPercentageAndBelow > CurrentHealthPercent)
					{
						Chat.AddExamineMsgFromServer((GameObject)RelatedPart.HealthMaster.gameObject, severity.Affects.PickRandom());
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
		public class SeverityAndSymptoms
		{
			public float HealthPercentageAndBelow;
			//Could expand later on
			public List<string> Affects;
		}
	}
}
