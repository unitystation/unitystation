//-----------------------------------------------------------------
// This class hosts utility methods for handling session information.
//-----------------------------------------------------------------

using UnityEngine;
using UnityEditor;
using System.IO;

public class UPASession {

	public static UPAImage CreateImage (int w, int h) {
		string path = EditorUtility.SaveFilePanel ("Create UPAImage",
		                                           "Assets/", "Pixel Image.asset", "asset");
		if (path == "") {
			return null;
		}

		path = FileUtil.GetProjectRelativePath(path);

		UPAImage img = ScriptableObject.CreateInstance<UPAImage>();
		AssetDatabase.CreateAsset (img, path);

		AssetDatabase.SaveAssets();

		img.Init(w, h);
		EditorUtility.SetDirty(img);
		UPAEditorWindow.CurrentImg = img;

		EditorPrefs.SetString ("currentImgPath", AssetDatabase.GetAssetPath (img));

		if (UPAEditorWindow.window != null)
			UPAEditorWindow.window.Repaint();
		else
			UPAEditorWindow.Init();

		img.gridSpacing = 10 - Mathf.Abs (img.width - img.height)/100f;
		return img;
	}

	public static UPAImage OpenImage () {
		string path = EditorUtility.OpenFilePanel(
			"Find an Image (.asset | .png | .jpg)",
			"Assets/",
			"Image Files;*.asset;*.jpg;*.png");

		if (path.Length != 0) {
			// Check if the loaded file is an Asset or Image
			if (path.EndsWith(".asset")) {
				path = FileUtil.GetProjectRelativePath(path);
				UPAImage img = AssetDatabase.LoadAssetAtPath(path, typeof(UPAImage)) as UPAImage;
				EditorPrefs.SetString ("currentImgPath", path);
				return img;
			}
			else
			{
				// Load Texture from file
				Texture2D tex = LoadImageFromFile(path);
				// Create a new Image with textures dimensions
				UPAImage img = CreateImage(tex.width, tex.height);
				// Set pixel colors
				img.layers[0].tex = tex;
				img.layers[0].tex.filterMode = FilterMode.Point;
				img.layers[0].tex.Apply ();
				for (int x = 0; x < img.width; x++) {
					for (int y = 0; y < img.height; y++) {
						img.layers[0].map[x + y * tex.width] = tex.GetPixel(x, tex.height - 1 - y);
					}
				}
			}
		}

		return null;
	}

	public static Texture2D LoadImageFromFile (string path) {
		Texture2D tex = null;
		byte[] fileData;
		if (File.Exists(path))     {
			fileData = File.ReadAllBytes(path);
			tex = new Texture2D(2, 2);
			tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
		}
		return tex;
	}

	public static UPAImage OpenImageByAsset (UPAImage img) {

		if (img == null) {
			Debug.LogWarning ("Image is null. Returning null.");
			EditorPrefs.SetString ("currentImgPath", "");
			return null;
		}

		string path = AssetDatabase.GetAssetPath (img);
		EditorPrefs.SetString ("currentImgPath", path);

		return img;
	}

	public static UPAImage OpenImageAtPath (string path) {
		if (path.Length != 0) {
			UPAImage img = AssetDatabase.LoadAssetAtPath(path, typeof(UPAImage)) as UPAImage;

			if (img == null) {
				EditorPrefs.SetString ("currentImgPath", "");
				return null;
			}

			EditorPrefs.SetString ("currentImgPath", path);
			return img;
		}

		return null;
	}

	public static bool ExportImage (UPAImage img, TextureType type, TextureExtension extension)
	{

		var folder = "Assets/";
		var fileName = img.name + "." + extension.ToString().ToLower();

		if (PlayerPrefs.HasKey("pixelfile"))
		{
			folder = PlayerPrefs.GetString("pixelfolder");
			fileName = PlayerPrefs.GetString("pixelfile");
		}

		string path = EditorUtility.SaveFilePanel(
			"Export image as " + extension.ToString(),
			folder,
			fileName,
			extension.ToString().ToLower());

		if (path.Length == 0)
			return false;

		byte[] bytes;
		if (extension == TextureExtension.PNG) {
			// Encode texture into PNG
			bytes = img.GetFinalImage(true).EncodeToPNG();
		} else {
			// Encode texture into JPG

			#if UNITY_4_2
			bytes = img.GetFinalImage(true).EncodeToPNG();
			#elif UNITY_4_3
			bytes = img.GetFinalImage(true).EncodeToPNG();
			#elif UNITY_4_5
			bytes = img.GetFinalImage(true).EncodeToJPG();
			#else
			bytes = img.GetFinalImage(true).EncodeToJPG();
			#endif
		}

		path = FileUtil.GetProjectRelativePath(path);

		PlayerPrefs.SetString("pixeldir", Path.GetDirectoryName(path));
		PlayerPrefs.SetString("pixelfile", Path.GetFileName(path));
		PlayerPrefs.Save();

		//Write to a file in the project folder
		File.WriteAllBytes(path, bytes);
		AssetDatabase.Refresh();

		TextureImporter texImp = AssetImporter.GetAtPath(path) as TextureImporter;

		if (type == TextureType.texture)
			texImp.textureType = TextureImporterType.Default;
		else if (type == TextureType.sprite) {
			texImp.textureType = TextureImporterType.Sprite;

			#if UNITY_4_2
			texImp.spritePixelsToUnits = 10;
			#elif UNITY_4_3
			texImp.spritePixelsToUnits = 10;
			#elif UNITY_4_5
			texImp.spritePixelsToUnits = 10;
			#else
			texImp.spritePixelsPerUnit = 32;
			#endif
		}

		texImp.filterMode = FilterMode.Point;
		texImp.textureCompression = TextureImporterCompression.Uncompressed;

		AssetDatabase.ImportAsset(path);

		return true;
	}
}
