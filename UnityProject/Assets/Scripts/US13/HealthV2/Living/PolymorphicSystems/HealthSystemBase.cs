using System;
using SecureStuff;
using UnityEngine;
using US13.HealthV2.Living.BodyParts;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;

namespace US13.HealthV2.Living.PolymorphicSystems
{
	[Serializable]
	public abstract class HealthSystemBase : IAllowedReflection
	{

		//TODO Out of order adding systems  or Ensure fixed order?


		[HideInInspector] public LivingHealthMasterBase Base;

		/// <summary>
		/// Used for initialising when a system wakes up
		/// </summary>
		public virtual void InIt()
		{

		}

		/// <summary>
		/// Used for when the players Spawns in for the first time, When you want to add initial contents
		/// </summary>
		public virtual void StartFresh()
		{

		}

		public void InternalBodyPartAdded(BodyPart bodyPart, IBodyPartComponentBase BodyPartComponentBase)
		{
			BodyPartComponentBase.SetSystem(this, false);
		}


		public virtual void BodyPartAdded(BodyPart bodyPart)
		{

		}

		public void InternalBodyPartRemoved(BodyPart bodyPart, IBodyPartComponentBase BodyPartComponentBase)
		{
			BodyPartComponentBase.SetSystem(this, true);
			BodyPartRemoved(bodyPart);
		}

		public virtual void BodyPartRemoved(BodyPart bodyPart)
		{
		}

		public virtual void SystemUpdate()
		{

		}

		/// <summary>
		/// The method that is used to grab the configuration of the system from the HealthSO or similar data source points.
		/// Make sure to properly set this up in inherited classes, or all your stuff will always use the default/null values.
		/// </summary>
		/// <returns>HealthSystemBase</returns>
		public abstract HealthSystemBase CloneThisSystem();
	}
}

