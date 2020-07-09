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

		private void OnEnable()
		{
			UpdateManager.Add(DoEffectTimeCheck, TIMER_INTERVAL);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, DoEffectTimeCheck);
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
	}
}