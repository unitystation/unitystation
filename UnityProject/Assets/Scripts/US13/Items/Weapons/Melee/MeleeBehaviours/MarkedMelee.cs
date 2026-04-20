using System.Collections.Generic;
using System.Text;
using UnityEngine;
using US13.Core.Attributes;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.HealthV2;
using US13.Items.Weapons.ActivatableWeaponComponents.Server;
using US13.Player;
using US13.Systems.StatusesAndEffects;
using US13.Systems.StatusesAndEffects.Implementations;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.Items.Weapons.Melee
{
	/// <summary>
	/// Adding this to a weapon allows it to mark enemies and do bonus damage when they are marked.
	/// </summary>
	public class MarkedMelee : ICustomMeleeBehaviour, IExaminable
	{
		[SerializeField] private Marked statusEffect;

		[SerializeField] private float markedHitBonus;
		[SerializeField] private float backstabBonus;
		[SerializeField] private float pushForce;

		[SerializeField] private bool reqWield = false;
		[SerializeField] private bool doPush = false;


		private bool isCooldown;

		private ItemAttributesV2 attribs;
		private ActivatableWeapon activatable;
		private ChangeDamageOnActivate avChangeDamage;

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

			if (reqWield)
			{
				if (activatable.IsActive == false)
				{
					Chat.AddExamineMsgFromServer(attacker, "You need to be wielding this to attack");
					return modStats;
				}
			}

			var targetPlayerScript = target.GetComponent<PlayerScript>();
			if (targetPlayerScript != null && targetPlayerScript.StatusEffectManager.HasStatus(statusEffect))
			{
				var damageTotal = markedHitBonus + stats.Damage;

				var targetDir = targetPlayerScript.PlayerDirectional.CurrentDirection;
				var attackerDir = attacker.GetComponent<PlayerScript>().PlayerDirectional.CurrentDirection;

				//Backstabbing
				if (targetDir.Equals(attackerDir))
				{
					damageTotal += backstabBonus;
				}

				modStats.Damage = damageTotal;
				targetPlayerScript.StatusEffectManager.RemoveStatus(statusEffect);
				return modStats;
			}

			return modStats;
		}

		public void OnHitBehaviour(GameObject attacker, GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats)
		{
			if (doPush == false) return;

			Vector2 dir = (target.transform.position - attacker.transform.position).normalized;
			var objPhys = target.GetComponent<UniversalObjectPhysics>();
			if (objPhys != null) objPhys.NewtonianPush(dir, pushForce, 1, 0);
		}
		public void OnBlockBehaviour(GameObject attacker, GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats) { }

		public string Examine(Vector3 worldPos = default)
		{
			var baseDamage = reqWield ? avChangeDamage.ActivatedHitDamage : attribs.ServerHitDamage;
			StringBuilder exam = new StringBuilder();
			exam.AppendLine($"Mark a creature with a destabilizing force using the projectile, then hit them with melee to do {baseDamage + markedHitBonus}")
				.AppendLine($"Does {baseDamage + markedHitBonus + backstabBonus} damage instead if the target is backstabbed.");
			return exam.ToString();
		}
	}
}