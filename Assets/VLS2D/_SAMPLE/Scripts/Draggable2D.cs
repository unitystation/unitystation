/**************************************************************************************************
 *      Draggable2D (Physics Based)
 *      Adapted by: Jacob Fletcher (www.picogames.com)
 *      Last Edit: January 3, 2015 
 *      Original: http://forum.unity3d.com/threads/rigidbody2d-dragable-script.212168/
 **************************************************************************************************/ 
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera)), DisallowMultipleComponent]
public class Draggable2D : MonoBehaviour
{
    public float distance = 0.05f;
    public float damper = 0.5f;
    public float frequency = 12.0f;
    public float drag = 1f;
    public float angularDrag = 5f;
    public bool showLineRenderer = true;
    public bool attachToCenterOfMass = false;
    public LayerMask selectableLayers = 0;

    private Vector2 hitOffset = Vector2.zero;
    private Camera thisCamera;
    private SpringJoint2D springJoint;

    //private LineRenderer line;
    //private static Material lineMaterial;
    //private static Material GetLineMaterial()
    //{
    //    if(lineMaterial == null)
    //    {
    //        lineMaterial = new Material(Shader.Find("Transparent/Diffuse"));
    //        lineMaterial.hideFlags = HideFlags.HideAndDontSave;

    //        lineMaterial.color = new Color(0.25f, 0.25f, 0.2f, 1f);
    //    }

    //    return lineMaterial;
    //}

    void Awake()
    {
        thisCamera = gameObject.GetComponent<Camera>();
    }

    void Update()
    {  
 
            if (!Input.GetMouseButtonDown (0))
                    return;

            RaycastHit2D hit = Physics2D.Raycast(thisCamera.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, Mathf.Infinity, selectableLayers);

            if (hit.collider != null && hit.collider.GetComponent<Rigidbody2D>() != null && hit.collider.GetComponent<Rigidbody2D>().isKinematic == false) 
            {
                if (!springJoint)
                {
                    GameObject go = new GameObject("Rigidbody2D Dragger");
                    go.hideFlags = HideFlags.HideAndDontSave;

                    Rigidbody2D body = go.AddComponent<Rigidbody2D>();
                    springJoint = go.AddComponent<SpringJoint2D>();               
                    body.isKinematic = true;
                } 
                springJoint.transform.position = hit.point;

                if (!attachToCenterOfMass)
                    hitOffset = hit.rigidbody.transform.InverseTransformPoint(hit.point);
                else
                    hitOffset = Vector2.zero;

                springJoint.distance = distance;
                springJoint.dampingRatio = damper;
                springJoint.connectedBody = hit.rigidbody;           

                StartCoroutine ("DragObject", hit.fraction);
            }
 
    }


    IEnumerator DragObject(float distance)
    {
        float oldDrag = springJoint.connectedBody.drag;
        float oldAngularDrag = springJoint.connectedBody.angularDrag;

        //line = springJoint.gameObject.AddComponent<LineRenderer>();
        //line.SetWidth(0.1f, 0.1f);
        //line.material = GetLineMaterial();
        //line.gameObject.layer = gameObject.layer;

        springJoint.connectedBody.drag = drag;
        springJoint.connectedBody.angularDrag = angularDrag;

        springJoint.connectedAnchor = hitOffset;

        Ray ray;
        while (Input.GetMouseButton(0))
        {
            ray = thisCamera.ScreenPointToRay(Input.mousePosition);
            springJoint.transform.position = ray.GetPoint(distance);

            Debug.DrawLine(springJoint.connectedBody.transform.TransformPoint(springJoint.connectedAnchor), springJoint.transform.position);

            //line.SetPosition(0, springJoint.connectedBody.transform.TransformPoint(springJoint.connectedAnchor));
            //line.SetPosition(1, new Vector3(springJoint.transform.position.x, springJoint.transform.position.y, 0));
            
            yield return null;
        }
        
        if (springJoint.connectedBody)
        {
            springJoint.connectedBody.drag = oldDrag;
            springJoint.connectedBody.angularDrag = oldAngularDrag;
            springJoint.connectedBody = null;
        }

        //DestroyImmediate(line);
    }
}