using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;
using System.Linq;


public class PreviewCleanup : EditorWindow
{
	[MenuItem("Tools/PreviewCleanup")]
	public static void Apply()
	{
		//Moving old tiles  
		string path = Application.dataPath + "/textures/TilePreviews/Resources/Prefabs/Objects/";
		int counter = 0;
		int exist = 0;
		int cleaned = 0;
		string[] scan = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);
		foreach (String file in scan)
		{
			counter++;
			int t = scan.Length;
			EditorUtility.DisplayProgressBar(counter.ToString() + "/" + scan.Length + " Removing abundant preview sprites", "Sprite: " + counter, (float) counter / (float) scan.Length);
			//			Debug.Log ("Longpath data: " + file);

			//Get the filename without extention and path
			string name = Path.GetFileNameWithoutExtension(file);
			//			Debug.Log ("Creating tile for prefab: " + name);

			//Generating the path needed to chose the right tile output sub-folder
			string subPath = file.Substring(file.IndexOf("Objects") + 7);
			//			Debug.Log ("subPath data: " + subPath);
			string barePath = subPath.Substring(0, subPath.LastIndexOf(Path.DirectorySeparatorChar));
			//			Debug.Log ("barePath data: " + barePath);

			//Check if tile already exists
			if (System.IO.File.Exists (Application.dataPath +  "/Resources/Prefabs/Objects" + barePath + "/" + name + ".prefab")) {
//				Debug.Log ("A prefab for " + name + " exists... Skipping...");
				exist++;
			} 
			else {
				FileUtil.DeleteFileOrDirectory(file);
				FileUtil.DeleteFileOrDirectory(file + ".meta");
//				Debug.Log("DESTROY");
				cleaned++;
				UnityEditor.AssetDatabase.Refresh ();
			}
		}
		EditorUtility.ClearProgressBar();
		Debug.Log (counter + " Preview Sprites Processed");
		Debug.Log (exist + " Previes Sprites skipped, Prefab exists");
		Debug.Log(cleaned + " Preview Sprites deleted, prefab does not exist");
	}
}