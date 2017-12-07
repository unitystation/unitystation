using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.MemoryProfiler;
using UnityEditorInternal;
using Tilemaps.Editor.Utils;
using Tilemaps.Scripts.Utils;
using Tilemaps.Scripts.Tiles;
using Tilemaps.Scripts.Behaviours.Objects;


public class Prefab2Tile : EditorWindow
{
    [MenuItem("Tools/Prefab2Tile")]
    public static void Apply()
    {
        string[] watched = { "Objects" };
        foreach (string subject in watched)
        {
            Prefab2Tile.CheckTiles(subject);
            Prefab2Tile.GenerateTiles(subject);
            Prefab2Tile.CleanSprites(subject);
        }
    }


    public static void GenerateTiles(string subject)
    {
        //Moving old tiles
        string basePath = Application.dataPath + "/Resources/Prefabs/";
        string basePath2 = basePath + subject + "/";
        //			Debug.Log (basePath2);
        int counter = 0;
        int created = 0;
        string[] scan = Directory.GetFiles(basePath2, "*.prefab", SearchOption.AllDirectories);
        foreach (String file in scan)
        {
            counter++;
            int t = scan.Length;
            EditorUtility.DisplayProgressBar(counter.ToString() + "/" + scan.Length + " Generating Tiles for " + subject, "Tile: " + counter, (float)counter / (float)scan.Length);
            //			Debug.Log ("Longpath data: " + file);

            //Get the filename without extention and path
            string name = Path.GetFileNameWithoutExtension(file);
            //			Debug.Log ("Creating tile for prefab: " + name);

            //Generating the path needed to hook onto for selecting the game object
            string smallPath = file.Substring(file.IndexOf("Assets") + 0);
            //			Debug.Log ("smallpath data: " + smallPath);


            //Generating the path needed to chose the right tile output sub-folder
            string subPath = smallPath.Substring(smallPath.IndexOf(subject) + 7);
            //			Debug.Log ("subPath data: " + subPath);
            string barePath = subPath.Substring(0, subPath.LastIndexOf(Path.DirectorySeparatorChar));
            //			Debug.Log ("barePath data: " + barePath);

            //Check if tile already exists
            if (System.IO.File.Exists(Application.dataPath + "/Tilemaps/Tiles/" + subject + "/" + barePath + "/" + name + ".asset"))
            {
                //		Debug.Log ("A tile for " + name + " already exists... Skipping...");
            }
            else
            {


                //setup building the tile//
                var tile = TileBuilder.CreateTile<ObjectTile>(LayerType.Objects);

                //Cast the gameobject
                var cast = AssetDatabase.LoadAssetAtPath(smallPath, (typeof(GameObject))) as GameObject;
                if (barePath == "/WallMounts")
                {
                    tile.Rotatable = true;
                    tile.Offset = true;
                }
                else
                {
                    tile.Rotatable = false;
                    tile.Offset = false;
                }
                tile.Object = cast;
                //Create the tile
                TileBuilder.CreateAsset(tile, name, "Assets/Tilemaps/Tiles/" + subject + "/" + barePath);
                PreviewSpriteBuilder.Create(cast);
                created++;

            }
        }
        EditorUtility.ClearProgressBar();
        Debug.Log(created + " / " + counter + " Tiles created for prefabs");
    }


    public static void CleanSprites(string subject)
    {
        //Moving old tiles  
        string basePath = Application.dataPath + "/Textures/TilePreviews/res/Prefabs/";
        string basePath2 = basePath + subject + "/";
        int counter = 0;
        int cleaned = 0;
        string[] scan = Directory.GetFiles(basePath2, "*.png", SearchOption.AllDirectories);
        foreach (String file in scan)
        {
            counter++;
            int t = scan.Length;
            EditorUtility.DisplayProgressBar(counter.ToString() + "/" + scan.Length + " Removing abundant preview sprites for " + subject, "Sprite: " + counter, (float)counter / (float)scan.Length);
            //			Debug.Log ("Longpath data: " + file);

            //Get the filename without extention and path
            string name = Path.GetFileNameWithoutExtension(file);
            //			Debug.Log ("Creating tile for prefab: " + name);

            //Generating the path needed to chose the right tile output sub-folder
            string subPath = file.Substring(file.IndexOf(subject) + 7);
            //			Debug.Log ("subPath data: " + subPath);
            string barePath = subPath.Substring(0, subPath.LastIndexOf(Path.DirectorySeparatorChar));
            //			Debug.Log ("barePath data: " + barePath);

            //Check if tile already exists
            if (System.IO.File.Exists(Application.dataPath + "/Resources/Prefabs/" + subject + barePath + "/" + name + ".prefab"))
            {
                //				Debug.Log ("A prefab for " + name + " exists... Skipping...");

            }
            else
            {
                FileUtil.DeleteFileOrDirectory(file);
                FileUtil.DeleteFileOrDirectory(file + ".meta");
                //				Debug.Log("DESTROY");
                cleaned++;
                UnityEditor.AssetDatabase.Refresh();
            }
        }
        EditorUtility.ClearProgressBar();
        Debug.Log(cleaned + " / " + counter + " Preview sprites deleted for missing tiles");
    }

    public static void CheckTiles(string subject)
    {
        //Moving old tiles  
        string basePath = Application.dataPath + "/Tilemaps/Tiles/";
        string basePath2 = basePath + subject + "/";
        int counter = 0;
        int cleaned = 0;
        string[] scan = Directory.GetFiles(basePath2, "*.asset", SearchOption.AllDirectories);
        foreach (String file in scan)
        {
            counter++;
            int t = scan.Length;
            EditorUtility.DisplayProgressBar(counter.ToString() + "/" + scan.Length + " Removing abundant tiles for " + subject, "Tile: " + counter, (float)counter / (float)scan.Length);
            //			Debug.Log ("Longpath data: " + file);

            //Get the filename without extention and path
            string name = Path.GetFileNameWithoutExtension(file);
            //			Debug.Log ("Creating tile for prefab: " + name);

            //Generating the path needed to chose the right tile output sub-folder
            string subPath = file.Substring(file.IndexOf(subject) + 7);
            //			Debug.Log ("subPath data: " + subPath);
            string barePath = subPath.Substring(0, subPath.LastIndexOf(Path.DirectorySeparatorChar));
            //			Debug.Log ("barePath data: " + barePath);

            //Check if tile already exists
            if (System.IO.File.Exists(Application.dataPath + "/Resources/Prefabs/" + subject + "/" + barePath + "/" + name + ".prefab"))
            {
                //				Debug.Log ("A prefab for " + name + " exists... Skipping...");
            }
            else
            {
                FileUtil.DeleteFileOrDirectory(file);
                FileUtil.DeleteFileOrDirectory(file + ".meta");
                //				Debug.Log("DESTROY");
                cleaned++;
                UnityEditor.AssetDatabase.Refresh();
            }
        }
        EditorUtility.ClearProgressBar();
        Debug.Log(cleaned + " / " + counter + " Tiles deleted for Prefabs");


    }

}