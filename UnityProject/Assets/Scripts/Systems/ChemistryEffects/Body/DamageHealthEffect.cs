using System.Collections.Generic;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using Items;
using UnityEngine;

namespace Chemistry
{
	[CreateAssetMenu(fileName = "newDamageHealthEffect", menuName = "ScriptableObjects/Chemistry/DamageHealthEffect")]
	public class DamageHealthEffect : Chemistry.Effect
	{
		[System.Serializable]
		private struct DamageToDeal
		{
			public DamageType damageType;
			public float damageAmount;
		}
		[SerializeField]
		private List<DamageToDeal> damageToDeal = new List<DamageToDeal>();

		[SerializeField] private float DamageChancePercent = 1;

		public override void Apply(MonoBehaviour sender, float amount)
		{
			if (DMMath.Prob(DamageChancePercent) == false) return;

			var metabolismComponent = sender as MetabolismComponent;
			if (metabolismComponent == false) return;

			foreach (var damage in damageToDeal)
			{
				metabolismComponent.RelatedPart.TakeDamage(metabolismComponent.gameObject, damage.damageAmount, AttackType.Bio, damage.damageType);
			}
		}
	}
}
