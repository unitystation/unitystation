using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleScreenAnimator : MonoBehaviour, IAPCPowered
{
	private bool isOn;

	private bool isAnimating = false;
	public float timeBetweenFrames = 0.1f;
	public SpriteHandler SpriteHandler;
	public GameObject screenGlow;
	private int sIndex = 0;

	private void OnEnable()
	{
		SpriteHandler = this.GetComponentInChildren<SpriteHandler>();
	}

	private void ToggleOn(bool turnOn)
	{
		if (turnOn)
		{
			isOn = true;
			sIndex = 0;
			SpriteHandler.PushTexture();
		}
		else
		{
			isOn = false;
			SpriteHandler.PushClear();
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