using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public class BodyPartModification : MonoBehaviour
	{
		[NonSerialized]
		public BodyPart RelatedPart;
		public virtual void ImplantPeriodicUpdate() 
		{ 
			if(RelatedPart.IsBleedingInternally)
			{
				InternalDamageLogic();
			}
		}
		public virtual void RemovedFromBody(LivingHealthMasterBase livingHealthMasterBase){}
		public virtual void HealthMasterSet(){}
		public virtual void SetUpSystems(){}

		public virtual void Initialisation(){}
		public virtual void BloodWasPumped(){}
		public virtual void InternalDamageLogic()
		{
			RelatedPart.InternalBleedingLogic();
		}
	}

}
