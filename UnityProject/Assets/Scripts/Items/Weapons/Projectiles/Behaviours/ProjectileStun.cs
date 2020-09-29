﻿using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public class ProjectileStun : MonoBehaviour, IOnShoot, IOnHit
	{
		private GameObject shooter;
		private Gun weapon;
		private BodyPartType targetZone;

		[Tooltip("How long the player hit by this will be stunned")]
		[SerializeField] private float stunTime = 4.0f;

		[Tooltip("Will this stun disarm.")]
		[SerializeField] private bool willDisarm = true;

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			this.shooter = shooter;
			this.weapon = weapon;
			this.targetZone = targetZone;
		}

		public bool OnHit(RaycastHit2D hit)
		{
			return TryStun(hit);
		}

		private bool TryStun(RaycastHit2D hit)
		{
			var coll = hit.collider;
			var player = coll.GetComponent<RegisterPlayer>();
			if (player == null) return false;

			player.ServerStun(stunTime, willDisarm);

			Chat.AddAttackMsgToChat(shooter, coll.gameObject, targetZone, weapon.gameObject);
			Logger.LogTraceFormat($"{shooter} stunned {player.gameObject.name} for {stunTime} seconds with {weapon.name}", Category.Firearms);

			return true;
		}

		private void OnDisable()
		{
			shooter = null;
			weapon = null;
		}
	}
}