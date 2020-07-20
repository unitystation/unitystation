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

	//Limit number of damage calls
	private int damageAttempts = 0;
	private int maxDamageAttempts = 1;

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
			return (mobExplore.activated || mobFollow.activated || mobFlee.activated);
		}
	}

	public bool IsDead => health.IsDead;

	public bool IsUnconscious => health.IsCrit;

	private bool isKnockedDown = false;

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
			UpdateManager.Add(PeriodicUpdate, 1f);
			health.applyDamageEvent += AttackReceivedCoolDown;
			isServer = true;
			AIStartServer();
		}
	}

	public void OnDisable()
	{
		if (isServer)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
			health.applyDamageEvent += AttackReceivedCoolDown;
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
		if (MonitorKnockedDown())
		{
			return;
		}

		MonitorFollowingTime();
		MonitorExploreTime();
		MonitorFleeingTime();
	}

	protected void PeriodicUpdate()
	{
		if (damageAttempts >= maxDamageAttempts)
		{
			damageAttempts = 0;
		}
	}

	/// <summary>
	/// Updates the mob to fall down or stand up where appropriate
	/// </summary>
	/// <returns>Whether the mob is currently knocked down</returns>
	private bool MonitorKnockedDown()
	{
		if (IsDead || IsUnconscious)
		{
			Knockdown();
			return true;
		}
		else
		{
			GetUp();
			return false;
		}
	}

	/// <summary>
	/// Make the mob fall to the ground if it isn't already there
	/// </summary>
	private void Knockdown()
	{
		if (!isKnockedDown)
		{
			isKnockedDown = true;

			registerObject.Passable = true;
			dirSprites.SetToBodyLayer();

			SoundManager.PlayNetworkedAtPos("Bodyfall", transform.position, sourceObj: gameObject);
			dirSprites.SetRotationServer(knockedDownRotation);
		}
	}

	/// <summary>
	/// Make the mob stand up if it isn't already stood
	/// </summary>
	private void GetUp()
	{
		if (isKnockedDown)
		{
			isKnockedDown = false;

			registerObject.RestorePassableToDefault();
			dirSprites.SetToNPCLayer();

			dirSprites.SetRotationServer(0f);
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

	protected void AttackReceivedCoolDown(GameObject damagedBy = null)
	{
		if (damageAttempts >= maxDamageAttempts)
		{
			return;
		}

		damageAttempts++;

		OnAttackReceived(damagedBy);
	}

	/// <summary>
	/// Called on the server whenever the NPC is physically attacked
	/// </summary>
	/// <param name="damagedBy"></param>
	protected virtual void OnAttackReceived(GameObject damagedBy = null) { }

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
	protected void StartFleeing(GameObject fleeTarget, float fleeDuration = -1f)
	{
		ResetBehaviours();

		if (fleeTarget == null) //run from itself?
		{
			fleeTarget = gameObject;
		}

		mobFlee.FleeFromTarget(fleeTarget.transform);
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
	/// Common behavior to flee from attacker if health is less than X
	/// Call this within OnAttackedReceive method
	/// </summary>
	/// <param name="healthThreshold">If health is less than this, RUN!</param>
	/// <param name="attackedBy">Gameobject from the attacker. This can be null on fire!</param>
	/// <param name="fleeDuration">Time in seconds the flee behavior will last. Defaults to forever</param>
	protected void FleeIfHealthLessThan(float healthThreshold, GameObject attackedBy = null, float fleeDuration = -1f)
	{
		if (attackedBy == null)
		{
			attackedBy = gameObject;
		}

		if (health.OverallHealth < healthThreshold)
		{
			StartFleeing(attackedBy, fleeDuration);
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

	///<summary>
	/// Triggers on creatures with Pettable component when petted.
	///</summary>
	///<param name="performer">The player petting</param>
	public virtual void OnPetted(GameObject performer)
	{
		// face performer
		var dir = (performer.transform.position - transform.position).normalized;
		dirSprites.ChangeDirection(dir);
	}

	///<summary>
	/// Triggers when the explorer targets people and found one
	///</summary>
	///<param name="player">PlayerScript from the found player</param>
	public virtual void ExplorePeople (PlayerScript player){}

	/// <summary>
	/// Virtual method to override on extensions of this class. Called when paired with MobMeleeAction
	/// </summary>
	/// <param name="dir"></param>
	/// <param name="healthBehaviour"></param>
	/// <param name="doLerpAnimation"></param>
	public virtual void ActOnLiving(Vector3 dir, LivingHealthBehaviour healthBehaviour) {}

	/// <summary>
	/// Virtual method to override on extensions of this class. Called when paired with MobMeleeAction
	/// </summary>
	/// <param name="roundToInt"></param>
	/// <param name="dir"></param>
	/// <param name="doLerpAnimation"></param>
	public virtual void ActOnTile(Vector3Int roundToInt, Vector3 dir) {}

	public virtual void OnDespawnServer(DespawnInfo info)
	{
		ResetBehaviours();
	}
}
