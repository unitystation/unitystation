using System;
using System.Collections;
using HealthV2;
using Messages.Server;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Systems.MobAIs
{
	/// <summary>
	/// Basic MobAI for following and acting in melee.
	/// You can override the method for your own stuff.
	/// </summary>
	public class MobMeleeAction : MobFollow
	{
		[Tooltip("If a player gets close to this mob and blocks the mobs path to the target," +
		         "should the mob then focus on the human blocking it?. Only works if mob is targeting" +
		         "a player originally.")]
		[SerializeField]
		protected bool targetOtherPlayersWhoGetInWay = true;

		[Tooltip("Act on nothing but the target. No players in the way, no tiles, nada.")] [SerializeField]
		public bool onlyActOnTarget = false;

		[SerializeField] private bool doLerpOnAction;

		[SerializeField] private GameObject spriteHolder = null;

		[SerializeField] private float actionCooldown = 1f;

		protected LayerMask checkMask;
		protected int playersLayer;
		protected int npcLayer;

		private bool isForLerpBack;
		private Vector3 lerpFrom;
		private Vector3 lerpTo;
		private float lerpProgress;
		private bool lerping;
		private bool isActing = false;
		public bool isOnCooldown = false;

		private BoxCollider2D Collider;

		/// <summary>
		/// Maximum range that the mob will continue to try to act on the target
		/// NOTE: Max raycast range is 25 see MatrixManager line 357
		/// </summary>
		protected float TetherRange = 25f;

		public void OnEnable()
		{
			playersLayer = LayerMask.NameToLayer("Players");
			npcLayer = LayerMask.NameToLayer("NPC");
			checkMask = LayerMask.GetMask("Players", "NPC", "Objects");
			Collider = this.GetComponent<BoxCollider2D>();
		}

		/// <summary>
		/// Cooldown that tells DoAction() to not hit the player. ServerDoLerpAnimation() only affects the animation!!
		/// </summary>
		/// <returns></returns>
		private IEnumerator Cooldown()
		{
			isOnCooldown = true;
			yield return WaitFor.Seconds(actionCooldown);
			isOnCooldown = false;
		}

		/// <summary>
		/// Determines if the target of the action can be acted upon and what kind of target it is.
		/// Then performs the appropriate action. Action methods are individually overridable for flexibility.
		/// </summary>
		public override void DoAction()
		{
			if(isOnCooldown) return;
			if (objectPhysics.ContainedInObjectContainer != null) return;
			base.DoAction();
			var hitInfo = ValidateTarget();
			if (hitInfo.ItHit == false)
			{
				return;
			}

			StartCoroutine(Cooldown());
			var dir = (hitInfo.TileHitWorld - mobTile.WorldPositionServer).normalized;

			if (hitInfo.CollisionHit.GameObject != null &&
			    (hitInfo.TileHitWorld - mobTile.WorldPositionServer).sqrMagnitude <= 4)
			{
				if (onlyActOnTarget)
				{
					PerformActionOnlyOnTarget(hitInfo, dir);
					return;
				}

				if (hitInfo.CollisionHit.GameObject.layer == playersLayer)
				{
					PerformActionPlayer(hitInfo, dir);
					return;
				}

				if (hitInfo.CollisionHit.GameObject.layer == npcLayer)
				{
					PerformActionNpc(hitInfo, dir);
					return;
				}
			}

			PerformActionTile(hitInfo, dir);
		}

		/// <summary>
		/// Determines if the target is valid
		/// </summary>
		/// <returns>A CustomPhysicsHit result of a Line Cast to the target, or a default one if the target is invalid</returns>
		protected virtual MatrixManager.CustomPhysicsHit ValidateTarget()
		{
			if (FollowTarget != null)
			{
				if (mobAI.IsDead == false && mobAI.IsUnconscious == false)
				{
					var followLivingBehaviour = FollowTarget.GetComponent<LivingHealthMasterBase>();
					if (followLivingBehaviour != null)
					{
						if (followLivingBehaviour.IsDead)
						{
							FollowTarget = null;
						}
					}

					//Continue if it still exists and is in range
					if (FollowTarget != null && TargetDistance() < TetherRange)
					{
						Collider.enabled = false;
						Vector3 dir =
							(Vector3)(FollowTarget.WorldPositionServer - mobTile.WorldPositionServer).Normalize() /
							1.5f;
						var hitInfo = MatrixManager.Linecast(mobTile.WorldPositionServer + (dir*0.1f),
							LayerTypeSelection.Windows | LayerTypeSelection.Grills, checkMask,
							FollowTarget.WorldPositionServer);
						Collider.enabled = true;
						if (hitInfo.ItHit)
						{
							return hitInfo;
						}
					}
				}
			}

			FollowTarget = null;
			return new MatrixManager.CustomPhysicsHit();
		}

		/// <summary>
		/// What to do if the Mob will only act on the target
		/// </summary>
		protected virtual bool PerformActionOnlyOnTarget(MatrixManager.CustomPhysicsHit hitInfo, Vector3 dir)
		{
			var healthBehaviourV2 = hitInfo.CollisionHit.GameObject.GetComponent<LivingHealthMasterBase>();
			if (healthBehaviourV2 != null)
			{
				if (hitInfo.CollisionHit.GameObject == FollowTarget && healthBehaviourV2.IsDead == false)
				{
					ActOnLivingV2(dir, healthBehaviourV2);
					return true;
				}
			}
			else
			{
				var healthBehaviour = hitInfo.CollisionHit.GameObject.GetComponent<LivingHealthBehaviour>();
				if (healthBehaviour != null)
				{
					if (hitInfo.CollisionHit.GameObject == FollowTarget && healthBehaviour.IsDead == false)
					{
						ActOnLiving(dir, healthBehaviour);
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// What to do if the Mob is trying to act on a Player
		/// </summary>
		protected virtual bool PerformActionPlayer(MatrixManager.CustomPhysicsHit hitInfo, Vector3 dir)
		{
			var healthBehaviour = hitInfo.CollisionHit.GameObject.GetComponent<LivingHealthMasterBase>();
			if (healthBehaviour != null)
			{
				if (healthBehaviour.IsDead)
				{
					return false;
				}

				ActOnLivingV2(dir, healthBehaviour);

				if (FollowTarget != null && FollowTarget.gameObject.layer != playersLayer)
				{
					return true;
				}

				if (FollowTarget != null && FollowTarget == hitInfo.CollisionHit.GameObject)
				{
					return true;
				}

				if (targetOtherPlayersWhoGetInWay)
				{
					FollowTarget = hitInfo.CollisionHit.GameObject.GetComponent<RegisterTile>();
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// What to do if the Mob is trying to act on an NPC
		/// </summary>
		protected virtual bool PerformActionNpc(MatrixManager.CustomPhysicsHit hitInfo, Vector3 dir)
		{
			var target = hitInfo.CollisionHit.GameObject.GetComponent<MobAI>();

			//Prevent the mob from acting on itself and those like it
			if (target != null && mobAI != null)
			{
				if (target.mobName.Equals(mobAI.mobName, StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
			}

			var healthBehaviour = hitInfo.CollisionHit.GameObject.GetComponent<LivingHealthBehaviour>();


			if (healthBehaviour != null)
			{
				if (healthBehaviour.IsDead)
				{
					return false;
				}

				ActOnLiving(dir, healthBehaviour);
				return true;
			}
			else
			{
				//Safety catch in case the NPC is using the new health system
				var healthBehaviourV2 = hitInfo.CollisionHit.GameObject.GetComponent<LivingHealthMasterBase>();
				if (healthBehaviourV2 != null)
				{
					if (healthBehaviourV2.IsDead)
					{
						return false;
					}

					ActOnLivingV2(dir, healthBehaviourV2);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// What to do if the Mob is trying to act on a Tile
		/// </summary>
		protected virtual void PerformActionTile(MatrixManager.CustomPhysicsHit hitInfo, Vector3 dir)
		{
			if (TargetDistance() > 4.5f)
			{
				//Don't bother, the target is too far away to warrant a decision to break a tile
				return;
			}

			ActOnTile(hitInfo.TileHitWorld.RoundToInt(), dir);
		}

		/// <summary>
		/// Virtual method to override on extensions of this class for acting on living targets using the old health system
		/// </summary>
		protected virtual void ActOnLiving(Vector3 dir, LivingHealthBehaviour healthBehaviour)
		{
		}

		/// <summary>
		/// Virtual method to override on extensions of this class for acting on living targets using the new health system
		/// </summary>
		protected virtual void ActOnLivingV2(Vector3 dir, LivingHealthMasterBase livingHealth)
		{
		}

		/// <summary>
		/// Virtual method to override on extensions of this class for acting on tiles
		/// </summary>
		protected virtual void ActOnTile(Vector3Int roundToInt, Vector3 dir)
		{
		}

		public virtual void ServerDoLerpAnimation(Vector2 dir)
		{
			rotatable.SetFaceDirectionLocalVector(dir.RoundTo2Int());

			isActing = true;
			MobMeleeLerpMessage.Send(gameObject, dir);
			StartCoroutine(WaitForLerp());
		}

		private IEnumerator WaitForLerp()
		{
			float timeElapsed = 0f;
			while (isActing)
			{
				timeElapsed += Time.deltaTime;

				if (timeElapsed > 3f)
				{
					isActing = false;
				}

				yield return WaitFor.EndOfFrame;
			}

			yield return WaitFor.Seconds(actionCooldown);
			DeterminePostAction();
		}

		protected virtual void DeterminePostAction()
		{
			if (Random.value > 0.2f) //80% chance of hitting the target again
			{
				DoAction();
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

		protected void ServerUpdateMe()
		{
			CheckLerping();
		}

		private void CheckLerping()
		{
			if (!lerping)
			{
				return;
			}

			lerpProgress += Time.deltaTime;
			spriteHolder.transform.localPosition = Vector3.Lerp(lerpFrom, lerpTo, lerpProgress * 7f);

			if (spriteHolder.transform.localPosition != lerpTo && !(lerpProgress >= 1f))
			{
				return;
			}

			if (!isForLerpBack)
			{
				ResetLerp();
				spriteHolder.transform.localPosition = Vector3.zero;

				isActing = false;
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
