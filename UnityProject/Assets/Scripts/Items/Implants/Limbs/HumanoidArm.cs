using System.Collections.Generic;
using Player.Movement;
using UnityEngine;

namespace HealthV2.Limbs
{
	public class HumanoidArm : Limb
	{
		[Header("Arm Damage Stats")]
		[SerializeField] public float ArmMeleeDamage = 5f;
		[SerializeField] public DamageType ArmDamageType = DamageType.Brute;
		[SerializeField] public List<string> ArmDamageVerbs;
		[SerializeField] public TraumaticDamageTypes ArmTraumaticDamage;
		[SerializeField] public float ArmTraumaticChance = 0;
	}
}