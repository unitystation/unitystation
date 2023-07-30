using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CameraEffects
{
	public class NightVisionCamera : MonoBehaviour
	{
		// Public data
		public Shader shader;
		[Range(0f, 1f)]
		public float luminance = 0.44f;
		[Range(0.5f, 2f)]
		public float lensRadius = 0.84f;
		// Private data
		Material material;

		public bool lensRadiusMaxed = false;

		// Called by Camera to apply image effect
		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (shader != null)
			{
				if (material == false)
				{
					material = new Material(shader);
				}
				material.SetVector("_Luminance", new Vector4(luminance, luminance, luminance, luminance));
				if (lensRadiusMaxed == false)
					material.SetFloat("_LensRadius", lensRadius);
				else
					material.SetFloat("_LensRadius", 4f);
				Graphics.Blit(source, destination, material);
			}
			else
			{
				Graphics.Blit(source, destination);
			}
		}
	}
}