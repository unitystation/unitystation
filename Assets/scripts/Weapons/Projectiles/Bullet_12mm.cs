using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Bullet_12mm : BulletBehaviour
{

    public override void OnShoot()
    {
        if (isServer)
        {
            GameObject casing = GameObject.Instantiate(Resources.Load("BulletCasing") as GameObject, transform.position, Quaternion.identity);
            NetworkServer.Spawn(casing);
            RpcShootSFX();
        }
    }

    [ClientRpc]
    void RpcShootSFX(){
        SoundManager.PlayAtPosition("ShootSMG", transform.position);
    }
}
