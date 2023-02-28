using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chemistry;
using HealthV2;
using HealthV2.Living.PolymorphicSystems;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;
using UnityEngine.Serialization;

public class HungerComponent : BodyPartComponentBase
{
	/// <summary>
	/// Modifier that multiplicatively reduces the efficiency of the body part based on damage
	/// </summary>
	[Tooltip("Modifier to reduce efficiency when the character gets hungry")] [NonSerialized]
	public Modifier HungerModifier = new Modifier();
	//TODO add to Body part


	/// <summary>
	/// The nutriment reagent that this part consumes in order to perform tasks
	/// </summary>
	[Tooltip("What does this live off?")] [SerializeField]
	public Reagent Nutriment;


	/// <summary>
	/// The amount of of nutriment to consumed each tick as part of passive metabolism
	/// </summary>
	[NonSerialized] //Automatically generated runtime
	public float PassiveConsumptionNutriment = 0.00012f;

	/// <summary>
	/// The amount of of nutriment to consume in order to perform work, eg heal damage or replenish blood supply
	/// </summary>
	[Tooltip("How much more nutriment does it consume each Second")]
	public float HealingNutrimentMultiplier = 2f;
	// /\ Regeneration = hyper nutriment consumption healing = all body parts?

	public HungerState HungerState = HungerState.Normal;

	[FormerlySerializedAs("ReagentCirculated")] [HideInInspector]
	public ReagentCirculatedComponent reagentCirculatedComponent;

	/// <summary>
	/// Heals damage caused by sources other than lack of blood reagent
	/// </summary>
	/// <param name="amount">Amount to heal</param>
	public void NutrimentHeal(double amount)
	{
		double DamageMultiplier = RelatedPart.TotalDamageWithoutOxy / amount;

		for (int i = 0; i < RelatedPart.Damages.Length; i++)
		{
			if ((int) DamageType.Oxy == i) continue;
			RelatedPart.HealDamage(null, (float) (RelatedPart.Damages[i] / DamageMultiplier), i);
		}
	}

	public float BloodThroughput => reagentCirculatedComponent.Throughput;

	public override void Awake()
	{
		base.Awake();
		reagentCirculatedComponent = GetComponent<ReagentCirculatedComponent>();
	}

	public override bool HasSystem(LivingHealthMasterBase livingHealth)
	{
		return livingHealth.ActiveSystems.OfType<HungerSystem>().Any();
	}

	public override HealthSystemBase GenSystem(LivingHealthMasterBase livingHealth)
	{
		return new HungerSystem();
	}
}
