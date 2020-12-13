using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;
using AddressableReferences;

namespace Systems.MobAIs
{
	/// <summary>
	/// Basic AI behaviour for following and melee attacking a target
	/// </summary>
	// [RequireComponent(typeof(MobAI))]
	public class MobMeleeAttack : MobFollow
	{
		[Tooltip("The sprites gameobject. Needs to be a child of the prefab root")]
		public GameObject spriteHolder;

		[Tooltip("If a player gets close to this mob and blocks the mobs path to the target," +
				 "should the mob then focus on the human blocking it?. Only works if mob is targeting" +
				 "a player originally.")]
		public bool targetOtherPlayersWhoGetInWay;

		[Tooltip("Attack nothing but the target. No players in the way, no tiles, nada.")]
		public bool onlyHitTarget;

		[SerializeField] private AddressableAudioSource attackSound = null;
		

		public int hitDamage = 30;
		public string attackVerb;
		public BodyPartType defaultTarget;
		public float meleeCoolDown = 1f;

		private LayerMask checkMask;
		private int playersLayer;
		private int npcLayer;

		private MobAI mobAI;

		private bool isForLerpBack;
		private Vector3 lerpFrom;
		private Vector3 lerpTo;
		private float lerpProgress;
		private bool lerping;
		private bool isAttacking = false;


		public override void OnEnable()
		{
			base.OnEnable();
			playersLayer = LayerMask.NameToLayer("Players");
			npcLayer = LayerMask.NameToLayer("NPC");
			checkMask = LayerMask.GetMask("Players", "NPC", "Objects");
			mobAI = GetComponent<MobAI>();
		}

		protected override void OnPushSolid(Vector3Int destination)
		{
			CheckForAttackTarget();
		}

		protected override void OnTileReached(Vector3Int tilePos)
		{
			base.OnTileReached(tilePos);
			CheckForAttackTarget();
		}

		//Where is the target? Is there something in the way we can break
		//to get to the target?
		private bool CheckForAttackTarget()
		{
			if (followTarget != null)
			{
				if (mobAI.IsDead || mobAI.IsUnconscious)
				{
					Deactivate();
					followTarget = null;
					return false;
				}

				var followLivingBehaviour = followTarget.GetComponent<LivingHealthBehaviour>();
				var distanceToTarget = Vector3.Distance(followTarget.transform.position, transform.position);
				if (followLivingBehaviour != null)
				{
					//When to stop following on the server:
					if (followLivingBehaviour.IsDead || distanceToTarget > 30f)
					{
						Deactivate();
						followTarget = null;
						return false;
					}
				}

				var dirToTarget = (followTarget.position - transform.position).normalized;
				var hitInfo =
					MatrixManager.Linecast(transform.position + dirToTarget, LayerTypeSelection.Windows, checkMask, followTarget.position);
				//	Debug.DrawLine(transform.position + dirToTarget, followTarget.position, Color.blue, 10f);
				if (hitInfo.CollisionHit.GameObject != null)
				{
					if (Vector3.Distance(transform.position, hitInfo.TileHitWorld) < 1.5f)
					{
						var dir = (hitInfo.TileHitWorld - transform.position).normalized;

						//Only hit target
						if (onlyHitTarget)
						{
							var healthBehaviour = hitInfo.CollisionHit.GameObject.transform.GetComponent<LivingHealthBehaviour>();
							if (hitInfo.CollisionHit.GameObject.transform != followTarget || healthBehaviour.IsDead)
							{
								return false;
							}
							else
							{
								AttackFlesh(dir, healthBehaviour);
								return true;
							}
						}

						//What to do with player hit?
						if (hitInfo.CollisionHit.GameObject.transform.gameObject.layer == playersLayer)
						{
							var healthBehaviour = hitInfo.CollisionHit.GameObject.transform.GetComponent<LivingHealthBehaviour>();
							if (healthBehaviour.IsDead)
							{
								return false;
							}

							AttackFlesh(dir, healthBehaviour);

							if (followTarget.gameObject.layer == playersLayer)
							{
								if (followTarget != hitInfo.CollisionHit.GameObject.transform)
								{
									if (targetOtherPlayersWhoGetInWay)
									{
										followTarget = hitInfo.CollisionHit.GameObject.transform;
									}
								}
							}

							return true;
						}

						//What to do with NPC hit?
						if (hitInfo.CollisionHit.GameObject.transform.gameObject.layer == npcLayer)
						{
							var mobAi = hitInfo.CollisionHit.GameObject.transform.GetComponent<MobAI>();
							if (mobAi != null && mobAI != null)
							{
								if (mobAi.mobName.Equals(mobAI.mobName, StringComparison.OrdinalIgnoreCase))
								{
									return false;
								}
							}

							var healthBehaviour = hitInfo.CollisionHit.GameObject.transform.GetComponent<LivingHealthBehaviour>();
							if (healthBehaviour != null)
							{
								if (healthBehaviour.IsDead) return false;

								AttackFlesh(dir, healthBehaviour);
								return true;
							}
						}

						//What to do with Tile hits?
						if (distanceToTarget > 4.5f)
						{
							//Don't bother, the target is too far away to warrant a decision to break a tile
							return false;
						}

						AttackTile(hitInfo.TileHitWorld.RoundToInt(), dir);
						return true;
					}
				}
			}

			return false;
		}

		private void AttackFlesh(Vector2 dir, LivingHealthBehaviour healthBehaviour)
		{
			StartCoroutine(AttackFleshRoutine(dir, healthBehaviour));
		}

		//We need to slow the attack down because clients are behind server
		IEnumerator AttackFleshRoutine(Vector2 dir, LivingHealthBehaviour healthBehaviour)
		{
			if (healthBehaviour.connectionToClient == null)
			{
				yield break;
			}

			ServerDoLerpAnimation(dir);

			Debug.Log(
				$"CONN CLIENT TIME: {healthBehaviour.connectionToClient.lastMessageTime} Network time: {(float)NetworkTime.time}");
			if (PlayerManager.LocalPlayerScript != null
				&& PlayerManager.LocalPlayerScript.playerHealth != null
				&& PlayerManager.LocalPlayerScript.playerHealth == healthBehaviour ||
				healthBehaviour.RTT == 0f)
			{
				yield return WaitFor.EndOfFrame;
			}
			else
			{
				Debug.Log($"WAIT FOR ATTACK: {healthBehaviour.RTT / 2f}");
				yield return WaitFor.Seconds(healthBehaviour.RTT / 2f);
			}

			if (Vector3.Distance(transform.position, healthBehaviour.transform.position) < 1.5f)
			{
				healthBehaviour.ApplyDamageToBodypart(gameObject, hitDamage, AttackType.Melee, DamageType.Brute,
					defaultTarget.Randomize());
				Chat.AddAttackMsgToChat(gameObject, healthBehaviour.gameObject, defaultTarget, null, attackVerb);
				SoundManager.PlayNetworkedAtPos(attackSound, transform.position, sourceObj: gameObject);
			}
		}

		private void AttackTile(Vector3Int worldPos, Vector2 dir)
		{
			var matrix = MatrixManager.AtPoint(worldPos, true);
			matrix.MetaTileMap.ApplyDamage(MatrixManager.WorldToLocalInt(worldPos, matrix), hitDamage * 2, worldPos);
			ServerDoLerpAnimation(dir);
		}

		private void ServerDoLerpAnimation(Vector2 dir)
		{
			directional.FaceDirection(Orientation.From(dir));

			Pause = true;
			isAttacking = true;
			MobMeleeLerpMessage.Send(gameObject, dir);
			StartCoroutine(WaitForLerp());
		}

		IEnumerator WaitForLerp()
		{
			float timeElapsed = 0f;
			while (isAttacking)
			{
				timeElapsed += Time.deltaTime;

				if (timeElapsed > 3f)
				{
					isAttacking = false;
				}

				yield return WaitFor.EndOfFrame;
			}

			yield return WaitFor.Seconds(meleeCoolDown);

			DeterminePostAttackActions();
		}

		//What should the Mob do after an attack action has finished:
		private void DeterminePostAttackActions()
		{
			if (Random.value > 0.2f) //80% chance of hitting the target again
			{
				if (!CheckForAttackTarget())
				{
					Pause = false;
				}
			}
			else
			{
				Pause = false;
			}
		}

		public void ClientDoLerpAnimation(Vector2 dir)
		{
			lerpFrom = spriteHolder.transform.localPosition;
			lerpTo = spriteHolder.transform.localPosition + (Vector3)(dir * 0.5f);

			lerpProgress = 0f;
			isForLerpBack = true;
			lerping = true;
		}

		private void ResetLerp()
		{
			lerpProgress = 0f;
			lerping = false;
			isForLerpBack = false;
		}

		protected override void ServerUpdateMe()
		{
			CheckLerping();
			base.ServerUpdateMe();
		}

		void CheckLerping()
		{
			if (lerping)
			{
				lerpProgress += Time.deltaTime;
				spriteHolder.transform.localPosition = Vector3.Lerp(lerpFrom, lerpTo, lerpProgress * 7f);
				if (spriteHolder.transform.localPosition == lerpTo || lerpProgress >= 1f)
				{
					if (!isForLerpBack)
					{
						ResetLerp();
						spriteHolder.transform.localPosition = Vector3.zero;

						if (isServer)
						{
							isAttacking = false;
						}
					}
					else
					{
						//To lerp back
						ResetLerp();
						lerpTo = lerpFrom;
						lerpFrom = spriteHolder.transform.localPosition;
						lerping = true;
					}
				}
			}
		}
	}
}
