using Chemistry;
using UnityEngine;
using US13.Items.Implants.Organs;
using US13.Tilemaps.Behaviours.Meta.Atmospherics.Data;

namespace US13.Items.Implants.Xenomorphs
{
	public class XenomorphLungs : Lungs
	{
		[SerializeField]
		private float circulatedReagentAmount = 10;

		public override bool BreatheIn(GasMix breathGasMix, ReagentMix blood, float efficiency, bool DoEmot = true )
		{
			var baseBool = base.BreatheIn(breathGasMix, blood, efficiency, DoEmot);

			if (SaturationComponent.CurrentBloodSaturation < (ReagentCirculatedComponent.bloodType.BLOOD_REAGENT_SATURATION_OKAY))
			{
				blood.Add(ReagentCirculatedComponent.bloodType.CirculatedReagent, circulatedReagentAmount);
			}

			return baseBool;
		}
	}
}