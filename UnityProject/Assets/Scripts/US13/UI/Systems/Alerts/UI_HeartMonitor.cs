using System;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using US13.HealthV2;
using US13.Managers;
using US13.Managers.UpdateManager;
using US13.Player;
using US13.UI.Core;
using US13.UI.Systems.CameraOverlays;
using Util;

namespace US13.UI.Systems.Alerts
{
	/// <summary>
	///     Controller for the heart monitor GUI
	/// </summary>
	public class UI_HeartMonitor : TooltipMonoBehaviour
	{
		public override string Tooltip => "health";

		private int currentSprite = 0;

		public Image pulseImg;

		[SerializeField] private Image bgImage = default;

		[SerializeField] public List<Spritelist> StatesSprites;

		[SerializeField] private Sprite[] statesBgImages = default;

		private int CurrentSpriteSet = 0;
		private float timeWait;
		private float blinkTimer;

		[Tooltip("Time between monitor bg blinks")] [SerializeField]
		private float criticalBlinkingTime = 0.5f;

		private float temporaryDamageValue = 0;
		private const float temporaryDamageDecay = 10f;
		private const float temporaryDamageMultiplier = 1.5f;

		private DateTime lastHitTime = DateTime.UtcNow;
		private const float temporaryDamageHangTimeSeconds = 1.5f;

		private float cachedPriorPercent = 0.0f;
		private bool attached = false;


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
			if (OverlayCrits.Instance == null) return;
			// Ensure crit overlay is reset to normal.
			OverlayCrits.Instance.SetNewHealthValue(100.0f);
		}

		//Managed by UpdateManager
		void UpdateMe()
		{
			if (PlayerManager.LocalPlayerScript == null || PlayerManager.LocalPlayerScript.IsNormal == false) return;

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

			if (blinkTimer >= criticalBlinkingTime)
			{
				blinkTimer = 0;
				// blinking bg when state is Crit
				if (CurrentSpriteSet == 4)
				{
					CurrentSpriteSet = 5;
					bgImage.sprite = statesBgImages[CurrentSpriteSet];
				}
				else if (CurrentSpriteSet == 5)
				{
					CurrentSpriteSet = 4;
					bgImage.sprite = statesBgImages[CurrentSpriteSet];
				}
			}
		}

		private void CheckHealth()
		{
			float healthPercentage = PlayerManager.LocalPlayerScript.playerHealth.HealthPercentage();
			if (cachedPriorPercent.Approx(healthPercentage) && temporaryDamageValue <= 0.1f) return;
			float damageTaken = cachedPriorPercent - healthPercentage;
			cachedPriorPercent = healthPercentage;

			if (damageTaken > 1.0f)
			{
				temporaryDamageValue += damageTaken * temporaryDamageMultiplier;
				temporaryDamageValue = Mathf.Min(85.0f, temporaryDamageValue);
				lastHitTime = DateTime.UtcNow;
			}

			if (DateTime.UtcNow - lastHitTime > TimeSpan.FromSeconds(temporaryDamageHangTimeSeconds))
			{
				temporaryDamageValue -= Time.deltaTime * temporaryDamageDecay;
				temporaryDamageValue = Mathf.Max(0.0f, temporaryDamageValue);
			}

			OverlayCrits.Instance.SetNewHealthValue(healthPercentage - temporaryDamageValue);

			switch (healthPercentage)
			{
				case >= 100.0f:
					CurrentSpriteSet = 0;
					break;
				case >= 66.67f:
					CurrentSpriteSet = 1;
					break;
				case >= 33.33f:
					CurrentSpriteSet = 2;
					break;
				case >= -66.67f:
					CurrentSpriteSet = 3;
					break;
				case > -100.0f:
					CurrentSpriteSet = 4;
					break;
				default:
					CurrentSpriteSet = 6;
					break;
			}

			// crit state has 2 sprite sets (blinking)
			if (CurrentSpriteSet is < 4 or > 5) SoundManager.ClientStop("Critstate", true);
			pulseImg.sprite = StatesSprites[CurrentSpriteSet].SP[currentSprite];
			bgImage.sprite = statesBgImages[CurrentSpriteSet];
		}
	}
}