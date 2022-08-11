using UnityEngine;

namespace Systems.Research
{
	public class DamageEffectBase : ArtifactEffect
	{
		//If a damage effect creates an explosion, it will damage the artifact, triggering itself again and again etc...
		//Artifacts that have an effect that creates and explosion are immune to explosives as a result.
		[Tooltip("Will this effect cause an explosion near the artifact?")]
		public bool isExplosive = false;

		public virtual void DoEffect(DamageInfo damageInfo, UniversalObjectPhysics objectPhysics)
		{
		}
	}
}
