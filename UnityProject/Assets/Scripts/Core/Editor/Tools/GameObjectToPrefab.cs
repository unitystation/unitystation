using System.IO;
using Logs;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Will convert a traditional GameObject and all its child objects to prefabs.
/// </summary>
public class GameObjectToPrefab : EditorWindow
{
	private string path;
	private bool createEmptyPrefabs = false;
	private bool createFolders = false;
	private GameObject parentObject;

	[MenuItem("Tools/GameObject to Prefab Converter")]
	private static void Init()
	{
		// Get existing open window or if none, make a new one:
		GameObjectToPrefab window = (GameObjectToPrefab)GetWindow(typeof(GameObjectToPrefab));
		window.titleContent.text = "GameObject to Prefab Converter";
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

	private void CreatePrefabsRecursive(GameObject parentObject, string currentPath)
	{
		string newPath = currentPath;
		int childCount = parentObject.transform.childCount;

		if ((childCount > 0) && createFolders)
		{
			newPath += "/" + parentObject.name;
			Loggy.Log($"Creating folder {newPath}", Category.Editor);
			Directory.CreateDirectory(newPath);
		}

		Component[] components = parentObject.GetComponents<Component>();
		bool hasComponents = !((components.Length == 1) && (components[0].GetType() == typeof(Transform)));
		if (createEmptyPrefabs || hasComponents)
		{
			Loggy.Log($"Saving Prefab {parentObject.name}", Category.Editor);
			PrefabUtility.SaveAsPrefabAsset(parentObject, $"{newPath}/{parentObject.name}.prefab");
		}

		for (int childIndex = 0; childIndex < childCount; childIndex++)
		{
			CreatePrefabsRecursive(parentObject.transform.GetChild(childIndex).gameObject, newPath);
		}
	}

	void OnGUI()
	{
		EditorGUILayout.HelpBox("Choose the GameObject that contains the childs you want to convert to prefabs", MessageType.Info);
		parentObject = (GameObject)EditorGUILayout.ObjectField(parentObject, typeof(GameObject), true);

		EditorGUILayout.Separator();

		EditorGUILayout.HelpBox("Choose the path where the prefabs will be generated", MessageType.Info);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Save folder:", GUILayout.MaxWidth(75.0f));

		path = EditorGUILayout.TextField(path, EditorStyles.textField);

		if (GUILayout.Button("...", GUILayout.MaxWidth(30.0f)))
		{
			SelectPath();
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Separator();

		EditorGUILayout.HelpBox("Click this if you want objects with no components other than Transform to be also converted as prefabs", MessageType.Info);

		createEmptyPrefabs = EditorGUILayout.Toggle("Create Empty Prefabs", createEmptyPrefabs);

		EditorGUILayout.Separator();

		EditorGUILayout.HelpBox("Click this if you want objects with children to become subfolders\nIf you don't check this, everything will be created at the same level", MessageType.Info);

		createFolders = EditorGUILayout.Toggle("Create Subfolders", createFolders);

		EditorGUILayout.Separator();

		if (GUILayout.Button("Convert"))
		{
			bool isOk = true;
			if (parentObject == null)
			{
				EditorUtility.DisplayDialog("Error", "You must choose a parent object", "OK");
				isOk = false;
			}

			if (string.IsNullOrWhiteSpace(path))
			{
				EditorUtility.DisplayDialog("Error", "You must choose a destination path", "OK");
				isOk = false;
			}

			if (isOk)
			{
				CreatePrefabsRecursive(parentObject, path);
				EditorUtility.DisplayDialog("Complete", "It's done!", "OK");
			}
		}
	}
}