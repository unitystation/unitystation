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
	private LayerMask npcMask;
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
		Attacking,
		Frozen
	}

	public StatueStatus status;

	private readonly Dictionary<int, Orientation> Orientations = new Dictionary<int, Orientation>()
	{
		{1, Orientation.Up},
		{2, Orientation.Right},
		{3, Orientation.Down},
		{4, Orientation.Left}
	};

		int DirToInt(Vector3 direction)
	{
		var angleOfDir = Vector3.Angle((Vector2) direction, transform.up);
		if (direction.x < 0f)
		{
			angleOfDir = -angleOfDir;
		}
		if (angleOfDir > 180f)
		{
			angleOfDir = -180 + (angleOfDir - 180f);
		}

		if (angleOfDir < -180f)
		{
			angleOfDir = 180f + (angleOfDir + 180f);
		}

		switch (angleOfDir)
		{
			case 0:
				return 1;
			case float n when n == -180f || n == 180f:
				return 3;
			case float n when n > 0f:
				return 2;
			case float n when n < 0f:
				return 4;
			default:
				return 2;

		}
	}

	public override void OnEnable()
	{
		base.OnEnable();
		hitMask = LayerMask.GetMask("Walls", "Players");
		npcMask = LayerMask.GetMask("Walls", "NPC");
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

	protected override void UpdateMe()
	{
		base.UpdateMe();

		if (DeadMonitor()) return;
		StatusLoop();
	}

	bool DeadMonitor()
	{
		if (!isServer) return true;
		if (IsDead || IsUnconscious)
		{
			if(IsDead && !DeathSoundPlayed && DeathSounds.Count > 0)
			{
				DeathSoundPlayed = true;
				SoundManager.PlayNetworkedAtPos(
					DeathSounds[Random.Range(0, DeathSounds.Count -1)],
					transform.position,
					Random.Range(0.9f, 1.1f),
					sourceObj: gameObject);

					return true;
			}
		}

		return false;
	}


	void StatusLoop()
	{
		if (status == StatueStatus.Frozen || status == StatueStatus.Attacking)
		{
			MonitorIdleness();
			return;
		}

		if (status == StatueStatus.Searching)
		{
			moveWaitTime += Time.deltaTime;
			if (moveWaitTime >= movementTickRate)
			{
				moveWaitTime = 0f;
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

	bool IsSomeoneLookingAtMe()
	{
		var hits = coneOfSight.GetObjectsInSight(hitMask, dirSprites.CurrentFacingDirection, 10f, 20);
		if (hits.Count == 0) return false;

		foreach (Collider2D coll in hits)
		{
			var dir = (transform.position - coll.gameObject.transform.position).normalized;

			if (coll.gameObject.layer == playersLayer
				&& !coll.gameObject.GetComponent<LivingHealthBehaviour>().IsDead
				&& coll.gameObject.GetComponent<Directional>()?.CurrentDirection == Orientations[DirToInt(dir)])
			{
				Freeze();
				return true;
			}
		}

		return false;
	}

	//The statue has heard something!!
	public override void LocalChatReceived(ChatEvent chatEvent)
	{
		if (chatEvent.originator == null) return;

		if (status == StatueStatus.Searching)
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

	void PlaySound()
	{
		if (!IsDead && !IsUnconscious && GenericSounds.Count > 0)
		{
			var num = Random.Range(1, 5);
			if (num == 1)
			{
				SoundManager.PlayNetworkedAtPos(
					GenericSounds[Random.Range(0, GenericSounds.Count - 1)],
					transform.position, Random.Range(0.9f, 1.1f),sourceObj: gameObject);
			}
			Invoke("PlaySound", PlaySoundTime);
		}
	}

	//Determine if mob has become idle:
	void MonitorIdleness()
	{

		if (!mobAttack.performingDecision && mobAttack.followTarget == null && !IsSomeoneLookingAtMe())
		{
			BeginSearch();
		}
	}

	void BeginSearch()
	{
		searchWaitTime = 0f;
		status = StatueStatus.Searching;
	}

	void Freeze()
	{
		ResetBehaviours();
		status = StatueStatus.Frozen;
		mobAttack.followTarget = null;
	}

	void BeginAttack(GameObject target)
	{
		ResetBehaviours();
		status = StatueStatus.Attacking;
		StartCoroutine(StatueStalk(target.transform));
	}

	IEnumerator StatueStalk(Transform stalked)
	{	
		while (!IsSomeoneLookingAtMe())
		{
			if(mobAttack.followTarget == null)
			{
				mobAttack.StartFollowing(stalked);
			}
			yield return WaitFor.Seconds(.2f);
		}

		Freeze();
		yield break;
	}

	protected override void ResetBehaviours()
	{
		base.ResetBehaviours();
		status = StatueStatus.Searching;
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