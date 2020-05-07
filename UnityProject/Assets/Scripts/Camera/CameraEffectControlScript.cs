using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CameraEffects
{
	public class CameraEffectControlScript : MonoBehaviour
	{

		public DrunkCamera drunkCamera;

		public GlitchEffect glitchEffect;

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Backspace))
			{
				ChangeDrunkState(drunkCamera);
			}

			if (Input.GetKeyDown(KeyCode.P))
			{
				ChangeGlitchState(glitchEffect);
			}
		}

		public void ChangeDrunkState(DrunkCamera drunkCamera)
		{
			drunkCamera.enabled = !drunkCamera.enabled;
		}

		public void ChangeGlitchState(GlitchEffect glitchEffect)
		{
			glitchEffect.enabled = !glitchEffect.enabled;
		}
	}
}