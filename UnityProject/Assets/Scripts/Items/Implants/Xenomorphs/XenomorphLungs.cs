using Chemistry;
using Items.Implants.Organs;
using Systems.Atmospherics;
using UnityEngine;

namespace HealthV2
{
	public class XenomorphLungs : Lungs
	{
		[SerializeField]
		private float circulatedReagentAmount = 10;

		public override bool BreatheIn(GasMix breathGasMix, ReagentMix blood, float efficiency)
		{
			var baseBool = base.BreatheIn(breathGasMix, blood, efficiency);

			if (SaturationComponent.CurrentBloodSaturation < (ReagentCirculatedComponent.bloodType.BLOOD_REAGENT_SATURATION_OKAY))
			{
				blood.Add(ReagentCirculatedComponent.bloodType.CirculatedReagent, circulatedReagentAmount);
			}

			return baseBool;
		}
	}
}