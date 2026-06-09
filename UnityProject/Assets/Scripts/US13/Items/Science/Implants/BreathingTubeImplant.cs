using Mirror;
using NaughtyAttributes;
using UnityEngine;
using US13.HealthV2.Living;
using Util;

namespace US13.Items.Science.Implants
{
	public class BreathingTubeImplant : BodyPartFunctionality
	{
		[field: SyncVar] public bool isEMPed { get; private set; } = false;

		public bool isEMPVunerable = false;

		[ShowIf("isEMPVunerable")]
		public int EMPResistance = 2;

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			RelatedPart.HealthMaster.RespiratorySystem.AddImplant(this);
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			RelatedPart.HealthMaster.RespiratorySystem.RemoveImplant(this);
		}

		public override void OnEmp(int strength)
		{
			if (isEMPVunerable == false) return;

			if (EMPResistance == 0 || DMMath.Prob(100 / EMPResistance))
			{
				isEMPed = true;
			}
		}

	}
}
