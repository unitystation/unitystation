using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public class Brain : Organ
	{
		//stuff in here?
		//nah
		public override void SetUpSystems()
		{
			base.SetUpSystems();
			RelatedPart.HealthMaster.Setbrain(this);
		}
		//Ensure removal of brain

		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			livingHealth.brain = null;

		}
	}
}