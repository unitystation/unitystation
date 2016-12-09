using UnityEngine;
using System.Collections;
using PlayGroup;


public class Camera2DFollow: MonoBehaviour {

    //Static to make sure its the only cam in scene & for later access to camshake
    public static Camera2DFollow followControl;

    public Transform target;

    public float damping = 1;

    public float lookAheadFactor = 3;
    private float lookAheadSave;
    public float lookAheadReturnSpeed = 0.5f;
    public float lookAheadMoveThreshold = 0.1f;
    public float yOffSet = 0f;

    bool isSearching = false;

    public float offsetZ = -1f;
    Vector3 lastTargetPosition;
    Vector3 currentVelocity;
    Vector3 lookAheadPos;

    float nextTimeToSearch = 0;


    private bool zooming = false;
    private float newZpos;
    private float zoomSpeed;
    private float zoomLerp = 0f;
    private float fromZ;

    // Y Pos Lerp
    private bool yLerp = false;
    private float newYposRestrict;
    private float newYLerpSpeed;
    private float yLerpVal;
    private float fromY;

    void Awake() {
        if(followControl == null) {

            followControl = this;

        }
    }
    
    void Start() {
        lookAheadSave = lookAheadFactor;
        if(target != null) {
            lastTargetPosition = target.position;
            offsetZ = (transform.position - target.position).z;
        }
        transform.parent = null;
    }

    void LateUpdate() {
        if(target != null) {
            // only update lookahead pos if accelerating or changed direction
            float xMoveDelta = (target.position - lastTargetPosition).x;

            bool updateLookAheadTarget = Mathf.Abs(xMoveDelta) > lookAheadMoveThreshold;

            if(updateLookAheadTarget) {
                lookAheadPos = lookAheadFactor * Vector3.right * Mathf.Sign(xMoveDelta);
            } else {
                lookAheadPos = Vector3.MoveTowards(lookAheadPos, Vector3.zero, Time.deltaTime * lookAheadReturnSpeed);
            }

            Vector3 aheadTargetPos = target.position + lookAheadPos + Vector3.forward * offsetZ;
            aheadTargetPos.y += yOffSet;
            Vector3 newPos = Vector3.SmoothDamp(transform.position, aheadTargetPos, ref currentVelocity, damping);
            
            transform.position = newPos;

            // adjust to 32 pixel steps
            gameObject.transform.position = new Vector3((Mathf.RoundToInt(transform.position.x * 32f) / 32f),
                                                        (Mathf.RoundToInt(transform.position.y * 32f) / 32f),
                                                        transform.position.z);

            lastTargetPosition = target.position;
        }
    }


    public void LookAheadTemp(float newLookAhead) {

        lookAheadFactor = newLookAhead;
        StartCoroutine(LookAheadSwitch());
    }

    //COROUTINES
    IEnumerator LookAheadSwitch() {
        yield return new WaitForSeconds(2f);
        lookAheadFactor = lookAheadSave;
    }
}
