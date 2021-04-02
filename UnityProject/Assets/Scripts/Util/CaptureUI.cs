using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Util{

	/// <summary>
	/// Takes a picture of the UI then stores it to a place in %APPDATA%/LocalLow/unitystation.
	/// </summary>

	public class CaptureUI : MonoBehaviour
	{
		public RectTransform UI_ToCapture;

		private int width;
		private int height;

		public string Path = "/SNAPS";
		public string FileName = "filename.PNG";

		private void Awake()
		{
			width = System.Convert.ToInt32(UI_ToCapture.rect.width / 2.2f);
			height = System.Convert.ToInt32(UI_ToCapture.rect.height / 2.2f);
			Debug.Log(width + height);
		}

		public IEnumerator TakeScreenShot()
		{

			Vector2 temp = UI_ToCapture.transform.position;
			var startX = temp.x - width / 2;
			var startY = temp.y - height / 2;

			Debug.Log(temp);

			yield return WaitFor.EndOfFrame;

			var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
			tex.ReadPixels(new Rect(startX, startY, width, height), 0, 0);
			tex.Apply();

			// Encode texture into PNG
			var bytes = tex.EncodeToPNG();
			Destroy(tex);

			if (Directory.Exists(Application.persistentDataPath + Path))
			{
				File.WriteAllBytes(Application.persistentDataPath + Path + "/" + FileName, bytes);
				Debug.Log(Application.persistentDataPath + Path);
			}
			else
			{
				Directory.CreateDirectory(Application.persistentDataPath + Path);
				File.WriteAllBytes(Application.persistentDataPath + Path + "/" + FileName, bytes);
			}
		}
	}
}
