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

		public void ChangeDrunkState()
		{
			drunkCamera.enabled = !drunkCamera.enabled;
		}

		public void ChangeGlitchState()
		{
			glitchEffect.enabled = !glitchEffect.enabled;
		}

		public void ChangeNightVisionState()
		{
			nightVisionCamera.enabled = !nightVisionCamera.enabled;
		}
	}
}