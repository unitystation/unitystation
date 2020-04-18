using System;
using Light2D;
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

	private LightMountStates wallMount;

	public Color customColor;

	public bool SwitchState { get; private set; }

	[SyncVar(hook =nameof(SyncLightState))]
	private LightState mState;

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

		mState = InitialState;
	}
	public override void OnStartClient()
	{
		SyncLightState(mState, mState);
		base.OnStartClient();
	}

	public void SubscribeToSwitch(ref Action<bool> triggerEvent)
	{
		Debug.Log("Light source is subscribed");
		triggerEvent += OnSwitchInvokeEvent;
	}

	private void OnSwitchInvokeEvent(bool newState)
	{
		SwitchState = newState;
		if (wallMount != null)
		{
			if (wallMount.State == LightMountState.Broken ||
			    wallMount.State == LightMountState.MissingBulb)
			{
				return;
			}

			wallMount.SwitchChangeState(newState ? LightState.On : LightState.Off);
		}
		Trigger(newState);
	}

	public override void Trigger(bool iState)
	{
		ServerChangeLightState(iState ? LightState.On : LightState.Off);
	}

	private void SyncLightState(LightState oldState, LightState newState)
	{
		mState = newState;
		if (mLightRendererObject != null)
		{
			mLightRendererObject.SetActive(mState == LightState.On ? true : false);
		}
	}

	[Server]
	public void ServerChangeLightState(LightState newState)
	{
		mState = newState;
	}
}