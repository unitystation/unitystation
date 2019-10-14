using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

	private float exploringTime = 0f;
	private float exploreTimeMax;

	private float fleeingTime = 0f;
	private float fleeTimeMax;

	//Events:
	protected UnityEvent followingStopped = new UnityEvent();
	protected UnityEvent exploringStopped = new UnityEvent();
	protected UnityEvent fleeingStopped = new UnityEvent();

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
			AIStartServer();
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
	/// Called when the AI has come online on the server
	/// </summary>
	protected virtual void AIStartServer() { }

	/// <summary>
	/// Server only update loop. Make sure to call base.UpdateMe() if overriding
	/// </summary>
	protected virtual void UpdateMe()
	{
		MonitorFollowingTime();
		MonitorExploreTime();
		MonitorFleeingTime();
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

	void MonitorExploreTime()
	{
		if (mobExplore.activated && exploreTimeMax != -1f)
		{
			exploringTime += Time.deltaTime;
			if (exploringTime > exploreTimeMax)
			{
				StopExploring();
			}
		}
	}

	void MonitorFleeingTime()
	{
		if (mobFlee.activated && fleeTimeMax != -1f)
		{
			fleeingTime += Time.deltaTime;
			if (fleeingTime > fleeTimeMax)
			{
				StopFleeing();
			}
		}
	}

	/// <summary>
	/// Called on the server whenever a localchat event has been heard
	/// by the NPC
	/// </summary>
	public virtual void LocalChatReceived(ChatEvent chatEvent) { }

	/// <summary>
	/// Call this to begin following a target.
	/// </summary>
	/// <param name="target"></param>
	/// <param name="followDuration"></param>
	protected void FollowTarget(Transform target, float followDuration = -1f)
	{
		ResetBehaviours();
		followTimeMax = followDuration;
		followingTime = 0f;
		mobFollow.StartFollowing(target);
	}

	/// <summary>
	/// Stops any following behaviour
	/// </summary>
	protected void StopFollowing()
	{
		mobFollow.Deactivate();
		followTimeMax = -1f;
		followingTime = 0f;
		followingStopped.Invoke();
	}

	/// <summary>
	/// Begins exploring for the target
	/// </summary>
	protected void BeginExploring(MobExplore.Target target = MobExplore.Target.food, float exploreDuration = -1f)
	{
		ResetBehaviours();
		mobExplore.BeginExploring(target);
		exploreTimeMax = exploreDuration;
		exploringTime = 0f;
	}

	/// <summary>
	/// Stop exploring
	/// </summary>
	protected void StopExploring()
	{
		mobExplore.Deactivate();
		exploreTimeMax = -1f;
		exploringTime = 0f;
		exploringStopped.Invoke();
	}

	/// <summary>
	/// Start fleeing from the target
	/// </summary>
	protected void StartFleeing(Transform fleeTarget, float fleeDuration = -1f)
	{
		ResetBehaviours();
		mobFlee.FleeFromTarget(fleeTarget);
		fleeTimeMax = fleeDuration;
		fleeingTime = 0f;
	}

	//Stop fleeing
	protected void StopFleeing()
	{
		mobFlee.Deactivate();
		fleeTimeMax = -1f;
		fleeingTime = 0f;
		fleeingStopped.Invoke();
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
