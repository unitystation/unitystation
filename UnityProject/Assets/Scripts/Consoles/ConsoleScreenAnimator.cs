using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleScreenAnimator : MonoBehaviour, IAPCPowered
{
	private bool isOn;

	private bool isAnimating = false;
	public float timeBetweenFrames = 0.1f;
	public SpriteRenderer spriteRenderer;
	public GameObject screenGlow;
	public SpriteSheetAndData onSprites;
	public List<List<SpriteHandler.SpriteInfo>> sprites = new List<List<SpriteHandler.SpriteInfo>>();
	private int sIndex = 0;
	public float Delay;

	private void OnEnable()
	{
		CheckAnimatorState();
	}

	private void ToggleOn(bool turnOn)
	{
		if (turnOn)
		{
			isOn = true;
			sIndex = 0;
			CheckAnimatorState();
		}
		else
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

	public void PowerNetworkUpdate(float Voltage)
	{
	}

	void CheckAnimatorState()
	{
		if (!gameObject.activeInHierarchy) return;
		if (isOn && !isAnimating)
		{
			StartCoroutine(Animator());
		}
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

	IEnumerator Animator()
	{
		isAnimating = true;
		if (screenGlow != null)
		{
			screenGlow.SetActive(true);
		}

		while (isOn)
		{
			if (sprites.Count == 0)
			{
				if (onSprites.Texture != null)
				{
					sprites = SpriteFunctions.CompleteSpriteSetup(onSprites);
				}
			}

			spriteRenderer.sprite = sprites[0][sIndex].sprite;
			Delay = sprites[0][sIndex].waitTime;
			sIndex++;
			if (sIndex == sprites[0].Count)
			{
				sIndex = 0;
			}

			yield return WaitFor.Seconds(Delay);
		}

		isAnimating = false;
	}
}