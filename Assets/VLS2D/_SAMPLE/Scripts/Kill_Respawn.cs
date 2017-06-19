using UnityEngine;
using System.Collections;

public class Kill_Respawn : MonoBehaviour
{
    public Vector3 respawnPosition = Vector3.zero;

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(respawnPosition, 1f);
    }

    void OnCollisionEnter2D(Collision2D _col)
    {
        _col.gameObject.transform.position = respawnPosition;
    }
}
