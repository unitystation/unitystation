using System;
using System.Collections.Generic;
using UnityEngine;
using Systems.Explosions;

namespace HealthV2
{
	public class BodyPartFunctionality : MonoBehaviour, IEmpAble
	{
		protected BodyPart bodyPart;
		[NonSerialized]
		public BodyPart RelatedPart;

		public bool isEMPVunerable = false;

		public virtual void ImplantPeriodicUpdate(){}
		public virtual void RemovedFromBody(LivingHealthMasterBase livingHealth){}
		public virtual void AddedToBody(LivingHealthMasterBase livingHealth){}
		public virtual void SetUpSystems(){}
		public virtual void InternalDamageLogic() {}

		public virtual void OnEmp(int strength)
		{
			if (isEMPVunerable == false) return;
		}

		private void Awake()
		{
			bodyPart = GetComponent<BodyPart>();
		}

	}
}
