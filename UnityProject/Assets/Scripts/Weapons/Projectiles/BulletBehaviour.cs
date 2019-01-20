using UnityEngine;

public abstract class BulletBehaviour : MonoBehaviour
{
	private BodyPartType bodyAim;
	[Range(0, 100)]
	public int damage = 25;
	private GameObject shooter;
	private Weapon weapon;
	public DamageType damageType;
	private bool isSuicide = false;

	public TrailRenderer trail;

	private Rigidbody2D thisRigi;
	//	public BodyPartType BodyPartAim { get; private set; };

	public Vector2 Direction { get; private set; }

	/// <summary>
	/// Shoot the controlledByPlayer
	/// </summary>
	/// <param name="controlledByPlayer">player doing the shooting</param>
	/// <param name="targetZone">body part being targeted</param>
	/// <param name="fromWeapon">Weapon the shot is being fired from</param>
	public void Suicide(GameObject controlledByPlayer, Weapon fromWeapon, BodyPartType targetZone = BodyPartType.Chest) {
		isSuicide = true;
		StartShoot(Vector2.zero, 0, controlledByPlayer, fromWeapon, targetZone);
		OnShoot();
	}

	/// <summary>
	/// Shoot in a direction
	/// </summary>
	/// <param name="dir"></param>
	/// <param name="angle"></param>
	/// <param name="controlledByPlayer"></param>
	/// <param name="targetZone"></param>
	/// <param name="fromWeapon">Weapon the shot is being fired from</param>
	public void Shoot(Vector2 dir, float angle, GameObject controlledByPlayer, Weapon fromWeapon, BodyPartType targetZone = BodyPartType.Chest)
	{
		isSuicide = false;
		StartShoot(dir, angle, controlledByPlayer, fromWeapon, targetZone);
		OnShoot();
	}

	private void StartShoot(Vector2 dir, float angle, GameObject controlledByPlayer, Weapon fromWeapon, BodyPartType targetZone)
	{
		weapon = fromWeapon;
		Direction = dir;
		bodyAim = targetZone;
		shooter = controlledByPlayer;
		transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
		Vector3 startPos = new Vector3(dir.x, dir.y, transform.position.z);
		transform.position += startPos;
		thisRigi = GetComponent<Rigidbody2D>();
		if (!isSuicide)
		{
			thisRigi.AddForce(dir.normalized * 24f, ForceMode2D.Impulse);
		}
		else
		{
			thisRigi.velocity = Vector2.zero;
		}
	}

	//TODO  - change so that on call the bullets damage is set properly
	public abstract void OnShoot();

	private void OnCollisionEnter2D(Collision2D coll)
	{
		if (coll.gameObject == shooter && !isSuicide)
		{
			return;
		}
		ReturnToPool();
	}

	private void OnTriggerEnter2D(Collider2D coll)
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
		PoolManager.Instance.PoolClientDestroy(gameObject);
		if (trail != null)
		{
			trail.Clear();
		}
	}
}