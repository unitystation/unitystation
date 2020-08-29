using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleScreenAnimator : MonoBehaviour, IAPCPowered
{
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
	public GameObject ScreenGlow;

	private void ToggleOn(bool turnOn)
	{
		if (turnOn)
		{
			if (SpriteHandlerHere == null)
			{
				Logger.Log("Sprite handler is missing on" + this.gameObject);
				return;
			}
			SpriteHandlerHere.PushTexture();
		}
		else
		{
			SpriteHandlerHere.PushClear();
			if (ScreenGlow != null)
			{
				ScreenGlow.SetActive(false);
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