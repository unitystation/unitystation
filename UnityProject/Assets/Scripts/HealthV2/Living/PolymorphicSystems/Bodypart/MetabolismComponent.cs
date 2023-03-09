using UnityEngine;

namespace HealthV2.Living.PolymorphicSystems.Bodypart
{
	public class MetabolismComponent : BodyPartComponentBase
	{
		[Tooltip("How many metabolic reactions can happen inside of this body part Per tick per 1u of blood flow ")]
		public float ReagentMetabolism = 0.2f;

		[HideInInspector]
		public ReagentCirculatedComponent reagentCirculatedComponent;

		[HideInInspector]
		public SaturationComponent saturationComponent;

		public float GetCurrentBloodSaturation
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


		public float GetThroughput
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

		public override bool HasSystem(LivingHealthMasterBase livingHealth)
		{
			return true;
		}

		public override HealthSystemBase GenSystem(LivingHealthMasterBase livingHealth)
		{
			return new ChemicalMetabolismSystem();
		}

	}
}
