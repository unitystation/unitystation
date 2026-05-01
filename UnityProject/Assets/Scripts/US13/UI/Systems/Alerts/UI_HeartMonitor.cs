using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
		private const float temporaryDamageDecay = 2000;
		private const float temporaryDamageMultiplier = 50f;


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
			float maxHealth = PlayerManager.LocalPlayerScript.playerHealth.MaxHealth;
			float damageDelta = Math.Max(0, PlayerManager.LocalPlayerScript.playerHealth.OverallHealth - PlayerManager.LocalPlayerScript.playerHealth.OverallHealth);
			float healthPercentage = PlayerManager.LocalPlayerScript.playerHealth.HealthPercentage();

			switch (healthPercentage)
			{
				case >=100.0f:
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

			//Causes a brief flash upon taking damage
			temporaryDamageValue += damageDelta * temporaryDamageMultiplier;
			temporaryDamageValue = Math.Min(85, temporaryDamageValue);
			if (temporaryDamageValue > 0) //Is damage taken not healing
			{
				temporaryDamageValue -= (temporaryDamageDecay * Time.deltaTime); //Reduce temp damage by decay
				temporaryDamageValue = Math.Max(0, temporaryDamageValue);

				healthPercentage -= (100 * temporaryDamageValue) / maxHealth; //Apply this false health loss to percentage for crit overlay
			}

			OverlayCrits.Instance.SetNewHealthValue(healthPercentage);

			// crit state has 2 sprite sets (blinking)
			if (CurrentSpriteSet != 4 && CurrentSpriteSet != 5)
				SoundManager.ClientStop("Critstate", true);

			pulseImg.sprite = StatesSprites[CurrentSpriteSet].SP[currentSprite];
			bgImage.sprite = statesBgImages[CurrentSpriteSet];
		}
	}
}