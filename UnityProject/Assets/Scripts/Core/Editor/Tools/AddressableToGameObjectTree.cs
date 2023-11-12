using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logs;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Assets.Scripts.Editor.Tools
{
	/// <summary>
	/// Will convert all addressables find in the assets tree to a game object that respect the same hierarchy.
	/// All folders will be converted in a GameObject that contains either child GameObject (folders)
	/// or have a AssetReferenceLibrary that will contains AssetReferences
	/// All prefab addressables will be put in the AssetReferenceLibrary of the right GameObject in the hierarchy.
	/// </summary>
	public class AddressableToGameObjectTree : EditorWindow
	{
		string path;
		GameObject rootObject;
		private string[] assetsPaths;

		[MenuItem("Tools/Addressables to GameObject Tree Converter")]
		private static void Init()
		{
			// Get existing open window or if none, make a new one:
			AddressableToGameObjectTree window = (AddressableToGameObjectTree)GetWindow(typeof(AddressableToGameObjectTree));
			window.titleContent.text = "Addressables to GameObject Tree Converter";
			window.Show();
		}

		private void SelectPath()
		{
			string baseSTR = EditorUtility.OpenFolderPanel("Select folder to save prefabs", Application.dataPath, "prefabs");
			if (baseSTR != "")
			{
				if (-1 == baseSTR.IndexOf(Application.dataPath, System.StringComparison.InvariantCultureIgnoreCase))
				{
					EditorUtility.DisplayDialog("Assets folder error", "Target folder must be any child directory to : \"" + Application.dataPath + "\"", "OK");
					path = "";
				}
				else
				{
					path = baseSTR.Replace(Application.dataPath, "Assets");
				}
			}
		}

		private void ConvertToGameObjectRecursive(GameObject parentObject, string path)
		{
			string objectName = path.Split('/').Last();

			GameObject gameObject = new GameObject(objectName);
			gameObject.transform.parent = parentObject.transform;
			Loggy.Log($"Adding gameobject {objectName} to tree", Category.Editor);

			// List of all prefabs exactly at path (without their file name)
			//List<string> prefabsAtPath = assetsPaths.Where(p => string.Join("/", p.Split('/').Reverse().Skip(1).Reverse()).Equals(path) && p.Contains(".prefab")).ToList();

			string[] subFolders = AssetDatabase.GetSubFolders(path);

			foreach (string subFolder in subFolders)
				ConvertToGameObjectRecursive(gameObject, subFolder);
		}

		void OnGUI()
		{
			EditorGUILayout.HelpBox("Choose the root path where the addressables are found", MessageType.Info);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Addressables root folder:", GUILayout.MaxWidth(75.0f));

			path = EditorGUILayout.TextField(path, EditorStyles.textField);

			if (GUILayout.Button("...", GUILayout.MaxWidth(30.0f)))
			{
				SelectPath();
			}

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Separator();

			EditorGUILayout.HelpBox("Choose the GameObject that contains the tree of Asset References", MessageType.Info);
			rootObject = (GameObject)EditorGUILayout.ObjectField(rootObject, typeof(GameObject), true);

			EditorGUILayout.Separator();

			if (GUILayout.Button("Convert"))
			{
				bool isOk = true;

				if (rootObject == null)
				{
					EditorUtility.DisplayDialog("Error", "You must choose a root object", "OK");
					isOk = false;
				}

				if (string.IsNullOrWhiteSpace(path))
				{
					EditorUtility.DisplayDialog("Error", "You must choose a destination path", "OK");
					isOk = false;
				}

				if (isOk)
				{
					assetsPaths = AssetDatabase.GetAllAssetPaths();
					ConvertToGameObjectRecursive(rootObject, path);
					EditorUtility.DisplayDialog("Complete", "It's done!", "OK");
				}
			}
		}
	}
}