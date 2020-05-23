using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
public class BulletBehaviour : MonoBehaviour
{
	private BodyPartType bodyAim;
	[Range(0, 100)]
	public float damage = 25;
	private GameObject shooter;
	protected Gun weapon;
	public DamageType damageType;
	public AttackType attackType = AttackType.Bullet;
	private bool isSuicide = false;

	public bool isMiningBullet = false;
	/// <summary>
	/// Cached trailRenderer. Note that not all bullets have a trail, thus this can be null.
	/// </summary>
	protected LocalTrailRenderer trailRenderer;

	/// <summary>
	/// Rigidbody on the child transform (the one that actually moves when a shot happens)
	/// </summary>
	protected Rigidbody2D rigidBody;
	//	public BodyPartType BodyPartAim { get; private set; };

	public float maxBulletDistance;
	public bool isRangeLimit = false;

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
	public void Suicide(GameObject controlledByPlayer, Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest) {
		isSuicide = true;
		StartShoot(Vector2.zero, controlledByPlayer, fromWeapon, targetZone);
	}

	/// <summary>
	/// Shoot in a direction
	/// </summary>
	/// <param name="dir"></param>
	/// <param name="controlledByPlayer"></param>
	/// <param name="targetZone"></param>
	/// <param name="fromWeapon">Weapon the shot is being fired from</param>
	public virtual void Shoot(Vector2 dir, GameObject controlledByPlayer, Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest)
	{
		isSuicide = false;
		StartShoot(dir, controlledByPlayer, fromWeapon, targetZone);
		if (isRangeLimit)
		{
			StartCoroutine(countTiles());
		}
		
	}

	public IEnumerator countTiles()
	{
		float time = maxBulletDistance / weapon.ProjectileVelocity;
		yield return WaitFor.Seconds(time);
		//Begin despawn
		DespawnThis();
	}

	protected void StartShoot(Vector2 dir, GameObject controlledByPlayer, Gun fromWeapon, BodyPartType targetZone)
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
			//TODO: Which is better? rigidBody.AddForce(dir.normalized * fromWeapon.ProjectileVelocity, ForceMode2D.Impulse);
			rigidBody.velocity = dir.normalized * fromWeapon.ProjectileVelocity;
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

	/// <summary>
	/// Invoked when BulletColliderBehavior passes the event up to us.
	/// </summary>
	public virtual void HandleCollisionEnter2D(Collision2D coll)
	{
		if (coll.gameObject == shooter && !isSuicide)
		{
			return;
		}
		DespawnThis();
	}

	/// <summary>
	/// Invoked when BulletColliderBehavior passes the event up to us.
	/// </summary>
	public void HandleTriggerEnter2D(Collider2D coll)
	{
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

		//body or object?
		var livingHealth = coll.GetComponent<LivingHealthBehaviour>();
		var integrity = coll.GetComponent<Integrity>();
		if (integrity != null)
		{
			//damage object
			integrity.ApplyDamage(damage, attackType, damageType);
			Chat.AddAttackMsgToChat(shooter, coll.gameObject, BodyPartType.None, weapon.gameObject);
			Logger.LogTraceFormat("Hit {0} for {1} with HealthBehaviour! bullet absorbed", Category.Firearms, integrity.gameObject.name, damage);
		}
		else
		{
			//damage human if there is one
			if (livingHealth == null || livingHealth.IsDead)
			{
				return;
			}

		// Trigger for things like stuns
		GetComponent<BulletHitTrigger>()?.BulletHitInteract(coll.gameObject);

			var aim = isSuicide ? bodyAim : bodyAim.Randomize();
			livingHealth.ApplyDamageToBodypart(shooter, damage, attackType, damageType, aim);
			Chat.AddAttackMsgToChat(shooter, coll.gameObject, aim, weapon.gameObject);
			Logger.LogTraceFormat("Hit {0} for {1} with HealthBehaviour! bullet absorbed", Category.Firearms, livingHealth.gameObject.name, damage);
		}

		DespawnThis();
	}

	protected virtual void DespawnThis()
	{
		if (trailRenderer != null)
		{
			trailRenderer.ShotDone();
		}
		rigidBody.velocity = Vector2.zero;
		Despawn.ClientSingle(gameObject);
	}
}