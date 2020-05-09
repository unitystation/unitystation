using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CameraEffects
{
	public class HologramCamera : MonoBehaviour
	{
		public Material material;

		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			Graphics.Blit(source, destination, material);
		}
	}
}