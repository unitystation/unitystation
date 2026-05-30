using System.Collections.Generic;
using UnityEngine;
using US13.Core.Attributes;
using US13.HealthV2;

namespace US13.Items.Weapons.Melee
{
	/// <summary>
	/// Randomizes damage whenever a swing occurs.
	/// </summary>
	public class RandomizeMeleeDamage : MonoBehaviour, ICustomMeleeBehaviour
	{
		[SerializeField]
		private int minDamage = 1;

		[SerializeField]
		private int maxDamage = 30;

		private static System.Random rnd = new System.Random();

		[SerializeReference, SelectImplementation(typeof(IHitRequirement))]
		private List<IHitRequirement> hitRequirements;

		List<IHitRequirement> ICustomMeleeBehaviour.Requirements
		{
			get => hitRequirements;
			set => hitRequirements = value;
		}
		private bool isEnabled = true;

		bool ICustomMeleeBehaviour.IsEnabled
		{
			get => isEnabled;
			set => isEnabled = value;
		}

		public WeaponNetworkActions.MeleeStats CustomMeleeBehaviour(GameObject attacker, GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats)
		{
			var modStats = stats;
			// +1 so the actual damage cap matches the inspector setting since rnd.Next is exclusive of its max integer value
			modStats.Damage = rnd.Next(minDamage, maxDamage + 1);
			return modStats;
		}

		public void OnHitBehaviour(GameObject attacker, GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats) { }
		public void OnBlockBehaviour(GameObject attacker, GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats) { }
	}
}