using System;
using Light2D;
using Lighting;
using Mirror;
using UnityEngine;

public enum LightState
{
	None = 0,
	On,
	Off,
}

[ExecuteInEditMode]
public class LightSource : ObjectTrigger
{
	private const LightState InitialState = LightState.On;

	[Header("Generates itself if this is null:")]
	public GameObject mLightRendererObject;

	public Color customColor;

	public bool SwitchState { get; private set; }

	[SyncVar(hook =nameof(SyncLightState))]
	private LightState mState;

	private APCPoweredDevice poweredDevice;
	private LightMountStates wallMount;

	[SerializeField]
	private LightSwitchV2 relatedLightSwitch;

	public LightSwitchV2 RelatedLightSwitch { get; private set; }

	void Start()
	{
		if (!Application.isPlaying)
		{
			return;
		}

		Color _color;

		if (customColor == new Color(0, 0, 0, 0))
		{
			_color = new Color(0.7264151f, 0.7264151f, 0.7264151f, 0.8f);
		}
		else
		{
			_color = customColor;
		}

		mLightRendererObject.GetComponent<LightSprite>().Color = _color;
	}

	private void Awake()
	{
		if (!Application.isPlaying)
		{
			return;
		}

		if (mLightRendererObject == null)
		{
			mLightRendererObject = LightSpriteBuilder.BuildDefault(gameObject, new Color(0, 0, 0, 0), 12);
		}

		poweredDevice = GetComponent<APCPoweredDevice>();
		if(poweredDevice != null)
			poweredDevice.OnPowerStateChangeEvent += PowerStateChange;
		wallMount = GetComponent<LightMountStates>();

		mState = InitialState;
	}

	private void PowerStateChange(PowerStates newState)
	{
		switch (newState)
		{
			case PowerStates.On:
				Trigger(true);
				return;
			case PowerStates.LowVoltage:
				return;
			case PowerStates.OverVoltage:
				return;
			default:
				Trigger(false);
				return;
		}
	}
	public void EnsureInit()
	{
		wallMount = GetComponent<LightMountStates>();
		SwitchState = true;
	}
	public override void OnStartClient()
	{
		SyncLightState(mState, mState);
		base.OnStartClient();
	}

	private void OnDestroy()
	{
		UnSubscribeFromSwitchEvent();
	}

	public bool SubscribeToSwitchEvent(LightSwitchV2 lightSwitch)
	{
		if (lightSwitch == null) return false;
		UnSubscribeFromSwitchEvent();
		Debug.Log("Light source is subscribed");
		SetRelatedSwitch(lightSwitch);
		lightSwitch.switchTriggerEvent += Trigger;
		return true;
	}

	public void SetRelatedSwitch(LightSwitchV2 lightSwitch)
	{
		relatedLightSwitch = lightSwitch;
	}

	public bool UnSubscribeFromSwitchEvent()
	{
		if (relatedLightSwitch == null) return false;
		Debug.Log("Light source is UnSubscribed");
		relatedLightSwitch.switchTriggerEvent -= Trigger;
		ClearRelatedSwitch();
		return true;
	}

	public void ClearRelatedSwitch()
	{
		relatedLightSwitch = null;
	}

	public override void Trigger(bool newState)
	{
		SwitchState = newState;
		if (wallMount != null)
		{
			wallMount.SwitchChangeState(newState);
		}
		ServerChangeLightState(newState ? LightState.On : LightState.Off);
	}

	[Server]
	public void ServerChangeLightState(LightState newState)
	{
		mState = newState;
	}

	private void SyncLightState(LightState oldState, LightState newState)
	{
		mState = newState;
		if (mLightRendererObject != null)
		{
			mLightRendererObject.SetActive(mState == LightState.On ? true : false);
		}
	}



	void OnDrawGizmosSelected()
	{
		if (relatedLightSwitch == null) return;
		var sprite = GetComponentInChildren<SpriteRenderer>();
		if (sprite == null)
			return;

		//Highlighting all controlled lightSources
		Gizmos.color = new Color(0, 1, 0, 1);
		Gizmos.DrawLine(relatedLightSwitch.transform.position, gameObject.transform.position);
		Gizmos.DrawSphere(relatedLightSwitch.transform.position, 0.25f);

	}
}