using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Doors;
using Systems.Mob;
using Random = UnityEngine.Random;
using AddressableReferences;
using HealthV2;
using Messages.Server.SoundMessages;
using System.Threading.Tasks;
using Systems.Score;


namespace Systems.MobAIs
{
	/// <summary>
	/// Generic hostile AI that will attack all players
	/// in its sight. Hostile AI's should inherit this one and
	/// override its methods!
	/// </summary>
	[RequireComponent(typeof(MobMeleeAction))]
	[RequireComponent(typeof(ConeOfSight))]
	public class GenericHostileAI : MobAI
	{
		[SerializeField]
		[Tooltip("Sounds played when this mob dies")]
		protected List<AddressableAudioSource> deathSounds = default;

		[SerializeField]
		[Tooltip("Sounds played randomly while this mob is alive")]
		protected List<AddressableAudioSource> randomSounds = default;

		[Tooltip("Amount of time to wait between each random sound. Decreasing this value could affect performance!")]
		[SerializeField]
		protected int playRandomSoundTimer = 3;

		[SerializeField]
		[Range(0,100)]
		protected int randomSoundProbability = 20;
		[SerializeField]
		protected float searchTickRate = 0.5f;
		protected float searchWaitTime = 0f;

		protected float movementTickRate = 1f;
		protected float moveWaitTime = 0f;

		protected float forceActionTickRate = 1f;
		protected float forceActionWaitTime = 0f;

		protected bool deathSoundPlayed = false;
		[SerializeField] protected MobStatus currentStatus;
		public MobStatus CurrentStatus => currentStatus;

		protected LayerMask hitMask;
		protected int playersLayer;
		protected MobMeleeAction mobMeleeAction;
		protected ConeOfSight coneOfSight;
		protected int fleeChance = 30;
		protected int attackLastAttackerChance = 80;

		[SerializeField] private int ScoreForKilling = 5;

		#region Lifecycle

		protected override void Awake()
		{
			hitMask = LayerMask.GetMask( "Players");
			playersLayer = LayerMask.NameToLayer("Players");
			mobMeleeAction = GetComponent<MobMeleeAction>();
			coneOfSight = GetComponent<ConeOfSight>();
			base.Awake();
		}

		protected override void OnSpawnMob()
		{
			movementTickRate = Random.Range(1f, 3f);
		}

		protected override void OnAIStart()
		{
			StartCoroutine(PlayRandomSound());
			BeginSearch();
		}

		#endregion
		public override void ContemplatePriority()
		{
			base.ContemplatePriority();

			if (!isServer || !MatrixManager.IsInitialized)
			{
				return;
			}

			if (IsDead || IsUnconscious)
			{
				HandleDeathOrUnconscious();
				return;
			}

			switch (currentStatus)
			{
				case MobStatus.Searching:
					HandleSearch();
					break;
				case MobStatus.Attacking:
					if(mobMeleeAction.isOnCooldown) break;
					MonitorIdleness();
					break;
				case MobStatus.None:
					MonitorIdleness();
					break;
				default:
					HandleSearch();
					break;
			}
		}

		/// <summary>
		/// Declare the current state of the mob as Searching.
		/// </summary>
		protected virtual void BeginSearch()
		{
			searchWaitTime = 0f;
			currentStatus = MobStatus.Searching;
		}

		/// <summary>
		/// Declare the current state of the mob as attacking.
		/// </summary>
		/// <param name="target">Gameobject that this mob will target to attack</param>
		protected virtual void BeginAttack(GameObject target)
		{
			currentStatus = MobStatus.Attacking;
			FollowTarget(target);
		}

		protected override void ResetBehaviours()
		{
			base.ResetBehaviours();
			mobMeleeAction.FollowTarget = null;
			currentStatus = MobStatus.None;
			searchWaitTime = 0f;
		}

		protected virtual void MonitorIdleness()
		{
			if (mobMeleeAction.FollowTarget == null)
			{
				BeginSearch();
			}
			//We have target but not acting, so force do something
			else
			{
				forceActionWaitTime += MobController.UpdateTimeInterval;
				if (forceActionWaitTime >= forceActionTickRate)
				{
					forceActionWaitTime = 0f;
					mobMeleeAction.DoAction();
				}
			}
		}
		/// <summary>
		/// Looks around and tries to find players to target
		/// </summary>
		/// <returns>Gameobject of the first player it found</returns>
		protected virtual GameObject SearchForTarget()
		{
			var player = Physics2D.OverlapCircleAll(registerObject.WorldPositionServer.To2Int(), 20f, hitMask);
			//var hits = coneOfSight.GetObjectsInSight(hitMask, LayerTypeSelection.Walls, dirSprites.CurrentFacingDirection, 10f, 20);
			if (player.Length == 0)
			{
				return null;
			}

			foreach (var coll in player)
			{
				if (MatrixManager.Linecast(
					gameObject.AssumedWorldPosServer(),
					LayerTypeSelection.Walls,
					null,
					coll.gameObject.AssumedWorldPosServer()).ItHit == false)
				{
					if(coll.gameObject.TryGetComponent<LivingHealthMasterBase>(out var health) == false ||
					   health.IsDead) continue;

					return coll.gameObject;
				}

			}

			return null;
		}

		/// <summary>
		/// Makes the mob move to a random direction
		/// </summary>
		protected virtual void DoRandomMove()
		{
			if (!MatrixManager.IsInitialized) return;

			var nudgeDir = GetNudgeDirFromInt(Random.Range(0, 8));
			if (registerObject.Matrix.IsSpaceAt(registerObject.LocalPosition + nudgeDir.To3Int(), true))
			{
				for (int i = 0; i < 8; i++)
				{
					var testDir = GetNudgeDirFromInt(i);
					var checkTile = registerObject.LocalPosition + testDir.To3Int();
					if (registerObject.Matrix.IsSpaceAt(checkTile, true))
					{
						continue;
					}
					if (registerObject.Matrix.IsPassableAtOneMatrixOneTile(checkTile, true, context: gameObject))
					{
						nudgeDir = testDir;
						break;
					}

					if (!registerObject.Matrix.GetFirst<DoorController>(checkTile, true))
					{
						continue;
					}
					nudgeDir = testDir;
					break;
				}
			}

			NudgeInDirection(nudgeDir);
			movementTickRate = Random.Range(1f, 3f);
		}

		protected virtual IEnumerator PlayRandomSound(bool force = false)
		{
			while(!IsDead && !IsUnconscious && randomSounds.Count > 0 && this != null)
			{
				yield return WaitFor.Seconds(playRandomSoundTimer);
				if (force || DMMath.Prob(randomSoundProbability))
				{
					AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(0.9f, 1.1f));
					SoundManager.PlayNetworkedAtPos(randomSounds, transform.position,
					audioSourceParameters, sourceObj: gameObject);
				}
			}
		}

		/// <summary>
		/// What happens when the mob dies or is unconscious
		/// </summary>
		protected virtual void HandleDeathOrUnconscious()
		{
			if (!IsDead || deathSoundPlayed || deathSounds.Count <= 0) return;
			ResetBehaviours();
			deathSoundPlayed = true;

			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(0.9f, 1.1f));
			SoundManager.PlayNetworkedAtPos(deathSounds, transform.position,
				audioSourceParameters, sourceObj: gameObject);
			ScoreMachine.AddToScoreInt(ScoreForKilling, RoundEndScoreBuilder.COMMON_SCORE_HOSTILENPCDEAD);
		}

		/// <summary>
		/// What happens if the mob is searching
		/// </summary>
		protected virtual void HandleSearch()
		{
			moveWaitTime += MobController.UpdateTimeInterval;
			searchWaitTime += MobController.UpdateTimeInterval;
			if (!(searchWaitTime >= searchTickRate)) return;
			searchWaitTime = 0f;
			var findTarget = SearchForTarget();
			if (findTarget != null)
			{
				BeginAttack(findTarget);
			}
			else
			{
				BeginSearch();
			}
		}

		protected override void OnAttackReceived(GameObject damagedBy = null)
		{
			if (damagedBy == null)
			{
				StartFleeing(damagedBy);
			}

			if (health.OverallHealth < -20f)
			{
				//30% chance the mob decides to flee:
				if (DMMath.Prob(fleeChance))
				{
					StartFleeing(damagedBy, 5f);
					return;
				}
			}

			if ((damagedBy is null) || damagedBy == mobMeleeAction.FollowTarget)
			{
				return;
			}

			//80% chance the mob decides to attack the new attacker
			if (DMMath.Prob(attackLastAttackerChance) == false)
			{
				return;
			}

			var playerScript = damagedBy.GetComponent<PlayerScript>();
			if (playerScript != null)
			{
				BeginAttack(damagedBy);
			}
		}

		public override void LocalChatReceived(ChatEvent chatEvent)
		{
			if (chatEvent.originator == null) return;

			if (currentStatus != MobStatus.Searching && currentStatus != MobStatus.None) return;

			//face towards the origin:
			var dir = (chatEvent.originator.transform.position - transform.position).normalized;
			rotatable.SetFaceDirectionLocalVector(dir.RoundTo2Int());

			//Then scan to see if anyone is there:
			var findTarget = SearchForTarget();
			if (findTarget != null)
			{
				BeginAttack(findTarget);
			}
		}

		public enum MobStatus
		{
			None,
			Searching,
			Attacking
		}
	}
}
