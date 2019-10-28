using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy Xeno NPC's
/// Will attack any human that they see
/// </summary>
[RequireComponent(typeof(MobMeleeAttack))]
[RequireComponent(typeof(ConeOfSight))]
public class XenoAI : MobAI
{
	private MobMeleeAttack mobAttack;
	private ConeOfSight coneOfSight;
	public float searchTickRate = 0.5f;
	private float movementTickRate = 1f;
	private float moveWaitTime = 0f;
	private float searchWaitTime = 0f;
	private LayerMask hitMask;
	private int playersLayer;

	public enum XenoStatus
	{
		None,
		Searching,
		Attacking
	}

	public XenoStatus status;

	public override void OnEnable()
	{
		base.OnEnable();
		hitMask = LayerMask.GetMask("Walls", "Players");
		playersLayer = LayerMask.NameToLayer("Players");
		mobAttack = GetComponent<MobMeleeAttack>();
		coneOfSight = GetComponent<ConeOfSight>();
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
		status = XenoStatus.Searching;
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

	//The alien has heard something!!
	public override void LocalChatReceived(ChatEvent chatEvent)
	{

	}

	private void DoRandomMove()
	{
		NudgeInDir(Random.Range(1,9));
		movementTickRate = Random.Range(1f, 3f);
	}

	protected override void UpdateMe()
	{
		base.UpdateMe();
		if (status == XenoStatus.Searching)
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

		if (status == XenoStatus.Attacking || status == XenoStatus.None)
		{
			MonitorIdleness();
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
		status = XenoStatus.Attacking;
		FollowTarget(target.transform);
	}

	protected override void ResetBehaviours()
	{
		base.ResetBehaviours();
		status = XenoStatus.None;
		searchWaitTime = 0f;
	}
}
