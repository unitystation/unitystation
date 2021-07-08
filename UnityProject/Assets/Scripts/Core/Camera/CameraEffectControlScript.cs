using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CameraEffects
{
	public class CameraEffectControlScript : MonoBehaviour
	{

		public DrunkCamera drunkCamera;
		public GreyscaleCamera greyscaleCamera;
		public GlitchEffect glitchEffect;
		public NightVisionCamera nightVisionCamera;

		[SerializeField]
		private GameObject minimalVisibilitySprite;

		[SerializeField]
		private int maxDrunkTime = 120;

		private const float TIMER_INTERVAL = 1f;
		private int drunkCameraTime = 0;

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

		public void AddDrunkTime(int time)
		{
			drunkCameraTime += time;

			drunkCameraTime = Mathf.Min(drunkCameraTime, maxDrunkTime);

			if (drunkCamera.enabled == false)
			{
				drunkCamera.ModerateDrunk();
				UpdateManager.Add(DoEffectTimeCheck, TIMER_INTERVAL);
			}
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
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, DoEffectTimeCheck);
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
		}
	}
}
