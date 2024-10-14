using System;
using System.Text;
using UnityEngine;
using Items;
using Systems.StatusesAndEffects.Implementations;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;
using Weapons.ActivatableWeapons;

namespace Weapons
{
	/// <summary>
	/// Adding this to a weapon allows it to mark enemies and do bonus damage when they are marked.
	/// </summary>
	public class MarkedMelee : MonoBehaviour, ICustomMeleeBehaviour, IExaminable
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

		void Awake()
		{
			attribs = gameObject.GetComponent<ItemAttributesV2>();
			activatable = gameObject.GetComponent<ActivatableWeapon>();
			avChangeDamage = gameObject.GetComponent<ChangeDamageOnActivate>();
			attribs.OnMelee += OnHit;
		}

		private void OnDestroy()
		{
			attribs.OnMelee -= OnHit;
		}

		private void OnHit(GameObject attacker, GameObject target)
		{
			if (doPush)
			{
				Vector2 dir = (target.transform.position - attacker.transform.position).normalized;
				var objPhys = target.GetComponent<UniversalObjectPhysics>();

				if (objPhys != null)
				{
					objPhys.NewtonianPush(dir, pushForce, 1, 0);
				}
			}

			var targetPlayerScript = target.GetComponent<PlayerScript>();
			if (targetPlayerScript != null)
			{
				var mark = Instantiate(statusEffect);
				targetPlayerScript.StatusEffectManager.RemoveStatus(mark);
			}
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
			var mark = Instantiate(statusEffect);
			if (targetPlayerScript != null && targetPlayerScript.StatusEffectManager.HasStatus(mark))
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
				return modStats;
			}

			return modStats;
		}

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