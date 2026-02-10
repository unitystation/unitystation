using UnityEngine;
using US13.HealthV2;

namespace US13.Items.Weapons.Melee
{
	/// <summary>
	/// Randomizes damage whenever a swing occurs.
	/// </summary>
	public class ExtradimBlade : MonoBehaviour, ICustomMeleeBehaviour
	{
		[SerializeField]
		private int minDamage = 1;

		[SerializeField]
		private int maxDamage = 30;

		private static System.Random rnd = new System.Random();

		public WeaponNetworkActions.MeleeStats CustomMeleeBehaviour(GameObject attacker, GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats)
		{
			var modStats = stats;
			modStats.Damage = rnd.Next(minDamage, maxDamage);
			return modStats;
		}
	}
}