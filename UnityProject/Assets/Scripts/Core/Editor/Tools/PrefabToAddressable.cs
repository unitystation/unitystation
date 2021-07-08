using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// Will convert all prefabs in a path to addressable and put them in chosen Addressable Group with chosen labels.
/// </summary>
public class PrefabToAddressable : EditorWindow
{
	private string path;
	private int groupIndex;
	private List<string> groupNames;
	private Dictionary<string, bool> labels;
	private string[] assetsPaths;

	[MenuItem("Tools/Prefab to Addressable Converter")]
	private static void Init()
	{
		// Get existing open window or if none, make a new one:
		PrefabToAddressable window = (PrefabToAddressable)GetWindow(typeof(PrefabToAddressable));
		window.titleContent.text = "Prefab to Addressable Converter";
		window.Show();
	}

	private void Awake()
	{
		AddressableAssetSettings assetSettings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;

		labels = new Dictionary<string, bool>();

		foreach (string label in assetSettings.GetLabels())
			labels.Add(label, false);

		groupNames = new List<string>();
		groupNames.AddRange(assetSettings.groups.Select(p => p.Name));
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

	private void CreateAddressable(string objGuid)
	{
		AddressableAssetSettings assetSettings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
		AddressableAssetGroup addressableAssetGroup = assetSettings.groups[groupIndex];

		if (!addressableAssetGroup.entries.Any(p => p.guid == objGuid))
		{
			AddressableAssetEntry addressableAssetEntry = assetSettings.CreateOrMoveEntry(objGuid, addressableAssetGroup);
			foreach (string label in labels.Where(p => p.Value).Select(q => q.Key))
				addressableAssetEntry.labels.Add(label);
		}
	}

	private void CreateAddressableRecursive(string path)
	{
		// Seriously, that's the only way I found to list all prefabs in a given path.		
		foreach (string prefabPath in assetsPaths.Where(p => p.Contains(path) && p.Contains(".prefab")))
			CreateAddressable(AssetDatabase.AssetPathToGUID(prefabPath));

		string[] subFolders = AssetDatabase.GetSubFolders(path);

		foreach (string subFolder in subFolders)
			CreateAddressableRecursive(subFolder);
	}

	void OnGUI()
	{
		EditorGUILayout.HelpBox("Choose the root path where the prefabs are found", MessageType.Info);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Prefab root folder:", GUILayout.MaxWidth(75.0f));

		path = EditorGUILayout.TextField(path, EditorStyles.textField);

		if (GUILayout.Button("...", GUILayout.MaxWidth(30.0f)))
		{
			SelectPath();
		}

		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Separator();
		EditorGUILayout.HelpBox("Choose the Addressable Group in which the Prefab will be added", MessageType.Info);

		groupIndex = EditorGUILayout.Popup(groupIndex, groupNames.ToArray());

		EditorGUILayout.Separator();
		EditorGUILayout.HelpBox("Choose a list of labels to add to the addressables", MessageType.Info);

		List<string> keys = labels.Select(p => p.Key).ToList();

		foreach (string labelKey in keys)
		{
			labels[labelKey] = EditorGUILayout.Toggle(labelKey, labels[labelKey]);
		}

		EditorGUILayout.Separator();

		if (GUILayout.Button("Convert"))
		{
			bool isOk = true;

			if (string.IsNullOrWhiteSpace(path))
			{
				EditorUtility.DisplayDialog("Error", "You must choose a destination path", "OK");
				isOk = false;
			}

			if (isOk)
			{
				assetsPaths = AssetDatabase.GetAllAssetPaths();
				CreateAddressableRecursive(path);
				EditorUtility.DisplayDialog("Complete", "It's done!", "OK");
			}
		}
	}
}