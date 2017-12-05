using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BulletCasing : NetworkBehaviour
{
    public GameObject spriteObj;

    void Start()
    {
        if (isServer)
        {
            Vector2 dir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            spriteObj.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            Vector2 ranLocalPos = new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f));
            spriteObj.transform.localPosition = ranLocalPos;
        }
    }
}
