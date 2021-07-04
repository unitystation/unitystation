using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CameraEffects
{
	public class GreyscaleCamera : MonoBehaviour
	{
		public Material material;
		[Range(0f, 1.0f)]
		public float amount;

		public Color tint = new Color(1, 1, 1, 1);

		public void Desaturate(float greyAmount)
		{
			material.SetFloat("_EffectAmount", greyAmount);
		}
		public void TintVision(Color toTint)
		{
			material.SetColor("_Color", toTint);
		}
		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			material.SetColor("_Color", tint);
			material.SetFloat("_EffectAmount", amount);
			Graphics.Blit(source, destination, material);
		}
	}
}


