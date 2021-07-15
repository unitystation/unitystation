﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
///     Controller for the heart monitor GUI
/// </summary>
public class UI_HeartMonitor : TooltipMonoBehaviour
{
	public override string Tooltip => "health";

	private int currentSprite = 0;

	//FIXME doing overlayCrit update based off heart monitor for time being
	public OverlayCrits overlayCrits;

	public Image pulseImg;

	[SerializeField] private Image bgImage = default;

	[SerializeField]
	public List<Spritelist> StatesSprites;

	[SerializeField] private Sprite[] statesBgImages = default;

	private int CurrentSpriteSet = 0;
	private float timeWait;
	private float blinkTimer;

	[Tooltip("Time between monitor bg blinks")]
	[SerializeField]
	private float criticalBlinkingTime = 0.5f;
	private float overallHealthCache = 100;


	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnSceneChange;
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnSceneChange;
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	private void OnSceneChange(Scene prev, Scene next)
	{
		// Ensure crit overlay is reset to normal.
		overlayCrits.SetState(OverlayState.normal);
	}

	//Managed by UpdateManager
	void UpdateMe()
	{
		if (PlayerManager.LocalPlayer == null || PlayerManager.LocalPlayerScript.IsGhost) return;

		CheckHealth();
		timeWait += Time.deltaTime;
		blinkTimer += Time.deltaTime;
		if (timeWait > 0.05f)
		{
			if (currentSprite != 27)
			{
				pulseImg.sprite = StatesSprites[CurrentSpriteSet].SP[currentSprite];
				currentSprite++;
				timeWait = 0f;
			}
			else
			{
				pulseImg.sprite = StatesSprites[CurrentSpriteSet].SP[currentSprite];
				if (timeWait > 2f)
				{
					currentSprite = 0;
					timeWait = 0f;
				}
			}
		}

		if(blinkTimer >= criticalBlinkingTime)
		{
			blinkTimer = 0;
			// blinking bg when state is Crit
			if (CurrentSpriteSet == 4)
			{
				CurrentSpriteSet = 5;
				bgImage.sprite = statesBgImages[CurrentSpriteSet];
			}
			else if(CurrentSpriteSet == 5)
			{
				CurrentSpriteSet = 4;
				bgImage.sprite = statesBgImages[CurrentSpriteSet];
			}
		}
	}

	private void CheckHealth()
	{
		if (PlayerManager.LocalPlayerScript.playerHealth.OverallHealth == overallHealthCache)
		{
			return;
		}
		float maxHealth = PlayerManager.LocalPlayerScript.playerHealth.MaxHealth;
		overallHealthCache = PlayerManager.LocalPlayerScript.playerHealth.OverallHealth;

		float HealthPercentage = overallHealthCache / maxHealth;
		for (int i = 0; i < 1; i++)
		{
			if (HealthPercentage >= 1)
			{
				CurrentSpriteSet = 0;
				overlayCrits.SetState(HealthPercentage);
				break;
			}
			else if (HealthPercentage >= 0.66f)
			{
				CurrentSpriteSet = 1;
				overlayCrits.SetState(HealthPercentage);
				break;
			}
			else if (HealthPercentage >= 0.33f)
			{
				CurrentSpriteSet = 2;
				overlayCrits.SetState(HealthPercentage);
				break;
			}
			else if (HealthPercentage > 0)
			{
				CurrentSpriteSet = 3;
				overlayCrits.SetState(HealthPercentage);
				break;
			}

			HealthPercentage = overallHealthCache / 100f;

			if (HealthPercentage > -0.66f)
			{
				CurrentSpriteSet = 3;
			}
			else if (HealthPercentage > -1)
			{
				// crit state has 2 sprite sets (blinking)
				// so next state is 6 instead of 5
				CurrentSpriteSet = 4;
			}
			else
			{
				CurrentSpriteSet = 6;

			}
			overlayCrits.SetState(HealthPercentage);
		}



		// crit state has 2 sprite sets (blinking)
		if (CurrentSpriteSet != 4 && CurrentSpriteSet != 5)
			SoundManager.Stop("Critstate");

		pulseImg.sprite = StatesSprites[CurrentSpriteSet].SP[currentSprite];
		bgImage.sprite = statesBgImages[CurrentSpriteSet];
	}
}
