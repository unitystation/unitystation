using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;

public abstract class BulletBehaviour : MonoBehaviour
{

    private Rigidbody2D thisRigi;
    public string shooterName;
    public int damage = 25;
    private BodyPartType bodyAim;
    //	public BodyPartType BodyPartAim { get; private set; };


    public void Shoot(Vector2 dir, float angle, string controlledByPlayer, BodyPartType targetZone = BodyPartType.CHEST)
    {
        StartShoot(dir, angle, controlledByPlayer, targetZone);
        OnShoot();
    }

    private void StartShoot(Vector2 dir, float angle, string controlledByPlayer, BodyPartType targetZone)
    {
        bodyAim = targetZone;
        shooterName = controlledByPlayer;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        Vector3 startPos = new Vector3(dir.x, dir.y, transform.position.z);
        transform.position += startPos;
        thisRigi = GetComponent<Rigidbody2D>();
        thisRigi.AddForce(dir.normalized * 24f, ForceMode2D.Impulse);
    }

    public abstract void OnShoot();

    void OnCollisionEnter2D(Collision2D coll)
    {
        PoolManager.Instance.PoolClientDestroy(gameObject);

    }

    private void OnTriggerEnter2D(Collider2D coll)
    {
        var damageable = coll.GetComponent<HealthBehaviour>();
        if (damageable == null ||
             damageable.IsDead ||
             damageable.gameObject.name.Equals(shooterName)) return;
        damageable.ApplyDamage(shooterName, damage, DamageType.BRUTE, bodyAim);
        //		Debug.LogFormat("Hit {0} for {1} with HealthBehaviour! bullet absorbed", damageable.gameObject.name, damage);
        PoolManager.Instance.PoolClientDestroy(gameObject);
    }
}
