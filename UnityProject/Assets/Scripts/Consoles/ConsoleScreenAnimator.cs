using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleScreenAnimator : MonoBehaviour, IAPCPowered
{
	private bool isOn;
	//whether we received our initial power state
	private bool stateInit;

	public float timeBetweenFrames = 0.1f;
	public SpriteRenderer spriteRenderer;
	public GameObject screenGlow;
	public Sprite[] onSprites;

	private int sIndex = 0;

	private void ToggleOn(bool turnOn)
	{
		if (turnOn && (!isOn || !stateInit))
		{
			isOn = true;
			sIndex = 0;
			StartCoroutine(Animator());
		}
		else if (!turnOn && (isOn || !stateInit))
		{
			isOn = false;
			//an unknown evil is enabling the spriteRenderer when power is initially off on client side
			//even though we disable it in this component. So to turn off the screen we just clear the sprite
			//rather than enabling / disabling the renderer.
			spriteRenderer.sprite = null;
			if (screenGlow != null)
			{
				screenGlow.SetActive(false);
			}
		}
	}

	public void PowerNetworkUpdate(float Voltage) {
	}

	public void StateUpdate(PowerStates State)
	{
		if (State == PowerStates.Off || State == PowerStates.LowVoltage)
		{
			ToggleOn(false);
		}
		else {
			ToggleOn(true);
		}

		if (!stateInit)
		{
			stateInit = true;
		}
	}

	IEnumerator Animator()
	{
		if (screenGlow != null)
		{
			screenGlow.SetActive(true);
		}

		while (isOn)
		{
			spriteRenderer.sprite = onSprites[sIndex];
			sIndex++;
			if (sIndex == onSprites.Length)
			{
				sIndex = 0;
			}
			yield return WaitFor.Seconds(timeBetweenFrames);
		}
	}
}