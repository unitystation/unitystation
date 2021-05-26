using System;
using System.Collections;
using Mirror;
using UnityEngine;
using AddressableReferences;
using System.Threading.Tasks;
using HealthV2;

namespace Systems.MobAIs
{
	/// <summary>
	/// Derives from MobMeleeAction, with the specific action of attacking
	/// </summary>
	public class MobMeleeAttack : MobMeleeAction
	{
		[SerializeField] private AddressableAudioSource attackSound = null;
		[SerializeField] protected int hitDamage = 30;
		[SerializeField] protected string attackVerb;
		protected BodyPartType defaultTarget;

		protected override void ActOnLiving(Vector3 dir, LivingHealthBehaviour healthBehaviour)
		{
			var ctc = healthBehaviour.connectionToClient;
			var rtt = healthBehaviour.RTT;
			var pos = healthBehaviour.GetComponent<RegisterTile>().WorldPositionServer;
			_ = AttackFleshRoutine(dir, healthBehaviour, null, pos, ctc, rtt);
		}
		protected override void ActOnLivingV2(Vector3 dir, LivingHealthMasterBase healthBehaviour)
		{
			var ctc = healthBehaviour.connectionToClient;
			var rtt = healthBehaviour.RTT;
			var pos = healthBehaviour.RegisterTile.WorldPositionServer;
			_ = AttackFleshRoutine(dir, null, healthBehaviour, pos, ctc, rtt);
		}

		//We need to slow the attack down because clients are behind server
		private async Task AttackFleshRoutine(Vector2 dir, LivingHealthBehaviour targetHealth, LivingHealthMasterBase targetHealthV2, 
			Vector3 worldPos, NetworkConnection ctc, float rtt)
		{
			if (targetHealth == null && targetHealthV2 == null) return;
			if (ctc == null) return;

			ServerDoLerpAnimation(dir);

			if (PlayerManager.LocalPlayerScript != null
				&& PlayerManager.LocalPlayerScript.playerHealth != null
				&& PlayerManager.LocalPlayerScript.playerHealth == targetHealthV2 ||
				rtt < 0.02f)
			{
				//Wait until the end of the frame
				await Task.Delay(1);
			}
			else
			{
				//Wait until RTT/2 seconds?
				Logger.Log($"WAIT FOR ATTACK: {rtt / 2f}", Category.Mobs);
				await Task.Delay((int)(rtt * 500));
			}

			if (Vector3.Distance(OriginTile.WorldPositionServer, worldPos) < 1.5f)
			{
				if(targetHealth != null)
				{
					targetHealth.ApplyDamageToBodyPart(gameObject, hitDamage, AttackType.Melee, DamageType.Brute,
						defaultTarget.Randomize());
					Chat.AddAttackMsgToChat(gameObject, targetHealth.gameObject, defaultTarget, null, attackVerb);
				}
				else
				{
					targetHealthV2.ApplyDamageToBodyPart(gameObject, hitDamage, AttackType.Melee, DamageType.Brute,
						defaultTarget.Randomize());
					Chat.AddAttackMsgToChat(gameObject, targetHealthV2.gameObject, defaultTarget, null, attackVerb);
				}
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
