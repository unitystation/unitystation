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
	protected NPCDirectionalSprites dirSprites;
	protected CustomNetTransform cnt;
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
		dirSprites = GetComponent<NPCDirectionalSprites>();
		cnt = GetComponent<CustomNetTransform>();
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

	/// <summary>
	/// please use these values:
	/// 1 = N, 2 = NE, 3 = E, 4 = SE, 5 = S, 6 = SW, 7 = W, 8 = NW
	/// This is because it is better to not allow any variations between the
	/// defined directions
	/// </summary>
	protected void NudgeInDir(int dir)
	{
		switch (dir)
		{
			case 1: //N
				cnt.Push(Vector2Int.up);
				break;
			case 2: //NE
				cnt.Push(Vector2Int.one);
				break;
			case 3: //E
				cnt.Push(Vector2Int.right);
				break;
			case 4: //SE
				cnt.Push(new Vector2Int(1, -1));
				break;
			case 5: //S
				cnt.Push(Vector2Int.down);
				break;
			case 6: //SW
				cnt.Push(Vector2Int.one * -1);
				break;
			case 7: //W
				cnt.Push(Vector2Int.left);
				break;
			case 8: //NW
				cnt.Push(new Vector2Int(-1, 1));
				break;
		}
	}

	//Resets all the behaviours:
	protected void ResetBehaviours()
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

		fleeTimeMax = -1f;
		fleeingTime = 0f;
		exploreTimeMax = -1f;
		exploringTime = 0f;
		followTimeMax = -1f;
		followingTime = 0f;
	}
}
