using System;
using UnityEngine;

public class OutsideFlashingLight : MonoBehaviour
{
	public float flashWaitTime = 1f;

	public GameObject lightSource;
	public SpriteRenderer lightSprite;
	public Color spriteOffCol;
	public Color spriteOnCol;
	private float timeCount;

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	private void UpdateMe()
	{
		timeCount += Time.deltaTime;
		if (timeCount >= flashWaitTime)
		{
			timeCount = 0f;
			SwitchLights();
		}
	}

	private void SwitchLights()
	{
		if (!lightSource.activeSelf)
		{
			lightSource.SetActive(true);
			lightSprite.color = spriteOnCol;
		}
		else
		{
			lightSource.SetActive(false);
			lightSprite.color = spriteOffCol;
		}
	}
}