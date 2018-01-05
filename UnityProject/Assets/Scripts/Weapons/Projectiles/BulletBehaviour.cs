using UnityEngine;

public abstract class BulletBehaviour : MonoBehaviour
{
	private BodyPartType bodyAim;
	public int damage = 25;
    public DamageType damageType = DamageType.BRUTE;
    public GameObject shooter;

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


    //TODO
    /**
     * This function should be converted to a standard non abstract function as to  
     *      Move forward with making the guns be the deteriminers of damage. This means you will
     *      go by Gun's damage value and type rather then the bullet. Note we probably want to store/use the weapon that
     *      does the firing here some how. 
    **/
    public abstract void OnShoot();
    


	private void OnCollisionEnter2D(Collision2D coll)
	{
		PoolManager.Instance.PoolClientDestroy(gameObject);
	}

	private void OnTriggerEnter2D(Collider2D coll)
	{
		HealthBehaviour damageable = coll.GetComponent<HealthBehaviour>();
		if (damageable == null || damageable.IsDead )
		{
			return;
		}
		damageable.ApplyDamage(shooter, damage, damageType, bodyAim);
		//		Debug.LogFormat("Hit {0} for {1} with HealthBehaviour! bullet absorbed", damageable.gameObject.name, damage);
		PoolManager.Instance.PoolClientDestroy(gameObject);
	}
}