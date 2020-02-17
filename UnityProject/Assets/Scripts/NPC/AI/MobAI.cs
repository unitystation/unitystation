using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(MobFollow))]
[RequireComponent(typeof(MobExplore))]
[RequireComponent(typeof(MobFlee))]
public class MobAI : MonoBehaviour, IServerDespawn
{
	public string mobName;
	[Tooltip("When the mob is unconscious, how much should the sprite obj " +
	         "be rotated to indicate a knocked down or dead NPC")]
	public float knockedDownRotation = 90f;

	protected MobFollow mobFollow;
	protected MobExplore mobExplore;
	protected MobFlee mobFlee;
	protected LivingHealthBehaviour health;
	protected NPCDirectionalSprites dirSprites;
	protected CustomNetTransform cnt;
	protected RegisterObject registerObject;
	protected UprightSprites uprightSprites;
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

	/// <summary>
	/// Is MobAI currently performing an AI task like following or exploring
	/// </summary>
	public bool IsPerformingTask
	{
		get
		{
			if (mobExplore.activated || mobFollow.activated || mobFlee.activated)
			{
				return true;
			}

			return false;
		}
	}

	public bool IsDead
	{
		get { return health.IsDead; }
	}

	public bool IsUnconscious
	{
		get { return health.IsCrit; }
	}

	protected virtual void Awake()
	{
		mobFollow = GetComponent<MobFollow>();
		mobExplore = GetComponent<MobExplore>();
		mobFlee = GetComponent<MobFlee>();
		health = GetComponent<LivingHealthBehaviour>();
		dirSprites = GetComponent<NPCDirectionalSprites>();
		cnt = GetComponent<CustomNetTransform>();
		registerObject = GetComponent<RegisterObject>();
		uprightSprites = GetComponent<UprightSprites>();
	}

	public virtual void OnEnable()
	{
		//only needed for starting via a map scene through the editor:
		if (CustomNetworkManager.Instance == null) return;

		if (CustomNetworkManager.Instance._isServer)
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			health.applyDamageEvent += OnAttackReceived;
			isServer = true;
			AIStartServer();
		}
	}

	public void OnDisable()
	{
		if (isServer)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			health.applyDamageEvent += OnAttackReceived;
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
		if (IsDead || IsUnconscious)
		{
			//Allow players to walk over the body:
			if (!registerObject.Passable)
			{
				registerObject.Passable = true;
				dirSprites.SetToBodyLayer();
				MonitorUprightState();
			}
			return;
		}

		//Maybe the mob was revived set passable back to false
		//and put sprite render sort layer back to NPC:
		if (registerObject.Passable)
		{
			registerObject.Passable = false;
			dirSprites.SetToNPCLayer();
			MonitorUprightState();
		}

		MonitorFollowingTime();
		MonitorExploreTime();
		MonitorFleeingTime();
	}


	//Should mob be knocked down?
	void MonitorUprightState()
	{
		if (IsDead || IsUnconscious)
		{
			if (dirSprites.spriteRend.transform.localEulerAngles.z == 0f)
			{
				SoundManager.PlayNetworkedAtPos("Bodyfall", transform.position);
				dirSprites.SetRotationServer(knockedDownRotation);
			}
		}
		else
		{
			if (dirSprites.spriteRend.transform.localEulerAngles.z != 0f)
			{
				dirSprites.SetRotationServer(0f);
			}
		}
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
	/// Called on the server whenever the NPC is physically attacked
	/// </summary>
	/// <param name="damagedBy"></param>
	protected virtual void OnAttackReceived(GameObject damagedBy) { }

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
	/// 0 = N, 1 = NE, 2 = E, 3 = SE, 4 = S, 5 = SW, 6 = W, 7 = NW
	/// This is because it is better to not allow any variations between the
	/// defined directions
	/// </summary>
	protected Vector2Int GetNudgeDirFromInt(int dir)
	{
		//Apply offset to the nudge dir if this mob is on a rotated matrix
		if (uprightSprites != null)
		{
			if (uprightSprites.ExtraRotation.eulerAngles != Vector3.zero)
			{
				var a = (uprightSprites.ExtraRotation.eulerAngles.z * -1f) / 45f;
				var b = dir + (int) a;
				if (b < -7)
				{
					b += 7;
				} else if (b > 7)
				{
					b -= 7;
				}

				dir = b;
			}
		}

		Vector2Int nudgeDir = Vector2Int.zero;
		switch (dir)
		{
			case 0: //N
				nudgeDir = Vector2Int.up;
				break;
			case 1: //NE
				nudgeDir = Vector2Int.one;
				break;
			case 2: //E
				nudgeDir = Vector2Int.right;
				break;
			case 3: //SE
				nudgeDir = new Vector2Int(1, -1);
				break;
			case 4: //S
				nudgeDir = Vector2Int.down;
				break;
			case 5: //SW
				nudgeDir = Vector2Int.one * -1;
				break;
			case 6: //W
				nudgeDir = Vector2Int.left;
				break;
			case 7: //NW
				nudgeDir = new Vector2Int(-1, 1);
				break;
		}

		return nudgeDir;
	}

	/// <summary>
	/// Nudge the Mob in a certain direction
	/// </summary>
	/// <param name="dir"></param>
	protected void NudgeInDirection(Vector2Int dir)
	{
		if (dir != Vector2Int.zero)
		{
			cnt.Push(dir);
			var angleOfDir = Vector3.Angle((Vector2)dir, transform.up);
			if (dir.x < 0f)
			{
				angleOfDir = -angleOfDir;
			}

			dirSprites.CheckSpriteServer(angleOfDir);
		}
	}

	/// <summary>
	/// Resets all the behaviours when choosing another action.
	/// Do not use this for a hard reset (for when reusing from a pool etc)
	/// use GoingOffStageServer instead
	/// </summary>
	/// <returns></returns>
	protected virtual void ResetBehaviours()
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

	public virtual void OnDespawnServer(DespawnInfo info)
	{
		ResetBehaviours();
	}
}
