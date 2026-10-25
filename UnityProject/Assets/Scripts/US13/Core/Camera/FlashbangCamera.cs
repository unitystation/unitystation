using UnityEngine;
using US13.Managers;

namespace US13.Core.Camera
{
	public class FlashbangCamera : CameraEffectBase
	{
		[Range(0f, 2f)] public float Power = 0;
		public Material material;

		public const float NO_LOWPASS = 22000.00f;
		public const float LOWPASS = 510.00f;
		private const string AUDIOMIXER_LOWPASS_KEY = "SFXLowpass";

		private void OnDisable()
		{
			if (AudioManager.Instance != null) AudioManager.Instance.GameplayMixer.audioMixer.ClearFloat(AUDIOMIXER_LOWPASS_KEY);
		}

		public void SetFlashbangSoundStrength(float strength)
		{
			AudioManager.Instance.GameplayMixer.audioMixer.SetFloat(AUDIOMIXER_LOWPASS_KEY, strength);
		}

		public float GetFlashbangSoundStrength()
		{
			AudioManager.Instance.GameplayMixer.audioMixer.GetFloat(AUDIOMIXER_LOWPASS_KEY, out var strength);
			return strength;
		}

		public override Material GetMaterial()
		{
			var mat = GetInstance(material);
			if (mat == null) return null;

			mat.SetFloat("_vignetteIntensity", Power);
			mat.SetFloat("_vignetteSize", Power);
			return mat;
		}
	}
}