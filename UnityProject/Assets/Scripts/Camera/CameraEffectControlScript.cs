using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CameraEffects
{
	public class CameraEffectControlScript : MonoBehaviour
	{

		public DrunkCamera drunkCamera;
		public int drunkCameraTime = 0;

		public GlitchEffect glitchEffect;

		public NightVisionCamera nightVisionCamera;

		private float timer;
		private const float TIMER_INTERVAL = 1f;

		public void ToggleDrunkEffectState()
		{
			drunkCamera.enabled = !drunkCamera.enabled;
		}

		public void ToggleGlitchEffectState()
		{
			glitchEffect.enabled = !glitchEffect.enabled;
		}

		public void ToggleNightVisionEffectState()
		{
			nightVisionCamera.enabled = !nightVisionCamera.enabled;
		}

		private void Update()
		{
			timer += Time.deltaTime;

			if (timer < TIMER_INTERVAL) return;

			Debug.Log("drunkCameraTime "+ drunkCameraTime);

			if (drunkCameraTime > 0)
			{
				drunkCamera.enabled = true;
				drunkCameraTime --;
			}
			else
			{
				drunkCamera.enabled = false;
			}

			timer = 0f;
		}
	}
}