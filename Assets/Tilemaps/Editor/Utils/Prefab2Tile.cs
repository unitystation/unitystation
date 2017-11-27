using Tilemaps.Scripts.Tiles;
using Tilemaps.Editor.Utils;
using Tilemaps.Scripts.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using Tilemaps.Scripts.Behaviours.Objects;
using UnityEngine.Tilemaps;
using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;




public class Prefab2Tile : EditorWindow
{
	[MenuItem("Tools/Prefab2Tile")]
	public static void Apply()
	{
		//Moving old tiles
		FileUtil.DeleteFileOrDirectory(Application.dataPath + "/Tilemaps/Tiles/Objects_Backup");
		FileUtil.MoveFileOrDirectory(Application.dataPath + "/Tilemaps/Tiles/Objects", Application.dataPath + "/Tilemaps/Tiles/Objects_Backup");
		FileUtil.DeleteFileOrDirectory(Application.dataPath + "/Tilemaps/Tiles/Objects");
		string path = Application.dataPath + "/Prefabs/Objects/";
		int counter = 0;
		foreach (String file in Directory.GetFiles(path, "*.prefab", SearchOption.AllDirectories)) {
			Debug.Log ("Longpath data: " + file);

			//Get the filename without extention and path
			string name = Path.GetFileNameWithoutExtension (file);		
			Debug.Log ("Creating tile for prefab: " + name);

			//Generating the path needed to hook onto for selecting the game object
			string smallPath = file.Substring(file.IndexOf("Assets") + 0);
			Debug.Log ("smallpath data: " + smallPath);

			//Generating the path needed to chose the right tile output sub-folder
			string subPath = smallPath.Substring(smallPath.IndexOf("Objects") + 7);
			Debug.Log ("subPath data: " + subPath);
			string barePath = subPath.Substring(0, subPath.LastIndexOf('/'));
			Debug.Log ("barePath data: " + barePath);

			//setup building the tile//
			var tile = TileBuilder.CreateTile<ObjectTile>(LayerType.Objects);

			//Cast the gameobject
			var cast = AssetDatabase.LoadAssetAtPath(smallPath, (typeof(GameObject))) as GameObject;

			tile.Object = cast;
			//Create the tile
			TileBuilder.CreateAsset(tile, name, "Assets/Tilemaps/Tiles/Objects" + barePath);
		
			counter++;
		}
		if (counter == 0)
			Debug.Log ("No prefabs processed");
		else
			Debug.Log (counter + " tiles created for prefabs");
	}
}