using UnityEngine;
using System.Collections;
using PicoGames.VLS2D;

public class VLSLookAt : MonoBehaviour 
{
    public VLSLight vlsLight;
    public Transform target;

    private Vector3 targetPosition;

    void Start()
    {
        if (vlsLight == null)
            gameObject.GetComponent<VLSLight>();
    }

	void Update () 
    {
        if (vlsLight == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, Mathf.Infinity);
            if (hit.rigidbody != null)
                target = hit.rigidbody.transform;
        
        }

        if (target == null)
            return;

        targetPosition = Vector3.Lerp(targetPosition, target.transform.position, Time.deltaTime * 5f);
        vlsLight.LookAt(targetPosition);
    }
}
