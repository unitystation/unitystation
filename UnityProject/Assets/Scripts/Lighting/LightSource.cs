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
public class LightSource : ObjectTrigger,IAPCPowered
{
	private const LightState InitialState = LightState.On;

	[Header("Generates itself if this is null:")]
	public GameObject mLightRendererObject;

	public Color customColor;

	[SerializeField]
	private bool isWithoutSwitch;

	public bool IsWithoutSwitch => isWithoutSwitch;
	public bool SwitchState { get; private set; }

	[SyncVar(hook =nameof(SyncLightState))]
	private LightState mState;
	private LightMountStates wallMount;
	public LightSwitchV2 relatedLightSwitch;

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

		wallMount = GetComponent<LightMountStates>();
		SwitchState = true;
		mState = InitialState;
	}

	private void OnDestroy()
	{
		UnSubscribeFromSwitchEvent();
	}

	public bool SubscribeToSwitchEvent(LightSwitchV2 lightSwitch)
	{
		if (lightSwitch == null) return false;
		UnSubscribeFromSwitchEvent();
		relatedLightSwitch = lightSwitch;
		lightSwitch.switchTriggerEvent += Trigger;
		return true;
	}

	public bool UnSubscribeFromSwitchEvent()
	{
		if (relatedLightSwitch == null) return false;
		relatedLightSwitch.switchTriggerEvent -= Trigger;
		relatedLightSwitch = null;
		return true;
	}

	public override void Trigger(bool newState)
	{
		SwitchState = newState;
		if (wallMount != null)
		{
			wallMount.SwitchChangeState(newState);
		}
		else
		{
			ServerChangeLightState(newState ? LightState.On : LightState.Off);
		}
	}

	public override void OnStartClient()
	{
		SyncLightState(mState, mState);
		base.OnStartClient();
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
			mLightRendererObject.SetActive(mState == LightState.On);
		}
	}

	void OnDrawGizmosSelected()
	{
		if (relatedLightSwitch == null) return;
		var sprite = GetComponentInChildren<SpriteRenderer>();
		if (sprite == null)
			return;

		//Highlighting all controlled lightSources
		Gizmos.color = new Color(1, 1, 0, 1);
		Gizmos.DrawLine(relatedLightSwitch.transform.position, gameObject.transform.position);
		Gizmos.DrawSphere(relatedLightSwitch.transform.position, 0.25f);

	}

	public void PowerNetworkUpdate(float Voltage)
	{

	}
	public void StateUpdate(PowerStates State)
	{
		switch (State)
		{
			case PowerStates.On:
				Trigger(true);
				return;
			case PowerStates.LowVoltage:
				Trigger(false);
				return;
			case PowerStates.OverVoltage:
				Trigger(true);
				return;
			default:
				Trigger(false);
				return;
		}
	}
}