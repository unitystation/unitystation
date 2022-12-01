using System;
using System.Collections;
using System.Collections.Generic;
using Core.Utils;
using UnityEngine;

namespace CameraEffects
{
	public class CameraEffectControlScript : MonoBehaviour
	{

		public DrunkCamera drunkCamera;
		public GreyscaleCamera greyscaleCamera;
		public GlitchEffect glitchEffect;
		public NightVisionCamera nightVisionCamera;

		public BlurryVision blurryVisionEffect;
		public ColourblindEmulation colourblindEmulationEffect;

		[SerializeField]
		private GameObject minimalVisibilitySprite;
		public Vector3 MinimalVisibilityScale { private set; get; } = new(3.5f, 3.5f, 8);


		[SerializeField] private int maxDrunkTime = 120000;
		[SerializeField] private int maxFlashTime = 25;

		private const float TIMER_INTERVAL = 1f;
		private float drunkCameraTime = 0;

		public LightingSystem LightingSystem;


		private MultiInterestBool blindness = new MultiInterestBool(true,
			MultiInterestBool.RegisterBehaviour.RegisterFalse,
			MultiInterestBool.BoolBehaviour.ReturnOnFalse);


		public MultiInterestBool Blindness => blindness;


		public float BlindFOVDistance = 0.65f;
		public float FullVisionFOVDistance = 15;


		public void Awake()
		{
			LightingSystem = this.GetComponent<LightingSystem>();
			blindness.OnBoolChange.AddListener(BlindnessValue);

			if (minimalVisibilitySprite != null)
			{
				MinimalVisibilityScale = minimalVisibilitySprite.transform.localScale;
				return;
			}
			Logger.LogWarning("[CameraEffectControlScript] - visibilitySprite is null! please set it from the inspector.");
		}


		private void OnEnable()
		{
			EventManager.AddHandler(Event.GhostSpawned, OnGhostSpawn);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, DoEffectTimeCheck);
			EventManager.RemoveHandler(Event.GhostSpawned, OnGhostSpawn);
		}

		private void OnGhostSpawn()
		{
			drunkCameraTime = 0;
			ToggleNightVisionEffectState(false);
			ToggleGlitchEffectState(false);
		}

		public void AddDrunkTime(float time)
		{
			drunkCameraTime += time;

			drunkCameraTime = Mathf.Min(drunkCameraTime, maxDrunkTime);

			if (drunkCamera.enabled == false)
			{
				ToggleDrunkEffectState(true);
				drunkCamera.ModerateDrunk();
				UpdateManager.Add(DoEffectTimeCheck, TIMER_INTERVAL);
			}
		}

		//setts the FOV to emulate blindness on the player
		public void BlindnessValue(bool isBlind)
		{
			if (isBlind)
			{
				Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().LightingSystem.fovDistance = BlindFOVDistance;
			}
			else
			{
				Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().LightingSystem.fovDistance = FullVisionFOVDistance;
			}
		}

		public void FlashEyes(float flashTime)
		{
			StartCoroutine(FlashEyesCoroutine(flashTime));
		}
		private IEnumerator FlashEyesCoroutine(float flashTime)
		{
			//TODO : Add flash effects here later
			yield break;
		}

		public void ToggleDrunkEffectState(bool state)
		{
			drunkCamera.enabled = state;
		}

		public void ToggleGlitchEffectState(bool state)
		{
			glitchEffect.enabled = state;
		}

		public void ToggleNightVisionEffectState(bool state)
		{
			nightVisionCamera.enabled = state;
		}

		private void DoEffectTimeCheck()
		{
			if (drunkCameraTime > 0)
			{
				drunkCamera.enabled = true;
				drunkCameraTime --;
			}
			else
			{
				drunkCamera.enabled = false;

			}
		}

		/// <summary>
		/// Updates the size of the dim light around the player that allows him to see themselves in the dark.
		/// </summary>
		public void AdjustPlayerVisibility(Vector3 newSize, float time)
		{
			LeanTween.scale(minimalVisibilitySprite, newSize, time);
		}

		public void EnsureAllEffectsAreDisabled()
		{
			//TODO: Find out a solution in the shaders why the screen inverts if both drunk and greyscale are both on
			drunkCamera.enabled = false;
			glitchEffect.enabled = false;
			nightVisionCamera.enabled = false;
			greyscaleCamera.enabled = false;
			colourblindEmulationEffect.SetColourMode(ColourBlindMode.None);
			blurryVisionEffect.SetBlurStrength(0);
		}
	}
}
