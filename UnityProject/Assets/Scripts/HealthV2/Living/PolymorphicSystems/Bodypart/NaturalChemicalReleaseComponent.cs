using System.Linq;
using UnityEngine;

namespace HealthV2.Living.PolymorphicSystems.Bodypart
{
	public class NaturalChemicalReleaseComponent : BodyPartComponentBase
	{


		//public Chemistry.Reagent NaturalToxinReagent;


		//public float ToxinGeneration = 0.0002f;
		[Tooltip("How much natural toxicity does this body part generate Per tick per 1u of blood flow ")]


		/// <summary>
		/// The Natural toxins that the body part makes ( give these to the liver to filter ) E.G Toxin
		/// </summary>
		[field: SerializeField] public float ToxinGeneration {get; private set; } = 0.0002f;

		[Tooltip("What reagent does this expel as waste?")]
		[field: SerializeField] public Chemistry.Reagent NaturalToxinReagent  {get; private set; }

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

		public override bool HasSystem(LivingHealthMasterBase livingHealth)
		{
			return livingHealth.ActiveSystems.OfType<NaturalChemicalReleaseSystem>().Any();
		}

		public override HealthSystemBase GenSystem(LivingHealthMasterBase livingHealth)
		{
			return new NaturalChemicalReleaseSystem();
		}
	}
}
