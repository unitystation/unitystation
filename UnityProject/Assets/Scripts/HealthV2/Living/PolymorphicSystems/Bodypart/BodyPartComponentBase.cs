using System;
using System.Linq;

namespace HealthV2.Living.PolymorphicSystems.Bodypart
{
	public abstract class BodyPartComponentBase : BodyPartFunctionality
	{
		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			foreach (var sys in livingHealth.ActiveSystems)
			{
				sys.BodyPartRemoved(RelatedPart);
			}
		}

		public override void AddedToBody(LivingHealthMasterBase livingHealth)
		{
			if(HasSystem(livingHealth) == false)
			{
				var sys = GenSystem(livingHealth);
				sys.Base = livingHealth;
				sys.InIt();
				livingHealth.ActiveSystems.Add(sys);
			}

			foreach (var sys in livingHealth.ActiveSystems)
			{
				sys.BodyPartAdded(RelatedPart);
			}
		} //Warning only add body parts do not remove body parts in this

		public abstract bool HasSystem(LivingHealthMasterBase livingHealth);

		public abstract HealthSystemBase GenSystem(LivingHealthMasterBase livingHealth);
	}
}
