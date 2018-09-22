using UnityEngine;

public abstract class BulletBehaviour : MonoBehaviour
{
	private BodyPartType bodyAim;
	public int damage = 25;
	public GameObject shooter;
	public DamageType damageType;
	public bool isSuicide = false;

	public TrailRenderer trail;

	private Rigidbody2D thisRigi;
	//	public BodyPartType BodyPartAim { get; private set; };

	public Vector2 Direction { get; private set; }

	public void Shoot(Vector2 dir, float angle, GameObject controlledByPlayer, BodyPartType targetZone = BodyPartType.CHEST)
	{
		StartShoot(dir, angle, controlledByPlayer, targetZone);
		OnShoot();
	}

	private void StartShoot(Vector2 dir, float angle, GameObject controlledByPlayer, BodyPartType targetZone)
	{
		Direction = dir;
		bodyAim = targetZone;
		shooter = controlledByPlayer;
		transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
		Vector3 startPos = new Vector3(dir.x, dir.y, transform.position.z);
		transform.position += startPos;
		thisRigi = GetComponent<Rigidbody2D>();
		thisRigi.AddForce(dir.normalized * 24f, ForceMode2D.Impulse);
	}

	//TODO  - change so that on call the bullets damage is set properly
	public abstract void OnShoot();

	private void OnCollisionEnter2D(Collision2D coll)
	{
		ReturnToPool();
	}

	private void OnTriggerEnter2D(Collider2D coll)
	{
		HealthBehaviour damageable = coll.GetComponent<HealthBehaviour>();

		if (coll.gameObject == shooter && !isSuicide)
		{
			return;
		}

		if (damageable == null || damageable.IsDead)
		{
			return;
		}
		damageable.ApplyDamage(shooter, damage, damageType, bodyAim);
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