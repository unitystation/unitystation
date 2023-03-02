using System.Collections;
using System.Collections.Generic;
using HealthV2;
using HealthV2.Living.PolymorphicSystems;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;

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
