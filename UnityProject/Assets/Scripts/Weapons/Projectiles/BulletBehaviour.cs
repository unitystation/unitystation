using UnityEngine;

public abstract class BulletBehaviour : MonoBehaviour
{
	private BodyPartType bodyAim;
	public int damage = 25;
	public GameObject shooter;
	public DamageType damageType;

	private Rigidbody2D thisRigi;
	//	public BodyPartType BodyPartAim { get; private set; };


	public void Shoot(Vector2 dir, float angle, GameObject controlledByPlayer, BodyPartType targetZone = BodyPartType.CHEST)
	{
		StartShoot(dir, angle, controlledByPlayer, targetZone);
		OnShoot();
	}

	private void StartShoot(Vector2 dir, float angle, GameObject controlledByPlayer, BodyPartType targetZone)
	{
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
		PoolManager.Instance.PoolClientDestroy(gameObject);
	}

	private void OnTriggerEnter2D(Collider2D coll)
	{
		HealthBehaviour damageable = coll.GetComponent<HealthBehaviour>();
		if (damageable == null || damageable.IsDead || coll.gameObject == shooter)
		{
			return;
		}
		damageable.ApplyDamage(shooter, damage, damageType, bodyAim);
		//		Debug.LogFormat("Hit {0} for {1} with HealthBehaviour! bullet absorbed", damageable.gameObject.name, damage);
		PoolManager.Instance.PoolClientDestroy(gameObject);
	}
}