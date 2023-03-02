using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using HealthV2.Living.PolymorphicSystems;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;
using UnityEngine.Serialization;

public class SaturationComponent : BodyPartComponentBase
{

	/// <summary>
	/// The reagent that is used by this body part, eg oxygen.
	/// </summary>
	[Tooltip("What type of blood does this body part work with?")] [NonSerialized]
	public BloodType bloodType = null;

	/// <summary>
	/// The reagent that is used by this body part, eg oxygen.
	/// </summary>
	[Tooltip("What blood reagent does this use?")]
	public Chemistry.Reagent requiredReagent;

	/// <summary>
	/// The reagent that the body part expels as waste, eg co2
	/// </summary>
	[Tooltip("What reagent does this expel as waste?")]
	public Chemistry.Reagent wasteReagent;

	/// <summary>
	/// The amount (in moles) of required reagent (eg oxygen) this body part needs consume each tick.
	/// </summary>
	[Tooltip("What percentage per update of oxygen*(Required reagent) is consumed")]
	[SerializeField]
	public float bloodReagentConsumedPercentageb = 0.5f;

	[FormerlySerializedAs("ReagentCirculated")] [HideInInspector]
	public ReagentCirculatedComponent reagentCirculatedComponent;


	public float currentBloodSaturation = 0;

	public float CurrentBloodSaturation => currentBloodSaturation;

	public override void Awake()
	{
		base.Awake();
		reagentCirculatedComponent = GetComponent<ReagentCirculatedComponent>();
	}

	public override HealthSystemBase GenSystem(LivingHealthMasterBase livingHealth)
	{
		return new ReagentSaturationSystem();
	}

	public override bool HasSystem(LivingHealthMasterBase livingHealth)
	{
		return livingHealth.ActiveSystems.OfType<ReagentSaturationSystem>().Any();
	}
}
