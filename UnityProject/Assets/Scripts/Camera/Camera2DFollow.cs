using System.Collections;
using UnityEngine;

public class Camera2DFollow : MonoBehaviour
{
	//Static to make sure its the only cam in scene & for later access to camshake
	public static Camera2DFollow followControl;

	private readonly bool adjustPixel = false;
	private readonly float lookAheadMoveThreshold = 0.1f;
	private readonly float lookAheadReturnSpeed = 0.5f;

	private readonly float yOffSet = -0.5f; 

	private Vector3 cachePos;
	private Vector3 currentVelocity;

	public float starScroll = 0.03f;

	public float damping;

	private bool isShaking;

	private Vector3 lastTargetPosition;

	public GameObject listenerObj;

	private float lookAheadFactor;
	private Vector3 lookAheadPos;
	private float lookAheadSave;
	private float offsetZ = -1f;

	public Transform starsBackground;
	public float pixelAdjustment = 64f;

	//Shake Cam
	private float shakeAmount;

	public Transform target;
	public float xOffset = 4f;

    public GameObject stencilMask;

	[HideInInspector]
	public LightingSystem lightingSystem;

	[HideInInspector]
	public Camera cam;

	private void Awake()
	{
		if (followControl == null)
		{
			followControl = this;
			cam = GetComponent<Camera>();
			lightingSystem = GetComponent<LightingSystem>();
		}
		else
		{
			Destroy(gameObject);
		}
	}

	private void Start()
	{
		lookAheadSave = lookAheadFactor;
		if (target != null)
		{
			lastTargetPosition = target.position;
			offsetZ = (transform.position - target.position).z;
        }
		transform.parent = null;
		starsBackground.parent = null;
	}

	private void LateUpdate()
	{
		if(!PlayerManager.LocalPlayerScript){
			return;
		}
		//Really should sort out the load order and then we can remove this check:
		if(!PlayerManager.LocalPlayerScript.weaponNetworkActions){
			return;
		}
		if (target != null && !isShaking && !PlayerManager.LocalPlayerScript.weaponNetworkActions.lerping)
		{
			// only update lookahead pos if accelerating or changed direction
			float xMoveDelta = (target.position - lastTargetPosition).x;

			bool updateLookAheadTarget = Mathf.Abs(xMoveDelta) > lookAheadMoveThreshold;

			if (updateLookAheadTarget)
			{
				lookAheadPos = lookAheadFactor * Vector3.right * Mathf.Sign(xMoveDelta);
			}
			else
			{
				lookAheadPos = Vector3.MoveTowards(lookAheadPos, Vector3.zero, Time.deltaTime * lookAheadReturnSpeed);
			}

			Vector3 aheadTargetPos = target.position + lookAheadPos + Vector3.forward * offsetZ;

			aheadTargetPos.y += yOffSet;

			// Disabled for now since it introduced errors in to pixel perfect light renderer.
			//aheadTargetPos.x += xOffset;

			Vector3 newPos = Vector3.SmoothDamp(transform.position, aheadTargetPos, ref currentVelocity, damping);
	
			if (adjustPixel)
			{
				newPos.x = Mathf.RoundToInt(newPos.x * pixelAdjustment) / pixelAdjustment;
				newPos.y = Mathf.RoundToInt(newPos.y * pixelAdjustment) / pixelAdjustment;
			}
			transform.position = newPos;
			starsBackground.position = -newPos * starScroll;

			lastTargetPosition = target.position;
			if (stencilMask != null && stencilMask.transform.parent != target) {
				stencilMask.transform.parent = target;
				stencilMask.transform.localPosition = Vector3.zero;
			}
        }
	}

    public void SetXOffset(float offset)
    {
        xOffset = offset;
    }

    public void LookAheadTemp(float newLookAhead)
	{
		lookAheadFactor = newLookAhead;
		StartCoroutine(LookAheadSwitch());
	}

	public void ZeroStars(){
		starsBackground.transform.position = transform.position;
	}

	private IEnumerator LookAheadSwitch()
	{
		yield return new WaitForSeconds(2f);
		lookAheadFactor = lookAheadSave;
	}

	public void Shake(float amt, float length)
	{
		isShaking = true;
		cachePos = transform.position;
		shakeAmount = amt;
		InvokeRepeating("DoShake", 0, 0.01f);
		Invoke("StopShake", length);
	}

	private void DoShake()
	{
		if (shakeAmount > 0)
		{
			Vector3 camPos = transform.position;
			float offsetX = Random.value * shakeAmount * 2 - shakeAmount;
			float offsetY = Random.value * shakeAmount * 2 - shakeAmount;
			camPos.x += offsetX;
			camPos.y += offsetY;
			transform.position = camPos;
		}
	}

	private void StopShake()
	{
		isShaking = false;
		CancelInvoke("DoShake");
		transform.position = cachePos;
	}
}