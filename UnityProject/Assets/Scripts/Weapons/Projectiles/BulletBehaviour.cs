using UnityEngine;

/// <summary>
/// Main behavior for a bullet, handles shooting and managing the trail rendering. Collision events are fired on
/// the child gameobject's BulletColliderBehavior and passed up to this component.
///
/// Note that the bullet prefab has this on the root transform, but the actual traveling projectile is in a
/// child transform. When shooting happens, the root transform remains still relative to its parent, but
/// the child transform is the one that actually moves.
///
/// This allows the trail to be relative to the matrix, so the trail still looks correct when the matrix is moving.
/// </summary>
public abstract class BulletBehaviour : MonoBehaviour
{
	private BodyPartType bodyAim;
	[Range(0, 100)]
	public int damage = 25;
	private GameObject shooter;
	private Weapon weapon;
	public DamageType damageType;
	private bool isSuicide = false;
	/// <summary>
	/// Cached trailRenderer. Note that not all bullets have a trail, thus this can be null.
	/// </summary>
	private LocalTrailRenderer trailRenderer;

	/// <summary>
	/// Rigidbody on the child transform (the one that actually moves when a shot happens)
	/// </summary>
	private Rigidbody2D rigidBody;
	//	public BodyPartType BodyPartAim { get; private set; };

	private void Awake()
	{
		//Using Awake() instead of start because Start() doesn't seem to get called when this is instantiated
		if (trailRenderer == null)
		{
			trailRenderer = GetComponent<LocalTrailRenderer>();
		}

		if (rigidBody == null)
		{
			rigidBody = GetComponentInChildren<Rigidbody2D>();
		}
	}

	public Vector2 Direction { get; private set; }

	/// <summary>
	/// Shoot the controlledByPlayer
	/// </summary>
	/// <param name="controlledByPlayer">player doing the shooting</param>
	/// <param name="targetZone">body part being targeted</param>
	/// <param name="fromWeapon">Weapon the shot is being fired from</param>
	public void Suicide(GameObject controlledByPlayer, Weapon fromWeapon, BodyPartType targetZone = BodyPartType.Chest) {
		isSuicide = true;
		StartShoot(Vector2.zero, controlledByPlayer, fromWeapon, targetZone);
		OnShoot();
	}

	/// <summary>
	/// Shoot in a direction
	/// </summary>
	/// <param name="dir"></param>
	/// <param name="controlledByPlayer"></param>
	/// <param name="targetZone"></param>
	/// <param name="fromWeapon">Weapon the shot is being fired from</param>
	public void Shoot(Vector2 dir, GameObject controlledByPlayer, Weapon fromWeapon, BodyPartType targetZone = BodyPartType.Chest)
	{
		isSuicide = false;
		StartShoot(dir, controlledByPlayer, fromWeapon, targetZone);
		OnShoot();
	}

	private void StartShoot(Vector2 dir, GameObject controlledByPlayer, Weapon fromWeapon, BodyPartType targetZone)
	{
		weapon = fromWeapon;
		Direction = dir;
		bodyAim = targetZone;
		shooter = controlledByPlayer;

		transform.parent = controlledByPlayer.transform.parent;
		Vector3 startPos = new Vector3(dir.x, dir.y, transform.position.z) / 2;
		transform.position += startPos;
		rigidBody.transform.rotation = Quaternion.AngleAxis(Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg, Vector3.forward);
		rigidBody.transform.localPosition = Vector3.zero;
		if (!isSuicide)
		{
			rigidBody.AddForce(dir.normalized * 24f, ForceMode2D.Impulse);
		}
		else
		{
			rigidBody.velocity = Vector2.zero;
		}

		//tell our trail to start drawing if we have one
		if (trailRenderer != null)
		{
			trailRenderer.ShotStarted();
		}
	}

	//TODO  - change so that on call the bullets damage is set properly
	public abstract void OnShoot();

	/// <summary>
	/// Invoked when BulletColliderBehavior passes the event up to us.
	/// </summary>
	public void HandleCollisionEnter2D(Collision2D coll)
	{
		if (coll.gameObject == shooter && !isSuicide)
		{
			return;
		}
		ReturnToPool();
	}

	/// <summary>
	/// Invoked when BulletColliderBehavior passes the event up to us.
	/// </summary>
	public void HandleTriggerEnter2D(Collider2D coll)
	{
		LivingHealthBehaviour damageable = coll.GetComponent<LivingHealthBehaviour>();

		//only harm others if it's not a suicide
		if (coll.gameObject == shooter && !isSuicide)
		{
			return;
		}

		//only harm the shooter if it's a suicide
		if (coll.gameObject != shooter && isSuicide)
		{
			return;
		}

		if (damageable == null || damageable.IsDead)
		{
			return;
		}
		var aim = isSuicide ? bodyAim : bodyAim.Randomize();
		damageable.ApplyDamage(shooter, damage, damageType, aim);
		PostToChatMessage.SendItemAttackMessage(weapon.gameObject, shooter, coll.gameObject, damage, aim);
		Logger.LogTraceFormat("Hit {0} for {1} with HealthBehaviour! bullet absorbed", Category.Firearms, damageable.gameObject.name, damage);
		ReturnToPool();
	}

	private void ReturnToPool()
	{
		if (trailRenderer != null)
		{
			trailRenderer.ShotDone();
		}
		PoolManager.Instance.PoolClientDestroy(gameObject);
	}
}