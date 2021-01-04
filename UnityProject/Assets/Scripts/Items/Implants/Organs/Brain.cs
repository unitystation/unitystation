using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public class Brain : BodyPart
	{
		//stuff in here?
		//nah
		public override void AddedToBody(LivingHealthMasterBase livingHealthMasterBase)
		{
			livingHealthMasterBase.Setbrain(this);
		}
		//Ensure removal of brain
	}
}