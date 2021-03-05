﻿using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class EmergencyLightAnimator : NetworkBehaviour
{
	public Sprite[] sprites;

	public float animateTime = 0.4f;
	private float timeElapsedSprite = 0;
	private int currentSprite = 0;
	public float rotateSpeed = 40f;

	private SpriteRenderer spriteRenderer;
	private LightSource lightSource;

	private void OnEnable()
	{
		lightSource = GetComponent<LightSource>();
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
	}

	private void OnDisable()
	{
		StopAnimation();
	}

	public void StartAnimation()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	public void StopAnimation()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}


	protected virtual void UpdateMe()
	{
		AnimateLight();
	}

	private void AnimateLight()
	{
		if (lightSource == null || lightSource.mLightRendererObject == null ||
		    lightSource.mLightRendererObject.transform == null)
		{
			Debug.LogError($"{gameObject.name} had something null");
			StopAnimation();
			return;
		}

		lightSource.mLightRendererObject.transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime, Space.World);
	}
	private void AnimateSprite()
	{
		timeElapsedSprite += Time.deltaTime;
		if (timeElapsedSprite >= animateTime)
		{
			spriteRenderer.sprite = sprites[currentSprite];
			if (sprites.Length == currentSprite)
			{
				currentSprite = 0;
			}
			else
			{
				currentSprite++;
			}

			timeElapsedSprite = 0;
		}
	}
}