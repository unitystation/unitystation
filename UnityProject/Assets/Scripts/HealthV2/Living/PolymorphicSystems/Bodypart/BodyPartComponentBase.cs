using System;
using System.Linq;
using SecureStuff;
using UnityEngine;

namespace HealthV2.Living.PolymorphicSystems.Bodypart
{
	public interface IBodyPartComponentBase
	{
		public void OnRemovedFromBody(LivingHealthMasterBase livingHealth);
		public void OnAddedToBody(LivingHealthMasterBase livingHealth);
		public bool HasSystem(LivingHealthMasterBase livingHealth);

		public void SetSystem(HealthSystemBase healthSystemBase, bool removing);
	}

	public abstract class BodyPartComponentBase<T> : BodyPartFunctionality, IBodyPartComponentBase  where T : HealthSystemBase, new()
	{
		[NonSerialized]
		public T AssociatedSystem;

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			foreach (var sys in livingHealth.ActiveSystems)
			{
				sys.InternalBodyPartRemoved(RelatedPart, this);
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
				sys.BodyPartAdded(this.RelatedPart);
			}

			foreach (var sys in livingHealth.ActiveSystems)
			{
				sys.InternalBodyPartAdded(RelatedPart, this);
			}
		} //Warning only add body parts do not remove body parts in this

		public bool HasSystem(LivingHealthMasterBase livingHealth)
		{
			return livingHealth.ActiveSystems.OfType<T>().Any();
		}

		public HealthSystemBase GenSystem()
		{
			return AllowedReflection.CreateInstance<T>();
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
