using System.Linq;
using UnityEngine;

namespace HealthV2.Living.PolymorphicSystems.Bodypart
{
	public class ReagentCirculatedComponent : BodyPartComponentBase<ReagentPoolSystem>
	{
		[Tooltip(" doesn't contribute to the volume of the blood pool, Just used for How much bleeding happens That type of thing")]
		public float Throughput;

		/// <summary>
		/// The reagent that is used by this body part, eg oxygen.
		/// </summary>
		[Tooltip("What type of blood does this body part work with?")]
		public BloodType bloodType = null;



		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			base.OnAddedToBody(livingHealth);
			if (bloodType == null)
			{
				bloodType = AssociatedSystem.bloodType;
			}
		}


	}
}
