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

	public bool switchState { get; private set; }

	[SyncVar]
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

	public void SubscribeToSwitch(ref Action<bool> triggerEvent)
	{
		Debug.Log("Light source is subscribed");
		triggerEvent += OnSwitchInvokeEvent;
	}

	private void OnSwitchInvokeEvent(bool newState)
	{
		switchState = newState;
		if (wallMount.State == LightMountStates.LightMountState.Broken ||
		    wallMount.State == LightMountStates.LightMountState.MissingBulb)
		{
			return;
		}

		wallMount.SwitchChangeState(newState ? LightState.On : LightState.Off);
		Trigger(newState);
	}

	public override void Trigger(bool iState)
	{
		mState = iState ? LightState.On : LightState.Off;
		if (mLightRendererObject != null)
		{
			mLightRendererObject.SetActive(iState);
		}
	}
}