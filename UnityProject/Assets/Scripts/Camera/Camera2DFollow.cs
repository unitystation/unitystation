using System.Collections;
using UnityEngine;

public class Camera2DFollow : MonoBehaviour
{
	//Static to make sure its the only cam in scene & for later access to camshake
	public static Camera2DFollow followControl;

	private readonly bool adjustPixel = false;
	private readonly float lookAheadMoveThreshold = 0.05f;
	private readonly float lookAheadReturnSpeed = 0.5f;

	private readonly float yOffSet = -0.5f;

	private Vector3 cachePos;
	private Vector3 currentVelocity;

	//destination we will recoil too - offset from current player position
	private Vector2 recoilOffsetDestination;
	//current offset from player position - will grow to reach recoilDestination then recover.
	private Vector2 recoilOffset;
	//how much time left until we reach our recoil destination
	private float recoilTime;
	//how much time left until we recover fully from recoil
	private float recoverTime;
	private CameraRecoilConfig activeRecoilConfig;


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

	public void SetCameraXOffset()
	{
		float xOffSet =
			(transform.position.x - Camera.main.ScreenToWorldPoint(UIManager.Hands.transform.position).x) * 1.38f;

		followControl.SetXOffset(xOffSet);
	}

	private void Update()
	{
		if(!PlayerManager.LocalPlayerScript){
			return;
		}
		//Really should sort out the load order and then we can remove this check:
		if(!PlayerManager.LocalPlayerScript.IsGhost && !PlayerManager.LocalPlayerScript.weaponNetworkActions){
			return;
		}
		if (target != null && !isShaking)
		{
			if ( (int)target.position.z == -100 )
			{ //not following target on invalid coordinates
				return;
			}

			recoilOffset = Vector3.zero;
			//if  we are recoiling, adjust target position
			if (recoilOffsetDestination != Vector2.zero)
			{
				//if we have recoil time left, continue to lerp to that
				if (recoilTime > 0)
				{
					recoilTime = Mathf.Max(recoilTime - Time.deltaTime, 0);
					recoilOffset = recoilOffsetDestination * ((activeRecoilConfig.RecoilDuration - recoilTime) / activeRecoilConfig.RecoilDuration);
					if (recoilTime == 0)
					{
						recoverTime = activeRecoilConfig.RecoveryDuration;
					}
				}
				else if (recoverTime > 0)
				{
					recoverTime = Mathf.Max(recoverTime - Time.deltaTime, 0);
					recoilOffset = recoilOffsetDestination - (recoilOffsetDestination * ((activeRecoilConfig.RecoveryDuration - recoverTime) / activeRecoilConfig.RecoveryDuration));
					if (recoverTime == 0)
					{
						recoilOffsetDestination = Vector2.zero;
					}
				}

			}


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

			// ReSharper disable once HONK1002
			transform.position = newPos + (Vector3)recoilOffset;
			listenerObj.transform.position = target.position;
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
		yield return WaitFor.Seconds(2f);
		lookAheadFactor = lookAheadSave;
	}

	/// <summary>
	/// Cause recoil in the specified direction, no effect if shaking
	/// </summary>
	/// <param name="dir">direction to recoil</param>
	/// <param name="cameraRecoilConfig">configuration for the recoil</param>
	public void Recoil(Vector2 dir, CameraRecoilConfig cameraRecoilConfig)
	{
		if (isShaking) return;
		this.activeRecoilConfig = cameraRecoilConfig;
		if (recoilOffsetDestination != Vector2.zero)
		{
			//recoil from current position
			Vector2 newRecoilOffsetDestination = recoilOffset + dir.normalized * activeRecoilConfig.Distance;
			//cap it so we don't recoil further than the maximum from our origin
			if (newRecoilOffsetDestination.magnitude > activeRecoilConfig.Distance)
			{
				newRecoilOffsetDestination = dir.normalized * activeRecoilConfig.Distance;
			}

			recoilOffsetDestination = newRecoilOffsetDestination;

			//ensure we will reach our max recoil sooner based on how close we already our to our recoil destination
			float distanceFromDestination = (recoilOffset - recoilOffsetDestination).magnitude;
			float distanceFromOrigin = recoilOffsetDestination.magnitude;

			recoilTime = activeRecoilConfig.RecoilDuration * (distanceFromDestination / distanceFromOrigin);
		}
		else
		{
			//begin recoiling
			recoilOffsetDestination = dir.normalized * activeRecoilConfig.Distance;
			recoilTime = activeRecoilConfig.RecoilDuration;
		}
	}

	/// <summary>
	/// Randomly shake for the specified duration.
	/// </summary>
	/// <param name="amt"></param>
	/// <param name="length"></param>
	public void Shake(float amt, float length)
	{
		//cancel recoil if it is happening
		if (recoilOffsetDestination != Vector2.zero)
		{
			recoilOffsetDestination = Vector2.zero;
			recoilTime = 0;
			recoverTime = 0;
			transform.position = cachePos;
		}

		isShaking = true;
		cachePos = transform.position;
		shakeAmount = amt;
		InvokeRepeating(nameof( DoShake ), 0, 0.01f);
		Invoke(nameof( StopShake ), length);

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
		CancelInvoke(nameof( DoShake ));
		transform.position = cachePos;
	}
}