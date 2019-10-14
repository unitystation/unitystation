using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MobFollow))]
[RequireComponent(typeof(MobExplore))]
[RequireComponent(typeof(MobFlee))]
public class MobAI : MonoBehaviour
{
	protected MobFollow mobFollow;
	protected MobExplore mobExplore;
	protected MobFlee mobFlee;
	protected LivingHealthBehaviour health;
	protected bool isServer;

	private float followingTime = 0f;
	private float followTimeMax;

	protected virtual void Awake()
	{
		mobFollow = GetComponent<MobFollow>();
		mobExplore = GetComponent<MobExplore>();
		mobFlee = GetComponent<MobFlee>();
		health = GetComponent<LivingHealthBehaviour>();
	}

	public virtual void OnEnable()
	{
		//only needed for starting via a map scene through the editor:
		if (CustomNetworkManager.Instance == null) return;

		if (CustomNetworkManager.Instance._isServer)
		{
			UpdateManager.Instance.Add(UpdateMe);
			isServer = true;
		}
	}

	public void OnDisable()
	{
		if (isServer)
		{
			UpdateManager.Instance.Remove(UpdateMe);
		}
	}

	/// <summary>
	/// Server only update loop. Make sure to call base.UpdateMe() if overriding
	/// </summary>
	protected virtual void UpdateMe()
	{
		MonitorFollowingTime();
	}

	void MonitorFollowingTime()
	{
		if (mobFollow.activated && followTimeMax != -1f)
		{
			followingTime += Time.deltaTime;
			if (followingTime > followTimeMax)
			{
				StopFollowing();
			}
		}
	}

	/// <summary>
	/// Called on the server whenever a localchat event has been heard
	/// by the NPC
	/// </summary>
	public virtual void LocalChatReceived(ChatEvent chatEvent) { }

	protected void FollowTarget(Transform target, float followDuration = -1f)
	{
		ResetBehaviours();
		followTimeMax = followDuration;
		followingTime = 0f;
		mobFollow.StartFollowing(target);
	}

	protected void StopFollowing()
	{
		mobFollow.Deactivate();
		followTimeMax = -1f;
		followingTime = 0f;
	}

	void ResetBehaviours()
	{
		if (mobFlee.activated)
		{
			mobFlee.Deactivate();
		}

		if (mobFollow.activated)
		{
			mobFollow.Deactivate();
		}

		if (mobExplore.activated)
		{
			mobExplore.Deactivate();
		}
	}
}
