using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BulletBehaviour : MonoBehaviour {

    private Rigidbody2D thisRigi;
	public string shooterName;


	public void Shoot(Vector2 dir, float angle, string controlledByPlayer){
		StartShoot(dir, angle, controlledByPlayer);
        OnShoot();
    }
		
	void StartShoot(Vector2 dir, float angle, string controlledByPlayer){
		shooterName = controlledByPlayer;
    	transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
		Vector3 startPos = new Vector3(dir.x, dir.y, transform.position.z);
		transform.position += startPos;
        thisRigi = GetComponent<Rigidbody2D>();
		thisRigi.AddForce(dir.normalized * 24f, ForceMode2D.Impulse);
    }

    public abstract void OnShoot();

    void OnCollisionEnter2D(Collision2D coll){
		PoolManager.PoolClientDestroy(gameObject);
    }
		
}
