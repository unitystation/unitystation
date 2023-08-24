using System;
using Audio.Containers;
using UnityEngine;

namespace Core.Camera
{
	public class FlashbangCamera : MonoBehaviour
	{
		private float speed = 2f;
		[Range(0f, 2f)] public float Power = 0;
		public Material material;

		public const float NO_LOWPASS = 22000.00f;
		public const float LOWPASS = 510.00f;
		private const string AUDIOMIXER_LOWPASS_KEY = "SFXLowpass";


		private void OnDisable()
		{
			AudioManager.Instance.GameplayMixer.audioMixer.ClearFloat(AUDIOMIXER_LOWPASS_KEY);
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

		private void Update()
		{
			// Create a new texture
			Texture2D noiseTexture = new Texture2D(256, 256);
			// Loop through each pixel of the texture
			for (int x = 0; x < noiseTexture.width; x++)
			{
				for (int y = 0; y < noiseTexture.height; y++)
				{
					// Calculate the noise value using Perlin noise
					float noiseValue = Mathf.PerlinNoise((float)x / noiseTexture.width * speed, (float)y / noiseTexture.height * speed);

					// Set the pixel color based on the noise value
					Color color = new Color(noiseValue, noiseValue, noiseValue);

					// Set the pixel color in the texture
					noiseTexture.SetPixel(x, y, color);
				}
			}
			// Apply the changes to the texture
			noiseTexture.Apply();
			// Assign the texture to the material
			material.SetTexture("_MainTex", noiseTexture);
		}

		// Called by Camera to apply image effect
		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			material.SetFloat("_vignetteIntensity", Power);
			material.SetFloat("_vignetteSize", Power);
			Graphics.Blit(source, destination, material);
		}
	}
}