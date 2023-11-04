using System.Linq;
using Chemistry;
using UnityEngine;
using UnityEngine.Serialization;

namespace HealthV2.Living.PolymorphicSystems.Bodypart
{
	public class ReagentCirculatedComponent : BodyPartComponentBase<ReagentPoolSystem>
	{
		[Tooltip(" doesn't contribute to the volume of the blood pool, Just used for How much bleeding happens That type of thing")]
		public float Throughput;

		public BloodType bloodType => bloodReagent as BloodType;

		/// <summary>
		/// The reagent that is used by this body part, eg oxygen.
		/// </summary>

		[Tooltip("What type of blood does this body part work with?"), FormerlySerializedAs("bloodType")]
		public Reagent bloodReagent = null;

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			base.OnAddedToBody(livingHealth);
			if (bloodReagent == null)
			{
				bloodReagent = AssociatedSystem.bloodReagent;
			}
		}


	}
}
