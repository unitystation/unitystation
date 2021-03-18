﻿using System;
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


namespace Systems.MobAIs
{
	/// <summary>
	/// Generic hostile AI that will attack all players
	/// in its sight. Hostile AI's should inherit this one and
	/// override its methods!
	/// </summary>
	[RequireComponent(typeof(MobMeleeAction))]
	[RequireComponent(typeof(ConeOfSight))]
	public class GenericHostileAI : MobAI, IServerSpawn
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
		protected float movementTickRate = 1f;
		protected float moveWaitTime = 0f;
		protected float searchWaitTime = 0f;
		protected bool deathSoundPlayed = false;
		[SerializeField] protected MobStatus currentStatus;
		public MobStatus CurrentStatus => currentStatus;

		protected LayerMask hitMask;
		protected int playersLayer;
		protected MobMeleeAction mobMeleeAction;
		protected ConeOfSight coneOfSight;
		protected SimpleAnimal simpleAnimal;
		protected int fleeChance = 30;
		protected int attackLastAttackerChance = 80;

		public override void OnEnable()
		{
			base.OnEnable();
			hitMask = LayerMask.GetMask( "Players");
			playersLayer = LayerMask.NameToLayer("Players");
			mobMeleeAction = GetComponent<MobMeleeAction>();
			coneOfSight = GetComponent<ConeOfSight>();
			simpleAnimal = GetComponent<SimpleAnimal>();

			if(CustomNetworkManager.IsServer == false) return;
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
			if (!mobMeleeAction.performingDecision && mobMeleeAction.FollowTarget == null)
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
			var player = Physics2D.OverlapCircleAll(transform.position, 20f, hitMask);
			//var hits = coneOfSight.GetObjectsInSight(hitMask, LayerTypeSelection.Walls, dirSprites.CurrentFacingDirection, 10f, 20);
			if (player.Length == 0)
			{
				return null;
			}

			foreach (var coll in player)
			{
				if (MatrixManager.Linecast(
					gameObject.WorldPosServer(),
					LayerTypeSelection.Walls,
					null,
					coll.gameObject.WorldPosServer()).ItHit == false)
				{
					if(coll.gameObject.TryGetComponent<LivingHealthMasterBase>(out var health) && health.IsDead) continue;

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

		protected virtual async Task PlayRandomSound(bool force = false)
		{
			while(!IsDead && !IsUnconscious && randomSounds.Count > 0)
			{
				await Task.Delay(playRandomSoundTimer * 1000); //Converted from seconds to milliseconds
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
				if (DMMath.Prob(fleeChance))
				{
					StartFleeing(damagedBy, 5f);
					return;
				}
			}

			if ((damagedBy is null) != false || damagedBy == mobMeleeAction.FollowTarget)
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
			directional.FaceDirection(Orientation.From(dir));

			//Then scan to see if anyone is there:
			var findTarget = SearchForTarget();
			if (findTarget != null)
			{
				BeginAttack(findTarget);
			}
		}

		public virtual void OnSpawnServer(SpawnInfo info)
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
			mobSprite.SetToNPCLayer();
			registerObject.RestoreAllToDefault();
			if (simpleAnimal != null)
			{
				simpleAnimal.SetDeadState(false);
			}
		}

		public override void OnDespawnServer(DespawnInfo info)
		{
			base.OnDespawnServer(info);
			mobSprite.SetToBodyLayer();
			deathSoundPlayed = false;
			registerObject.SetPassable(false, true);
		}

		public enum MobStatus
		{
			None,
			Searching,
			Attacking
		}
	}
}
