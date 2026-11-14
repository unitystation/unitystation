using System;
using System.Linq;
using SecureStuff;
using UnityEngine;
using Util;

namespace US13.HealthV2.Living.PolymorphicSystems.Bodypart
{
	public interface IBodyPartComponentBase
	{
		public void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null);
		public void OnAddedToBody(LivingHealthMasterBase livingHealth);
		public bool HasSystem(LivingHealthMasterBase livingHealth);
		public void BindSystem(LivingHealthMasterBase livingHealth);

		public void SetSystem(HealthSystemBase healthSystemBase, bool removing);
	}

	[System.Serializable]
	public abstract class BodyPartComponentBase<T> : BodyPartBehavior, IBodyPartComponentBase  where T : HealthSystemBase, new()
	{
		[NonSerialized]
		public T AssociatedSystem;

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			AssociatedSystem = null;
		}

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			BindSystem(livingHealth);
		}

		// Binding must not notify systems before the other providers have initialized.
		public void BindSystem(LivingHealthMasterBase livingHealth)
		{
			AssociatedSystem = Enumerable.OfType<T>(livingHealth.ActiveSystems).FirstOrDefault();
			if (AssociatedSystem == null)
			{
				var sys = GenSystem();
				sys.Base = livingHealth;
				sys.InIt();
				livingHealth.ActiveSystems.Add(sys);
				SetSystem(sys, false);
			}
		}

		public bool HasSystem(LivingHealthMasterBase livingHealth)
		{
			return SweetExtensions.OfType<T>(livingHealth.ActiveSystems).Any();
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
