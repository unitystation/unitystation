using System;
using System.Linq;

namespace HealthV2.Living.PolymorphicSystems.Bodypart
{
	public abstract class BodyPartComponentBase<T>  : BodyPartFunctionality where T : HealthSystemBase, new() //TODO Apparently doesn't like Generics AAAAAAAAAAAAAAA
	{
		public T AssociatedSystem;

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			foreach (var sys in livingHealth.ActiveSystems)
			{
				sys.InternalBodyPartRemoved(RelatedPart, this as BodyPartComponentBase<HealthSystemBase>);
				SetSystem(sys, true);
			}
		}

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			if(HasSystem(livingHealth) == false)
			{
				var sys = GenSystem();
				sys.Base = livingHealth;
				sys.InIt();
				livingHealth.ActiveSystems.Add(sys);
				SetSystem(sys, false);
			}

			foreach (var sys in livingHealth.ActiveSystems)
			{
				sys.InternalBodyPartAdded(RelatedPart, this as BodyPartComponentBase<HealthSystemBase>);
			}
		} //Warning only add body parts do not remove body parts in this

		public bool HasSystem(LivingHealthMasterBase livingHealth)
		{
			return livingHealth.ActiveSystems.OfType<T>().Any();
		}

		public HealthSystemBase GenSystem()
		{
			return new T();
		}

		public void SetSystem(HealthSystemBase healthSystemBase, bool removing)
		{
			if (healthSystemBase is T sys)
			{
				if (removing)
				{
					AssociatedSystem = default(T);
				}
				else
				{
					AssociatedSystem = sys;
				}
			}
		}
	}
}
