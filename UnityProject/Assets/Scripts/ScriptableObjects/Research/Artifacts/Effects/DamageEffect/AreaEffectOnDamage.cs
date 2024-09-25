using Core;
using UnityEngine;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

namespace Systems.Research
{
	[CreateAssetMenu(fileName = "AreaEffectOnDamage", menuName = "ScriptableObjects/Systems/Artifacts/AreaEffectOnDamage")]
	public class AreaEffectOnDamage : DamageEffectBase
	{
		[SerializeField]
		private AreaEffectBase AreaEffectToTrigger;

		public override void DoEffect(DamageInfo damageInfo, UniversalObjectPhysics objectPhysics) 
		{
			AreaEffectToTrigger.DoEffectAura(objectPhysics.gameObject);
		}

		public AreaEffectBase GetAreaEffect()
		{
			return AreaEffectToTrigger;
		}
	}
}
