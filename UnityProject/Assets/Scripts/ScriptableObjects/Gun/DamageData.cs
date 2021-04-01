using UnityEngine;

namespace ScriptableObjects.Gun
{
	/// <summary>
	/// This scriptable object allows developers to change layers right at run time in play mode
	/// Damage Data can be created for different behaviours
	/// So the same bullet prefab can have different damage for tiles, creatures, objects
	/// </summary>
	[CreateAssetMenu(fileName = "DamageData", menuName = "ScriptableObjects/Gun/DamageData", order = 0)]
	public class DamageData : ScriptableObject
	{
		[Range(0, 100)]
		[SerializeField] private float damage = 15;

		public float Damage => damage;

		[SerializeField] private DamageType damageType = DamageType.Brute;
		public DamageType DamageType => damageType;

		[SerializeField] private AttackType attackType = AttackType.Bullet;
		public AttackType AttackType => attackType;

		[Tooltip("How well this breaks through different types of armor.")]
		[SerializeField]
		[Range(0, 100)]
		private float armorPenetration;

		public float ArmorPenetration => armorPenetration;

		public void SetDamage(float Damage)
		{
			damage = Damage;
		}

		public void SetDamageType(DamageType DamageType)
		{
			damageType = DamageType;
		}

		public void SetAttackType(AttackType AttackType)
		{
			attackType = AttackType;
		}
	}
}