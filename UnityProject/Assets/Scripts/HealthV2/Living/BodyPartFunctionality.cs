using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public class BodyPartFunctionality : MonoBehaviour
	{
		protected BodyPart bodyPart;
		[NonSerialized]
		public BodyPart RelatedPart;

		public virtual void ImplantPeriodicUpdate(){}
		public virtual void RemovedFromBody(LivingHealthMasterBase livingHealth){}
		public virtual void AddedToBody(LivingHealthMasterBase livingHealth){}
		public virtual void SetUpSystems(){}
		public virtual void BloodWasPumped(){}
		public virtual void InternalDamageLogic() {}

		private void Awake()
		{
			bodyPart = GetComponent<BodyPart>();
		}
	}
}
