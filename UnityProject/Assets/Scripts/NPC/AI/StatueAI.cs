using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy Statue NPC's
/// Will attack any human that they see
/// </summary>
[RequireComponent(typeof(MobMeleeAttack))]
[RequireComponent(typeof(ConeOfSight))]
public class StatueAI : MobAI
{
	private MobMeleeAttack mobAttack;
	private ConeOfSight coneOfSight;
	public float searchTickRate = 0.5f;
	private float movementTickRate = 1f;
	private float moveWaitTime = 0f;
	private float searchWaitTime = 0f;
	private LayerMask hitMask;
	private int playersLayer;
	private bool DeathSoundPlayed = false;

	public List<string> DeathSounds = new List<string>();
	public List<string> GenericSounds = new List<string>();

	/// <summary>
	/// Changes Time that a sound has the chance to play
	/// WARNING, decreasing this time will decrease performance.
	/// </summary>
	public int PlaySoundTime = 3;

	public enum StatueStatus
	{
		None,
		Searching,
		Attacking
	}

	public StatueStatus status;

	public override void OnEnable()
	{
		base.OnEnable();
		hitMask = LayerMask.GetMask("Walls", "Players");
		playersLayer = LayerMask.NameToLayer("Players");
		mobAttack = GetComponent<MobMeleeAttack>();
		coneOfSight = GetComponent<ConeOfSight>();
		PlaySound();
	}

	//AI is now active on the server
	protected override void AIStartServer()
	{
		//begin searching
		movementTickRate = Random.Range(1f, 3f);
		BeginSearch();
	}

	//Sets a random time when a search should take place
	void BeginSearch()
	{
		searchWaitTime = 0f;
		status = StatueStatus.Searching;
	}

	GameObject SearchForTarget()
	{
		var hits = coneOfSight.GetObjectsInSight(hitMask, dirSprites.CurrentFacingDirection, 10f, 20);
		if (hits.Count == 0)
		{
			return null;
		}

		//lets find a target:
		foreach (Collider2D coll in hits)
		{
			if (coll.gameObject.layer == playersLayer)
			{
				return coll.gameObject;
			}
		}

		return null;
	}

	//The statue has heard something!!
	public override void LocalChatReceived(ChatEvent chatEvent)
	{
		if (chatEvent.originator == null) return;

		if (status == StatueStatus.Searching || status == StatueStatus.None)
		{
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
	}

	//The statue has been attacked by something!
	protected override void OnAttackReceived(GameObject damagedBy)
	{
		if (damagedBy == null) //when something is on fire, damagedBy is null
		{
			return;
		}

		if (health.OverallHealth < -20f)
		{
			//10% chance the statue decides to flee:
			if (Random.value < 0.1f)
			{
				StartFleeing(damagedBy.transform, 5f);
				return;
			}
		}

		if (damagedBy != mobAttack.followTarget)
		{
			//80% chance the statue decides to attack the new attacker
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

	private void DoRandomMove()
	{
		var nudgeDir = GetNudgeDirFromInt(Random.Range(0, 8));
		if (registerObject.Matrix.IsSpaceAt(registerObject.LocalPosition + nudgeDir.To3Int(), true))
		{
			for (int i = 0; i < 8; i++)
			{
				var testDir = GetNudgeDirFromInt(i);
				var checkTile = registerObject.LocalPosition + testDir.To3Int();
				if (!registerObject.Matrix.IsSpaceAt(checkTile, true))
				{
					if (registerObject.Matrix.IsPassableAt(checkTile, true))
					{
						nudgeDir = testDir;
						break;
					}
					else
					{
						if (registerObject.Matrix.GetFirst<DoorController>(checkTile, true))
						{
							nudgeDir = testDir;
							break;
						}
					}
				}
			}
		}

		NudgeInDirection(nudgeDir);
		movementTickRate = Random.Range(1f, 3f);
	}

	protected override void UpdateMe()
	{
		base.UpdateMe();

		if (!isServer) return;

		if (IsDead || IsUnconscious)
		{
			if (IsDead && !DeathSoundPlayed && DeathSounds.Count > 0)
			{
				DeathSoundPlayed = true;
				SoundManager.PlayNetworkedAtPos(DeathSounds[Random.Range(0, DeathSounds.Count - 1)], transform.position, Random.Range(0.9f, 1.1f), sourceObj: gameObject);
			}

			return;
		}

		if (status == StatueStatus.Searching)
		{
			moveWaitTime += Time.deltaTime;
			if (moveWaitTime >= movementTickRate)
			{
				moveWaitTime = 0f;
				DoRandomMove();
			}

			searchWaitTime += Time.deltaTime;
			if (searchWaitTime >= searchTickRate)
			{
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
		}

		if (status == StatueStatus.Attacking || status == StatueStatus.None)
		{
			MonitorIdleness();
		}
	}

	void PlaySound()
	{
		if (!IsDead && !IsUnconscious && GenericSounds.Count > 0)
		{
			var num = Random.Range(1, 5);
			if (num == 1)
			{
				SoundManager.PlayNetworkedAtPos(GenericSounds[Random.Range(0, GenericSounds.Count - 1)], transform.position, Random.Range(0.9f, 1.1f), sourceObj: gameObject);
			}
			Invoke("PlaySound", PlaySoundTime);
		}
	}

	//Determine if mob has become idle:
	void MonitorIdleness()
	{
		if (!mobAttack.performingDecision && mobAttack.followTarget == null)
		{
			BeginSearch();
		}
	}

	void BeginAttack(GameObject target)
	{
		status = StatueStatus.Attacking;
		FollowTarget(target.transform);
	}

	protected override void ResetBehaviours()
	{
		base.ResetBehaviours();
		status = StatueStatus.None;
		searchWaitTime = 0f;
	}

	public override void OnDespawnServer(DespawnInfo info)
	{
		base.OnDespawnServer(info);
		dirSprites.SetToBodyLayer();
		DeathSoundPlayed = false;
		registerObject.Passable = true;
	}
}