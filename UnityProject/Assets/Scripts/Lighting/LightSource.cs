using System;
using System.Runtime.Remoting.Messaging;
using Light2D;
using Lighting;
using Mirror;
using UnityEngine;

public enum LightState
{
	None = 0,
	On,
	Off,
	Emergency
}

[ExecuteInEditMode]
public class LightSource : ObjectTrigger,IAPCPowered, IServerDespawn
{
	[SerializeField]
	private LightState InitialState = LightState.On;

	[Header("Generates itself if this is null:")]
	public GameObject mLightRendererObject;

	public Color lightStateColorOn = new Color(0.7264151f, 0.7264151f, 0.7264151f, 0.8f);

	public Color lightStateColorEmergency = new Color(0.7264151f, 0, 0, 0.8f);

	[SerializeField]
	private bool isWithoutSwitch;

	public bool IsWithoutSwitch => isWithoutSwitch;
	public bool SwitchState { get; private set; }

	[SyncVar(hook =nameof(SyncLightState))]
	private LightState mState;
	private LightMountStates wallMount;
	private LightSprite lightSprite;
	public LightSwitchV2 relatedLightSwitch;

	void Start()
	{
		if (!Application.isPlaying)
		{
			return;
		}
		lightSprite.Color = lightStateColorOn;
	}

	private void Awake()
	{
		if (!Application.isPlaying)
		{
			return;
		}

		EnsureLightsInit();
		wallMount = GetComponent<LightMountStates>();
		SwitchState = true;
		mState = InitialState;
	}

	private void EnsureLightsInit()
	{
		if (mLightRendererObject == null)
		{
			mLightRendererObject = LightSpriteBuilder.BuildDefault(gameObject, new Color(0, 0, 0, 0), 12);
		}

		lightSprite = mLightRendererObject.GetComponent<LightSprite>();
		if (lightStateColorOn == new Color(0, 0, 0, 0))
		{
			lightStateColorOn = new Color(0.7264151f, 0.7264151f, 0.7264151f, 0.8f);
		}
		if (lightStateColorEmergency == new Color(0, 0, 0, 0))
		{
			lightStateColorEmergency = new Color(0.7264151f, 0, 0, 0.8f);
		}
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
			case PowerStates.Off:
				SyncLightState(mState, LightState.Emergency);
				return;
		}
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
		ChangeColorState(newState);
	}

	private void ChangeColorState(LightState newState)
	{
		if (mLightRendererObject != null)
		{
			switch (newState)
			{
				case LightState.On:
					lightSprite.Color = lightStateColorOn;
					mLightRendererObject.SetActive(true);
					break;
				case LightState.Emergency:
					lightSprite.Color = lightStateColorEmergency;
					mLightRendererObject.SetActive(true);
					break;
				case LightState.Off:
					lightSprite.Color = lightStateColorOn;
					mLightRendererObject.SetActive(false);
					break;
			}
		}
	}

	void OnDrawGizmosSelected()
	{

		var sprite = GetComponentInChildren<SpriteRenderer>();
		if (sprite == null)
			return;
		if (relatedLightSwitch == null)
		{
			if (isWithoutSwitch) return;
			Gizmos.color = new Color(1, 0.5f, 1, 1);
			Gizmos.DrawSphere(sprite.transform.position, 0.20f);
			return;
		}
		//Highlighting all controlled lightSources
		Gizmos.color = new Color(1, 1, 0, 1);
		Gizmos.DrawLine(relatedLightSwitch.transform.position, gameObject.transform.position);
		Gizmos.DrawSphere(relatedLightSwitch.transform.position, 0.25f);

	}

	public void PowerNetworkUpdate(float Voltage)
	{

	}


	public void OnDespawnServer(DespawnInfo info)
	{
		UnSubscribeFromSwitchEvent();
	}
}