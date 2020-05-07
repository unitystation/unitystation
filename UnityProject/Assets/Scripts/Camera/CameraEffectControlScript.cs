using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CameraEffects
{
	public class CameraEffectControlScript : MonoBehaviour
	{

		public DrunkCamera drunkCamera;

		public GlitchEffect glitchEffect;

		public NightVisionCamera nightVisionCamera;

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
	}
}