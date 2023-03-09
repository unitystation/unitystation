using System.Linq;
using UnityEngine;

namespace HealthV2.Living.PolymorphicSystems.Bodypart
{
	public class ReagentCirculatedComponent : BodyPartComponentBase
	{
		[Tooltip(" doesn't contribute to the volume of the blood pool, Just used for How much bleeding happens That type of thing")]
		public float Throughput;

		public override HealthSystemBase GenSystem(LivingHealthMasterBase livingHealth)
		{
			return new ReagentPoolSystem();
		}

		public override bool HasSystem(LivingHealthMasterBase livingHealth)
		{
			return livingHealth.ActiveSystems.OfType<ReagentPoolSystem>().Any();
		}
	}
}
