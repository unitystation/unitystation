using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using US13.Core.Attributes;
using US13.HealthV2.Living.BodyParts;

namespace US13.HealthV2.Living
{
	public sealed class BodyPartBehaviors : NetworkBehaviour
	{
		[SerializeReference, SelectImplementation(typeof(IBodyPartBehavior))]
		public List<IBodyPartBehavior> Behaviors = new();

		private void Awake()
		{

		}

		private void BehaviorsTick()
		{
			foreach (IBodyPartBehavior behavior in Behaviors)
			{
				behavior.ImplantPeriodicUpdate();
			}
		}

		public void OnBodyPartDamage(BodyPartDamageData data)
		{
			foreach (IBodyPartBehavior behavior in Behaviors)
			{
				behavior.OnTakeDamage(data);
			}
		}

		public void OnBodyPartAdded(LivingHealthMasterBase livingHealth)
		{
			foreach (IBodyPartBehavior behavior in Behaviors)
			{
				behavior.OnAddedToBody(livingHealth);
				behavior.SetUpSystems();
			}
		}

		public void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			foreach (IBodyPartBehavior behavior in Behaviors)
			{
				behavior.OnRemovedFromBody(livingHealth, source);
			}
		}

		public void DoInternalDamage()
		{
			foreach (IBodyPartBehavior behavior in Behaviors)
			{
				behavior.InternalDamageLogic();
			}
		}
	}
}