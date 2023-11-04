using HealthV2;
using Mirror;
using NaughtyAttributes;

namespace Items.Implants.Organs
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

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
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
