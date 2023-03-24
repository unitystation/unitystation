using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace HealthV2.Living.PolymorphicSystems.Bodypart
{
	public class SaturationComponent : BodyPartComponentBase<ReagentSaturationSystem>
	{
		public BloodType bloodType => reagentCirculatedComponent.bloodType;

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

		public bool isNotBloodReagentConsumed = false;

		public override void Awake()
		{
			base.Awake();
			reagentCirculatedComponent = GetComponent<ReagentCirculatedComponent>();
		}

		public void SetNotIsBloodReagentConsumed(bool State)
		{
			isNotBloodReagentConsumed = State;
			AssociatedSystem.BodyPartListChange();
		}
	}
}
