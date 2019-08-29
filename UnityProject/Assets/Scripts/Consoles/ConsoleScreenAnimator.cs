using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleScreenAnimator : MonoBehaviour, IAPCPowered
{
	private bool isOn = true;

	public float timeBetweenFrames = 0.1f;
	public SpriteRenderer spriteRenderer;
	public GameObject screenGlow;
	public Sprite[] onSprites;

	private int sIndex = 0;

	private void Start()
	{
		ToggleOn(isOn, true);
	}

	private void ToggleOn(bool turnOn, bool forceToggle = false)
	{
		if (turnOn && (!isOn || forceToggle))
		{
			isOn = true;
			sIndex = 0;
			StartCoroutine(Animator());
		}
		else if (!turnOn && (isOn || forceToggle))
		{
			isOn = false;
			spriteRenderer.enabled = false;
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
	}

	IEnumerator Animator()
	{
		if (screenGlow != null)
		{
			screenGlow.SetActive(true);
		}
		spriteRenderer.enabled = true;
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