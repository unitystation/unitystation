using System.Collections.Generic;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;
using UnityEngine.Serialization;

namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "newDamageHealthEffect", menuName = "ScriptableObjects/Chemistry/DamageHealthEffect")]
	public class DamageHealthEffect : Effect
	{
		[System.Serializable]
		private struct DamageToDeal
		{
			public DamageType damageType;
			public float damageAmount;

			public DamageToDeal(float damageAmount = 0, DamageType damageType = DamageType.Brute)
			{
				this.damageType = damageType;
				this.damageAmount = damageAmount;
			}
		}
		[SerializeField]
		private List<DamageToDeal> damageToDeal = new List<DamageToDeal>();

		[FormerlySerializedAs("DamageChancePercent")] [SerializeField] private float damageChancePercent = 1;
		[SerializeField] private ItemTrait requiredItemTrait = null;

		public override void Apply(MonoBehaviour sender, ReagentMix ReagentMix, Vector3 WorldPosition , float amount)
		{
			if (sender == null) return;
			if (DMMath.Prob(damageChancePercent) == false) return;

			var metabolismComponent = sender as MetabolismComponent;
			if (metabolismComponent is null) return;

			if (requiredItemTrait != null && metabolismComponent.RelatedPart.ItemAttributes.HasTrait(requiredItemTrait) == false) return;

			foreach (var damage in damageToDeal)
			{
				metabolismComponent.RelatedPart.TakeDamage(metabolismComponent.gameObject, damage.damageAmount, AttackType.Bio, damage.damageType);
			}
		}
	}
}
