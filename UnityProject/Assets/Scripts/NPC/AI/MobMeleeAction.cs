using System;
using System.Collections;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NPC.AI
{
	/// <summary>
	/// Derives from MobMeleeAttack but instead of attacking, performs an action
	/// on tile reached. You can override the method for your own stuff.
	/// </summary>
	public class MobMeleeAction : MobFollow
	{
		[Tooltip("If a player gets close to this mob and blocks the mobs path to the target," +
		         "should the mob then focus on the human blocking it?. Only works if mob is targeting" +
		         "a player originally.")]
		[SerializeField]
		private bool targetOtherPlayersWhoGetInWay = true;

		[Tooltip("Act on nothing but the target. No players in the way, no tiles, nada.")]
		[SerializeField]
		private bool onlyActOnTarget = false;

		[SerializeField]
		private bool doLerpOnAction;

		[SerializeField]
		private GameObject spriteHolder = null;

		[SerializeField]
		private float actionCooldown = 1f;

		private LayerMask checkMask;
		private int playersLayer;
		private int npcLayer;

		private MobAI mobAI;

		private bool isForLerpBack;
		private Vector3 lerpFrom;
		private Vector3 lerpTo;
		private float lerpProgress;
		private bool lerping;
		private bool isActing = false;

		public override void OnEnable()
		{
			base.OnEnable();
			playersLayer = LayerMask.NameToLayer("Players");
			npcLayer = LayerMask.NameToLayer("NPC");
			checkMask = LayerMask.GetMask("Players", "NPC", "Windows", "Objects");
			mobAI = GetComponent<MobAI>();
		}

		protected override void OnPushSolid(Vector3Int destination)
		{
			CheckForTargetAction();
		}

		protected override void OnTileReached(Vector3Int tilePos)
		{
			base.OnTileReached(tilePos);
			CheckForTargetAction();
		}

		protected virtual bool CheckForTargetAction()
		{
			if (followTarget == null)
			{
				return false;
			}

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
			RaycastHit2D hitInfo = Physics2D.Linecast(
				transform.position + dirToTarget,
				followTarget.position,
				checkMask);

			if (hitInfo.collider == null)
			{
				return false;
			}

			if (!(Vector3.Distance(transform.position, hitInfo.point) < 1.5f))
			{
				return false;
			}

			var dir = ((Vector3) hitInfo.point - transform.position).normalized;

			//Only hit target
			if (onlyActOnTarget)
			{
				var healthBehaviour = hitInfo.transform.GetComponent<LivingHealthBehaviour>();
				if (hitInfo.transform != followTarget || healthBehaviour.IsDead)
				{
					return false;
				}
				else
				{
					mobAI.ActOnLiving(dir, healthBehaviour);
					return true;
				}
			}

			//What to do with player hit?
			if (hitInfo.transform.gameObject.layer == playersLayer)
			{
				var healthBehaviour = hitInfo.transform.GetComponent<LivingHealthBehaviour>();
				if (healthBehaviour != null && healthBehaviour.IsDead)
				{
					return false;
				}

				mobAI.ActOnLiving(dir, healthBehaviour);

				if (followTarget == null) return false;

				if (followTarget.gameObject.layer != playersLayer)
				{
					return true;
				}

				if (followTarget == hitInfo.transform)
				{
					return true;
				}

				if (targetOtherPlayersWhoGetInWay)
				{
					followTarget = hitInfo.transform;
				}

				return true;
			}

			//What to do with NPC hit?
			if (hitInfo.transform.gameObject.layer == npcLayer)
			{
				var mobAi = hitInfo.transform.GetComponent<MobAI>();
				if (mobAi != null && mobAI != null)
				{
					if (mobAi.mobName.Equals(mobAI.mobName, StringComparison.OrdinalIgnoreCase))
					{
						return false;
					}
				}

				var healthBehaviour = hitInfo.transform.GetComponent<LivingHealthBehaviour>();
				if (healthBehaviour != null)
				{
					if (healthBehaviour.IsDead) return false;

					mobAI.ActOnLiving(dir, healthBehaviour);
					return true;
				}
			}

			//What to do with Tile hits?
			if (distanceToTarget > 4.5f)
			{
				//Don't bother, the target is too far away to warrant a decision to break a tile
				return false;
			}

			mobAI.ActOnTile(hitInfo.point.RoundToInt(), dir);
			return true;
		}

		public void ServerDoLerpAnimation(Vector2 dir)
		{
			var angleOfDir = Vector3.Angle(dir, transform.up);
			if (dir.x < 0f)
			{
				angleOfDir = -angleOfDir;
			}

			dirSprites.CheckSpriteServer(angleOfDir);

			Pause = true;
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

		private void DeterminePostAction()
		{
			if (Random.value > 0.2f) //80% chance of hitting the target again
			{
				if (!CheckForTargetAction())
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
			lerpTo = spriteHolder.transform.localPosition + (Vector3) (dir * 0.5f);

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

		protected override void UpdateMe()
		{
			CheckLerping();
			base.UpdateMe();
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

				if (isServer)
				{
					isActing = false;
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