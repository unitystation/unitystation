using System;
using System.Collections;
using Mirror;
using UnityEngine;
using AddressableReferences;
using System.Threading.Tasks;
using HealthV2;
using Logs;

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
		protected override void ActOnLivingV2(Vector3 dir, LivingHealthMasterBase livingHealth)
		{
			var ctc = livingHealth.connectionToClient;
			var rtt = livingHealth.RTT;
			var pos = livingHealth.RegisterTile.WorldPositionServer;
			_ = AttackFleshRoutine(dir, null, livingHealth, pos, ctc, rtt);
		}

		//We need to slow the attack down because clients are behind server
		private async Task AttackFleshRoutine(Vector2 dir, LivingHealthBehaviour targetHealth, LivingHealthMasterBase livingHealth,
			Vector3 worldPos, NetworkConnection ctc, float rtt)
		{
			if (targetHealth == null && livingHealth == null) return;
			if (ctc == null) return;

			ServerDoLerpAnimation(dir);

			if (PlayerManager.LocalPlayerScript != null
				&& PlayerManager.LocalPlayerScript.playerHealth != null
				&& PlayerManager.LocalPlayerScript.playerHealth == livingHealth ||
				rtt < 0.02f)
			{
				//Wait until the end of the frame
				await Task.Delay(1);
			}
			else
			{
				//Wait until RTT/2 seconds?
				Loggy.Log($"WAIT FOR ATTACK: {rtt / 2f}", Category.Mobs);
				await Task.Delay((int)(rtt * 500));
			}

			if (Vector3.Distance(mobTile.WorldPositionServer, worldPos) < 1.5f)
			{
				var bodyPartTarget = defaultTarget.Randomize();
				if(targetHealth != null)
				{
					targetHealth.ApplyDamageToBodyPart(gameObject, hitDamage, AttackType.Melee, DamageType.Brute,
						bodyPartTarget);
					Chat.AddAttackMsgToChat(gameObject, targetHealth.gameObject, bodyPartTarget, null, attackVerb);
				}
				else
				{
					livingHealth.ApplyDamageToBodyPart(gameObject, hitDamage, AttackType.Melee, DamageType.Brute,
						bodyPartTarget);
					Chat.AddAttackMsgToChat(gameObject, livingHealth.gameObject, bodyPartTarget, null, attackVerb);
				}
				SoundManager.PlayNetworkedAtPos(attackSound, mobTile.WorldPositionServer, sourceObj: gameObject);
			}
		}

		protected override void ActOnTile(Vector3Int worldPos, Vector3 dir)
		{
			var matrix = MatrixManager.AtPoint(worldPos, true);

			var pos = (worldPos - mobTile.WorldPositionServer).sqrMagnitude;

			//Greater than 4 means more than one tile away
			if (pos > 4)
			{
				//Attack adjacent tiles instead
				var normalised = (worldPos - mobTile.WorldPositionServer).Normalize();
				var posShift = mobTile.WorldPositionServer;
				var xSuccess = false;

				//x must be either -1, or 1 for us to attack it
				if (normalised.x != 0)
				{
					posShift.x += normalised.x;

					//Check for the tiles on the x tile
					xSuccess = CheckTile(true);

					//Check for impassable objects to hit on the x tile
					if(TryAttackObjects(posShift, dir)) return;
				}

				//x must have failed and y must be either -1, or 1 for us to attack it
				if (xSuccess == false && normalised.y != 0)
				{
					//Remove x change and then add y change
					posShift.x -= normalised.x;
					posShift.y += normalised.y;

					//Check for impassable objects to hit first before tile
					if(TryAttackObjects(posShift, new Vector3(0, normalised.y, 0))) return;

					if (CheckTile() == false)
					{
						//Else nothing to attack, x and y failed so stop
						return;
					}
				}

				bool CheckTile(bool isX = false)
				{
					dir = new Vector3(isX ? normalised.x : 0, isX ? 0 : normalised.y , 0);

					if (MatrixManager.IsWindowAt(posShift, true) || MatrixManager.IsGrillAt(posShift, true))
					{
						worldPos = posShift;
						return true;
					}

					return false;
				}
			}
			else
			{
				//This is the target tile so check for impassable objects to hit before attacking tile
				if(TryAttackObjects(worldPos, dir)) return;
			}

			matrix.MetaTileMap.ApplyDamage(MatrixManager.WorldToLocalInt(worldPos, matrix), hitDamage * 2, worldPos);
			ServerDoLerpAnimation(dir);
		}

		protected bool TryAttackObjects(Vector3Int worldPos, Vector3 dir)
		{
			var objects = MatrixManager.GetAt<RegisterObject>(worldPos, true);
			foreach (var objectOnTile in objects)
			{
				if(objectOnTile.IsPassable(true, gameObject)) continue;
				if(objectOnTile.TryGetComponent<Integrity>(out var objectIntegrity) == false || objectIntegrity.Resistances.Indestructable) continue;

				objectIntegrity.ApplyDamage(hitDamage, AttackType.Melee, DamageType.Brute);
				ServerDoLerpAnimation(dir);
				return true;
			}

			return false;
		}
	}
}
