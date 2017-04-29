using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class BulletBehaviour : NetworkBehaviour {

    private Rigidbody2D thisRigi;


    public void Shoot(Vector2 dir, float angle){
        RpcShoot(dir, angle);
        OnShoot();
    }

    [ClientRpc]
    void RpcShoot(Vector2 dir, float angle){
    transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
		Vector3 startPos = new Vector3(dir.x, dir.y, transform.position.z);
		transform.position += startPos;
        thisRigi = GetComponent<Rigidbody2D>();
		thisRigi.AddForce(dir.normalized * 24f, ForceMode2D.Impulse);
    }

    public abstract void OnShoot();

    void OnCollisionEnter2D(Collision2D coll){
        Destroy(gameObject);
        Debug.Log("Bullet hit: " + coll.gameObject.name);
    }
}
