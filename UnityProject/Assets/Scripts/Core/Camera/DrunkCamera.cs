using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CameraEffects
{
	public class DrunkCamera : MonoBehaviour
	{
		public Material material;

		[Range(0f, 10f)]
		public float LightScale = 5f;
		[Range(0f, 0.1f)]
		public float VertWave = 0.05f;
		[Range(0f, 0.1f)]
		public float HozWave = 0.05f;
		[Range(0f, 0.5f)]
		public float Waves = 0.25f;
		[Range(0f, 1f)]
		public float Speed = 0.5f;
		[Range(0f, 0.02f)]
		public float DoubleVision = 0.01f;

		public void Tipsy()
		{
			LightScale = 4.5f;
			VertWave = 0.003f;
			HozWave = 0.003f;
			Waves = 0;
			Speed = 0;
			DoubleVision = 0;
		}
		public void LightDrunk()
		{
			LightScale = 4f;
			VertWave = 0.01f;
			HozWave = 0.01f;
			Waves = 0.2f;
			Speed = 0.5f;
			DoubleVision = 0.002f;
		}
		public void ModerateDrunk()
		{
			LightScale = 4.5f;
			VertWave = 0.025f;
			HozWave = 0.025f;
			Waves = 0.25f;
			Speed = 0.5f;
			DoubleVision = 0.003f;
		}
		public void HeavyDrunk()
		{
			LightScale = 5f;
			VertWave = 0.04f;
			HozWave = 0.04f;
			Waves = 0.3f;
			Speed = 0.3f;
			DoubleVision = 0.005f;
		}
		public void BlackoutDrunk()
		{
			LightScale = 8f;
			VertWave = 0.06f;
			HozWave = 0.07f;
			Waves = 0.5f;
			Speed = 0.6f;
			DoubleVision = 0.015f;
		}
		public void Hungover()
		{
			LightScale = 2.8f;
			VertWave = 0.005f;
			HozWave = 0.01f;
			Waves = 0.0f;
			Speed = 0.0f;
			DoubleVision = 0.0f;
		}

		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			material.SetFloat("_LightAmount", LightScale);
			material.SetFloat("_CosAmount", VertWave);
			material.SetFloat("_SinAmount", HozWave);
			material.SetFloat("_Waves", Waves);
			material.SetFloat("_Speed", Speed);
			material.SetFloat("_DoubleVision", DoubleVision);

			Graphics.Blit(source, destination, material);
		}
	}
}