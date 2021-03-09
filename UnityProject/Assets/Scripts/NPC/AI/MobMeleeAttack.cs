using System;
using System.Collections;
using Mirror;
using UnityEngine;
using AddressableReferences;
using HealthV2;

namespace Systems.MobAIs
{
	/// <summary>
	/// Derives from MobMeleeAction, with the specific action of attacking
	/// </summary>
	public class MobMeleeAttack : MobMeleeAction
	{
		[SerializeField] private AddressableAudioSource attackSound = null;
		public int hitDamage = 30;
		public string attackVerb;
		public BodyPartType defaultTarget;

		protected override void ActOnLiving(Vector3 dir, LivingHealthMasterBase healthBehaviour)
		{
			StartCoroutine(AttackFleshRoutine(dir, healthBehaviour));
		}

		//We need to slow the attack down because clients are behind server
		IEnumerator AttackFleshRoutine(Vector2 dir, LivingHealthMasterBase healthBehaviour)
		{
			if (healthBehaviour.connectionToClient == null)
			{
				yield break;
			}

			ServerDoLerpAnimation(dir);

			Logger.Log(
				$"CONN CLIENT TIME: {healthBehaviour.connectionToClient.lastMessageTime} Network time: {(float)NetworkTime.time}",
				Category.Mobs);
			if (PlayerManager.LocalPlayerScript != null
				&& PlayerManager.LocalPlayerScript.playerHealth != null
				&& PlayerManager.LocalPlayerScript.playerHealth == healthBehaviour ||
				healthBehaviour.RTT == 0f)
			{
				yield return WaitFor.EndOfFrame;
			}
			else
			{
				Logger.Log($"WAIT FOR ATTACK: {healthBehaviour.RTT / 2f}", Category.Mobs);
				yield return WaitFor.Seconds(healthBehaviour.RTT / 2f);
			}

			if (Vector3.Distance(OriginTile.WorldPositionServer, healthBehaviour.RegisterTile.WorldPositionServer) < 1.5f)
			{
				healthBehaviour.ApplyDamageToBodypart(gameObject, hitDamage, AttackType.Melee, DamageType.Brute,
					defaultTarget.Randomize());
				Chat.AddAttackMsgToChat(gameObject, healthBehaviour.gameObject, defaultTarget, null, attackVerb);
				SoundManager.PlayNetworkedAtPos(attackSound, OriginTile.WorldPositionServer, sourceObj: gameObject);
			}
		}

		protected override void ActOnTile(Vector3Int worldPos, Vector3 dir)
		{
			var matrix = MatrixManager.AtPoint(worldPos, true);
			matrix.MetaTileMap.ApplyDamage(MatrixManager.WorldToLocalInt(worldPos, matrix), hitDamage * 2, worldPos);
			ServerDoLerpAnimation(dir);
		}
	}
}
