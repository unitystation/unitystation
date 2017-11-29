using Tilemaps.Scripts.Tiles;
using Tilemaps.Editor.Utils;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class Prefab2Tile : EditorWindow
{
    [MenuItem("Tools/Prefab2Tile")]
    public static void Apply()
    {
        //Moving old tiles
        FileUtil.DeleteFileOrDirectory(Application.dataPath + "/Tilemaps/Tiles/Objects_Backup");
//		FileUtil.MoveFileOrDirectory(Application.dataPath + "/Tilemaps/Tiles/Objects", Application.dataPath + "/Tilemaps/Tiles/Objects_Backup");
        FileUtil.DeleteFileOrDirectory(Application.dataPath + "/Tilemaps/Tiles/Objects");
        string path = Application.dataPath + "/Resources/Prefabs/Objects/";
        int counter = 0;
        string[] scan = Directory.GetFiles(path, "*.prefab", SearchOption.AllDirectories);
        foreach (String file in scan)
        {
            counter++;
            int t = scan.Length;
			EditorUtility.DisplayProgressBar(counter.ToString() + "/" + scan.Length + " Generating Tiles", "Tile: " + counter, (float) counter / (float) scan.Length);
//			Debug.Log ("Longpath data: " + file);

            //Get the filename without extention and path
            string name = Path.GetFileNameWithoutExtension(file);
//			Debug.Log ("Creating tile for prefab: " + name);

            //Generating the path needed to hook onto for selecting the game object
            string smallPath = file.Substring(file.IndexOf("Assets") + 0);
//			Debug.Log ("smallpath data: " + smallPath);

            //Generating the path needed to chose the right tile output sub-folder
            string subPath = smallPath.Substring(smallPath.IndexOf("Objects") + 7);
//			Debug.Log ("subPath data: " + subPath);
            string barePath = subPath.Substring(0, subPath.LastIndexOf(Path.DirectorySeparatorChar));
//			Debug.Log ("barePath data: " + barePath);

            //setup building the tile//
            var tile = TileBuilder.CreateTile<ObjectTile>(LayerType.Objects);

            //Cast the gameobject
            var cast = AssetDatabase.LoadAssetAtPath(smallPath, (typeof(GameObject))) as GameObject;
			if (barePath == "/WallMounts") {
				tile.Rotatable = true;
				tile.Offset = true;
			} 
			else {
				tile.Rotatable = false;
				tile.Offset = false;
			}
            tile.Object = cast;
            //Create the tile
            TileBuilder.CreateAsset(tile, name, "Assets/Tilemaps/Tiles/Objects" + barePath);
        }
        EditorUtility.ClearProgressBar();
        if (counter == 0)
            Debug.Log("No prefabs processed");
        else
            Debug.Log(counter + " tiles created for prefabs");
    }
}