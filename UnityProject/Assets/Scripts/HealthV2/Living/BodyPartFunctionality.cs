using System;
using System.Collections.Generic;
using UnityEngine;
using Systems.Explosions;
using NaughtyAttributes;

namespace HealthV2
{
	public class BodyPartFunctionality : MonoBehaviour, IEmpAble
	{
		protected BodyPart bodyPart;
		[NonSerialized]
		public BodyPart RelatedPart;

		public bool isEMPVunerable = false;

		[ShowIf("isEMPVunerable")]
		public int EMPResistance = 2;

		public virtual void ImplantPeriodicUpdate(){}
		public virtual void RemovedFromBody(LivingHealthMasterBase livingHealth){}
		public virtual void AddedToBody(LivingHealthMasterBase livingHealth){}
		public virtual void SetUpSystems(){}
		public virtual void InternalDamageLogic() {}
		
		public virtual void OnEmp(int strength)
		{
			if (isEMPVunerable == false) return;

			if(EMPResistance == 0)
			{
				EmpResult();
				return;
			}

			if (DMMath.Prob(1 / EMPResistance))
			{
				EmpResult();
			}
		}

		public virtual void EmpResult()
		{
			RelatedPart.ApplyTraumaDamage(TraumaticDamageTypes.BURN);
		}

		private void Awake()
		{
			bodyPart = GetComponent<BodyPart>();
		}

	}
}
