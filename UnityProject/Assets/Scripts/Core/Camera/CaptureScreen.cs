using SecureStuff;
using UnityEngine;

/// <summary>
/// Take a picture from a part of the game then stores it to a place in %APPDATA%/LocalLow/unitystation.
/// This script takes transparancy into account.
/// </summary>
///
namespace Core.Cam
{
	public class CaptureScreen : MonoBehaviour
	{
		public UnityEngine.Camera cam;

		public int width;
		public int height;

		public string Path = "/SNAPS";
		public string FileName = "filename.PNG";

		private bool takingScreenshot = false;

		private void Awake()
		{
			//Make sure there is a camera avaliable so the game doesn't throw an error.
			//Though it's best to setup your own camera with it's own position and size for
			//specifc use cases that does not require taking an entire picture of the screen.
			if(cam == null)
			{
				cam = UnityEngine.Camera.main;
			}
		}

		private void OnPostRender()
		{
			if (takingScreenshot)
			{
				takingScreenshot = false;
				RenderTexture texture = cam.targetTexture;
				Texture2D result = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);

				Rect rect = new Rect(0, 0, texture.width, texture.height);
				result.ReadPixels(rect, 0, 0);

				byte[] byteArray = result.EncodeToPNG();
				AccessFile.Write( byteArray, Path + "/" + FileName, FolderType.Data);

				RenderTexture.ReleaseTemporary(texture);
				cam.targetTexture = null;
			}
		}

		/// <summary>
		/// Takes a screen of a specfied preset area.
		/// </summary>
		/// <param name="w">Width of the picture</param>
		/// <param name="h">Height of the picture</param>
		public void TakeScreenshot(int w, int h)
		{
			width = w;
			height = h;
			cam.targetTexture = RenderTexture.GetTemporary(width, height, 16);
			takingScreenshot = true;
		}
	}
}
