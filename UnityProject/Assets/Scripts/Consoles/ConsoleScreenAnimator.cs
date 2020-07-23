using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleScreenAnimator : MonoBehaviour, IAPCPowered
{
	private bool isOn;

	private bool isAnimating = false;
	public float timeBetweenFrames = 0.1f;

	public SpriteHandler SpriteHandlerHere
	{
		get
		{
			if (spriteHandler == null)
			{
				spriteHandler = this.GetComponentInChildren<SpriteHandler>();
			}
			return spriteHandler;
		}
		set
		{
			spriteHandler = value;
		}
	}

	[SerializeField]
	private SpriteHandler spriteHandler;
	public GameObject screenGlow;
	private int sIndex = 0;

	private void OnEnable()
	{

	}

	private void ToggleOn(bool turnOn)
	{
		if (turnOn)
		{
			isOn = true;
			sIndex = 0;
			if (SpriteHandlerHere == null)
			{
				Logger.Log("Sprite handler is missing on" + this.gameObject);
				return;
			}
			SpriteHandlerHere.PushTexture();
		}
		else
		{
			isOn = false;
			SpriteHandlerHere.PushClear();
			if (screenGlow != null)
			{
				screenGlow.SetActive(false);
			}
		}
	}

	public void PowerNetworkUpdate(float Voltage)
	{
	}


	public void StateUpdate(PowerStates State)
	{
		if (State == PowerStates.Off || State == PowerStates.LowVoltage)
		{
			ToggleOn(false);
		}
		else
		{
			ToggleOn(true);
		}
	}
}