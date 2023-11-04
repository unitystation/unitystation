using SecureStuff;
using UnityEngine;

namespace Util
{

	/// <summary>
	/// Takes a picture of the UI then stores it to a place in %APPDATA%/LocalLow/unitystation.
	/// </summary>

	public class CaptureUI : MonoBehaviour
	{
		public RectTransform UI_ToCapture;

		private int width;
		private int height;
		[SerializeField] private float zoomLevel = 2.2f;

		public string Path = "/SNAPS";
		public string FileName = "filename.PNG";

		private void Awake()
		{
			width = System.Convert.ToInt32(UI_ToCapture.rect.width / zoomLevel);
			height = System.Convert.ToInt32(UI_ToCapture.rect.height / zoomLevel);
		}

		public void TakeScreenShot()
		{
			Vector2 temp = UI_ToCapture.transform.position;
			var startX = temp.x - width / 2;
			var startY = temp.y - height / 2;
			var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
			tex.ReadPixels(new Rect(startX, startY, width, height), 0, 0);
			tex.Apply();

			var bytes = tex.EncodeToPNG();
			Destroy(tex);


			AccessFile.Write(bytes, Path + "/" + FileName, FolderType.Data);

		}
	}
}