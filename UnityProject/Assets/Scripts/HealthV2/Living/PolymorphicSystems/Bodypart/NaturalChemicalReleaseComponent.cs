using System.Linq;
using UnityEngine;

namespace HealthV2.Living.PolymorphicSystems.Bodypart
{
	public class NaturalChemicalReleaseComponent : BodyPartComponentBase<NaturalChemicalReleaseSystem>
	{
		//public Chemistry.Reagent NaturalToxinReagent;

		//public float ToxinGeneration = 0.0002f;
		[Tooltip("How much natural toxicity does this body part generate Per tick per 1u of blood flow ")]


		/// <summary>
		/// The Natural toxins that the body part makes ( give these to the liver to filter ) E.G Toxin
		/// </summary>
		[field: SerializeField] public float ToxinGeneration {get; set; } = 0.0002f;

		[Tooltip("What reagent does this expel as waste?")]
		[field: SerializeField] public Chemistry.Reagent NaturalToxinReagent  {get; set; }

		[HideInInspector]
		public ReagentCirculatedComponent reagentCirculatedComponent;

		public float BloodThroughput
		{
			get
			{
				if (reagentCirculatedComponent == null) return 1;
				return reagentCirculatedComponent.Throughput;
			}
		}

		public override void Awake()
		{
			base.Awake();
			reagentCirculatedComponent = GetComponent<ReagentCirculatedComponent>();
		}
	}
}
