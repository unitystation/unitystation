using UnityEngine;

namespace HealthV2.Living.PolymorphicSystems
{
	[System.Serializable]
	public abstract class HealthSystemBase
	{

		//TODO Out of order adding systems  or Ensure fixed order?


		[HideInInspector] public LivingHealthMasterBase Base;

		public virtual void InIt()
		{

		}

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
