using UnityEngine;

namespace HealthV2.Living.PolymorphicSystems
{
	[System.Serializable]
	public abstract class HealthSystemBase
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

		public virtual void BodyPartAdded(BodyPart bodyPart)
		{
		}

		public virtual void BodyPartRemoved(BodyPart bodyPart)
		{
		}

		public virtual void SystemUpdate()
		{

		}

		public abstract HealthSystemBase CloneThisSystem();
	}
}
