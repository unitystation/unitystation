using Chemistry;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using Items.Implants.Organs;
using ScriptableObjects.Atmospherics;
using Systems.Atmospherics;
using UnityEngine;

namespace HealthV2
{
	public class XenomorphLungs : Lungs
	{
		[SerializeField]
		private float circulatedReagentAmount = 10;

		public SaturationComponent SaturationComponent;

		public ReagentCirculatedComponent _ReagentCirculatedComponent;

		public override void Awake()
		{
			base.Awake();
			SaturationComponent = this.GetComponentCustom<SaturationComponent>();
			_ReagentCirculatedComponent = this.GetComponentCustom<ReagentCirculatedComponent>();
		}


		protected override bool BreatheIn(GasMix breathGasMix, ReagentMix blood, float efficiency)
		{
			var baseBool = base.BreatheIn(breathGasMix, blood, efficiency);

			if (SaturationComponent.CurrentBloodSaturation < (_ReagentCirculatedComponent.bloodType.BLOOD_REAGENT_SATURATION_OKAY))
			{
				blood.Add(_ReagentCirculatedComponent.bloodType.CirculatedReagent, circulatedReagentAmount);
			}

			return baseBool;
		}
	}
}