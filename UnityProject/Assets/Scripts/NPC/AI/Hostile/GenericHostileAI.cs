using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NPC
{
	/// <summary>
	/// Generic hostile AI that will attack all players
	/// in its sight. Hostile AI's should inherit this one and
	/// override its methods!
	/// </summary>
	[RequireComponent(typeof(MobMeleeAttack))]
	[RequireComponent(typeof(ConeOfSight))]
	public class GenericHostileAI : MobAI, IServerSpawn
	{
		[SerializeField]
		protected List<string> deathSounds = new List<string>();
		[SerializeField]
		protected List<string> randomSound = new List<string>();
		[Tooltip("Amount of time to wait between each random sound. Decreasing this value could affect performance!")]
		[SerializeField]
		protected int playRandomSoundTimer = 3;
		[SerializeField]
		[Range(0,100)]
		protected int randomSoundProbability = 20;
		[SerializeField]
		protected float searchTickRate = 0.5f;
		protected float movementTickRate = 1f;
		protected float moveWaitTime = 0f;
		protected float searchWaitTime = 0f;
		protected bool deathSoundPlayed = false;
		[SerializeField] protected MobStatus currentStatus;
		public MobStatus CurrentStatus => currentStatus;

		protected LayerMask hitMask;
		protected int playersLayer;
		protected MobMeleeAttack mobMeleeAttack;
		protected ConeOfSight coneOfSight;
		protected SimpleAnimal simpleAnimal;

		public override void OnEnable()
		{
			base.OnEnable();
			hitMask = LayerMask.GetMask("Walls", "Players");
			playersLayer = LayerMask.NameToLayer("Players");
			mobMeleeAttack = GetComponent<MobMeleeAttack>();
			coneOfSight = GetComponent<ConeOfSight>();
			simpleAnimal = GetComponent<SimpleAnimal>();
			PlayRandomSound();
		}


		protected override void AIStartServer()
		{
			movementTickRate = Random.Range(1f, 3f);
			BeginSearch();
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
			FollowTarget(target.transform);
		}

		protected override void ResetBehaviours()
		{
			base.ResetBehaviours();
			mobFollow.followTarget = null;
			currentStatus = MobStatus.None;
			searchWaitTime = 0f;
		}

		protected virtual void MonitorIdleness()
		{
			if (!mobMeleeAttack.performingDecision && mobMeleeAttack.followTarget == null)
			{
				BeginSearch();
			}
		}

		/// <summary>
		/// Looks around and tries to find players to target
		/// </summary>
		/// <returns>Gameobject of the first player it found</returns>
		protected virtual GameObject SearchForTarget()
		{
			var hits = coneOfSight.GetObjectsInSight(hitMask, dirSprites.CurrentFacingDirection, 10f, 20);
			if (hits.Count == 0)
			{
				return null;
			}

			foreach (Collider2D coll in hits)
			{
				if (coll.gameObject.layer == playersLayer)
				{
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
					if (registerObject.Matrix.IsPassableAt(checkTile, true))
					{
						nudgeDir = testDir;
						break;
					}
					else
					{
						if (!registerObject.Matrix.GetFirst<DoorController>(checkTile, true))
						{
							continue;
						}
						nudgeDir = testDir;
						break;
					}
				}
			}

			NudgeInDirection(nudgeDir);
			movementTickRate = Random.Range(1f, 3f);
		}

		protected virtual void PlayRandomSound(bool force = false)
		{
			if (IsDead || IsUnconscious || randomSound.Count <= 0)
			{
				return;
			}

			if (!force)
			{
				if (!DMMath.Prob(randomSoundProbability))
				{
					return;
				}
			}

			{
				SoundManager.PlayNetworkedAtPos(
					randomSound.PickRandom(),
					transform.position,
					Random.Range(0.9f, 1.1f),
					sourceObj: gameObject);
			}
			Invoke(nameof(PlayRandomSound), playRandomSoundTimer);
		}

		/// <summary>
		/// What happens when the mob dies or is unconscious
		/// </summary>
		protected virtual void HandleDeathOrUnconscious()
		{
			if (!IsDead || deathSoundPlayed || deathSounds.Count <= 0) return;
			ResetBehaviours();
			deathSoundPlayed = true;
			SoundManager.PlayNetworkedAtPos(
				deathSounds.PickRandom(),
				transform.position,
				Random.Range(0.9f, 1.1f),
				sourceObj: gameObject);
		}

		/// <summary>
		/// What happens if the mob is searching
		/// </summary>
		protected virtual void HandleSearch()
		{
			moveWaitTime += Time.deltaTime;
			if (moveWaitTime >= movementTickRate)
			{
				moveWaitTime = 0f;
				DoRandomMove();
			}

			searchWaitTime += Time.deltaTime;
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

		protected override void UpdateMe()
		{
			base.UpdateMe();

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

		protected override void OnAttackReceived(GameObject damagedBy = null)
		{
			if (damagedBy == null)
			{
				StartFleeing(damagedBy);
			}

			if (health.OverallHealth < -20f)
			{
				//30% chance the mob decides to flee:
				if (Random.value < 0.3f)
				{
					StartFleeing(damagedBy, 5f);
					return;
				}
			}

			if (damagedBy != mobMeleeAttack.followTarget)
			{
				//80% chance the mob decides to attack the new attacker
				if (Random.value < 0.8f)
				{
					var playerScript = damagedBy.GetComponent<PlayerScript>();
					if (playerScript != null)
					{
						BeginAttack(damagedBy);
						return;
					}
				}
			}
		}

		public override void LocalChatReceived(ChatEvent chatEvent)
		{
			if (chatEvent.originator == null) return;

			if (currentStatus != MobStatus.Searching && currentStatus != MobStatus.None) return;

			//face towards the origin:
			var dir = (chatEvent.originator.transform.position - transform.position).normalized;
			dirSprites.ChangeDirection(dir);

			//Then scan to see if anyone is there:
			var findTarget = SearchForTarget();
			if (findTarget != null)
			{
				BeginAttack(findTarget);
			}
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			//FIXME This shouldn't be called by client yet it seems it is
			if (!isServer)
			{
				return;
			}

			OnSpawnMob();
		}

		protected virtual void OnSpawnMob()
		{
			dirSprites.SetToNPCLayer();
			registerObject.Passable = false;
			if (simpleAnimal != null)
			{
				simpleAnimal.SetDeadState(false);
			}
		}

		public override void OnDespawnServer(DespawnInfo info)
		{
			base.OnDespawnServer(info);
			dirSprites.SetToBodyLayer();
			deathSoundPlayed = false;
			registerObject.Passable = true;
		}

		public enum MobStatus
		{
			None,
			Searching,
			Attacking
		}
	}
}
