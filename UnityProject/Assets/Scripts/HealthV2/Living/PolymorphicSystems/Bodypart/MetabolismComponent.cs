using UnityEngine;

namespace HealthV2.Living.PolymorphicSystems.Bodypart
{
	public class MetabolismComponent : BodyPartComponentBase<ChemicalMetabolismSystem>
	{
		[Tooltip("How many metabolic reactions can happen inside of this body part Per tick per 1u of blood flow ")]
		public float ReagentMetabolism = 0.2f;

		[HideInInspector]
		public ReagentCirculatedComponent reagentCirculatedComponent;

		[HideInInspector]
		public SaturationComponent saturationComponent;

		public float CurrentBloodSaturation
		{
			get
			{
				if (saturationComponent != null)
				{
					return saturationComponent.currentBloodSaturation;
				}
				else
				{
					return 1;
				}
			}
		}


		public float BloodThroughput
		{
			get
			{
				if (reagentCirculatedComponent != null)
				{
					return reagentCirculatedComponent.Throughput;
				}
				else
				{
					return 1;
				}
			}
		}

		public override void Awake()
		{
			base.Awake();
			reagentCirculatedComponent = GetComponent<ReagentCirculatedComponent>();
			saturationComponent = GetComponent<SaturationComponent>();
		}
	}
}
