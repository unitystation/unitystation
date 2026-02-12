using UnityEngine;
using UnityEngine.U2D;

namespace US13.Core.Camera
{
	public class TheMostPerfectOfCamera : MonoBehaviour
	{
		public float screenWidthCache { get; set; }
		public float screenHeightCache { get; set; }

		public bool tolgge = true;

		public PixelPerfectCamera PixelPerfectCamera;

		private void Update()
		{
			if (tolgge == false) return;
			int screenWidth = Screen.width;
			int screenHeight = Screen.height;

			// Check if the screen size changed
			if (screenWidthCache != screenWidth || screenHeightCache != screenHeight)
			{
				// Make width and height even
				if (screenWidth % 2 != 0)
				{
					screenWidth--;
					//Loggy.Info($"Adjusted odd width to {screenWidth}");
				}
				if (screenHeight % 2 != 0)
				{
					screenHeight--;
					//Loggy.Info($"Adjusted odd height to {screenHeight}");
				}

				// Cache new values
				screenWidthCache = screenWidth;
				screenHeightCache = screenHeight;

				// Apply new render texture
				ApplyRenderTexture(screenWidth, screenHeight);
			}
		}

		private void ApplyRenderTexture(int width, int height)
		{
			PixelPerfectCamera.refResolutionX = width;
			PixelPerfectCamera.refResolutionY = height ;
		}


	}
}