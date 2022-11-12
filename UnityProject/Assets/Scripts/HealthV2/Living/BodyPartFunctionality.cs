using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Systems.Explosions;
using NaughtyAttributes;

namespace HealthV2
{
	public class BodyPartFunctionality : NetworkBehaviour, IEmpAble
	{

		[NonSerialized]
		public BodyPart RelatedPart;

		public bool isEMPVunerable = false;

		[ShowIf("isEMPVunerable")]
		public int EMPResistance = 2;

		public virtual void ImplantPeriodicUpdate(){}
		public virtual void RemovedFromBody(LivingHealthMasterBase livingHealth){}
		public virtual void AddedToBody(LivingHealthMasterBase livingHealth){} //Warning only add body parts do not remove body parts in this
		public virtual void SetUpSystems(){}
		public virtual void InternalDamageLogic() {}

		public virtual void OnEmp(int strength)
		{
			if (isEMPVunerable == false) return;

			if (EMPResistance == 0 || DMMath.Prob(100 / EMPResistance))
			{
				EmpResult(strength);
			}
		}

		public virtual void EmpResult(int strength)
		{
			//TODO With this implementation somewhere else generic
			RelatedPart.TakeDamage(this.gameObject,(int)(5f * strength / (EMPResistance + 1)), AttackType.Internal, DamageType.Burn, false, false, 0, (int)(100/EMPResistance + 1), TraumaticDamageTypes.BURN);
		}

		public virtual void Awake()
		{
			RelatedPart = GetComponent<BodyPart>();
		}

	}
}
