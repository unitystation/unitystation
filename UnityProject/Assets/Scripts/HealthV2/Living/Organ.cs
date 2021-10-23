using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public class Organ : MonoBehaviour
	{
		protected BodyPart bodyPart;
		[NonSerialized]
		public BodyPart RelatedPart;
		public virtual void ImplantPeriodicUpdate(){}
		public virtual void RemovedFromBody(LivingHealthMasterBase livingHealth){}
		public virtual void HealthMasterSet(){}
		public virtual void SetUpSystems(){}

		public virtual void Initialisation(){}
		public virtual void BloodWasPumped(){}
		public virtual void InternalDamageLogic()
		{
			RelatedPart.InternalBleedingLogic();
		}

		private void Awake()
		{
			bodyPart = GetComponent<BodyPart>();
		}
	}

}
