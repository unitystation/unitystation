using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
public class AutoCreatePrefab : EditorWindow
{
	private GameObject prefabToDuplicate;
	private List<AudioClip> audioClipsToReplace = new List<AudioClip>();
	private string savePath = "Assets/Prefabs/";

	[MenuItem("Tools/Prefab Duplicator")]
    public static void ShowWindow()
    {
	    GetWindow<AutoCreatePrefab>("Prefab Duplicator");
    }

    private void OnGUI()
    {
	    GUILayout.Label("Prefab Duplicator", EditorStyles.boldLabel);

	    if (audioClipsToReplace.Count == 0 && prefabToDuplicate == null)
	    {
		    EditorGUILayout.HelpBox("Please select the target prefab to duplicate, then select audio clips to create prefabs for before using this tool.", MessageType.Info);
	    }

	    prefabToDuplicate = EditorGUILayout.ObjectField("Prefab to Duplicate", prefabToDuplicate, typeof(GameObject), false) as GameObject;

	    DisplayAudioClipsList();

	    GUILayout.BeginHorizontal();
	    GUILayout.Label("Save Path:", GUILayout.Width(80));
	    savePath = EditorGUILayout.TextField(savePath);
	    if (GUILayout.Button("Change", GUILayout.Width(80)))
	    {
		    OpenSavePathPicker();
	    }
	    GUILayout.EndHorizontal();

	    if (GUILayout.Button("Duplicate and Replace Audio"))
	    {
		    DuplicateAndReplace();
	    }

	    if (GUILayout.Button("Find and Create Prefabs for All Audio Clips"))
	    {
		    FindAndCreatePrefabsForAllAudioClips();
	    }

	    HandleDragAndDrop();
    }

    private void HandleDragAndDrop()
    {
	    if (Event.current.type != EventType.DragUpdated && Event.current.type != EventType.DragPerform) return;
	    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
	    if (Event.current.type == EventType.DragPerform)
	    {
		    DragAndDrop.AcceptDrag();

		    foreach (Object obj in DragAndDrop.objectReferences)
		    {
			    if (obj is AudioClip clip && audioClipsToReplace.Contains(clip) == false)
			    {
				    audioClipsToReplace.Add(clip);
			    }
		    }
	    }

	    Event.current.Use();
    }

    private void DisplayAudioClipsList()
    {
	    EditorGUILayout.LabelField("Audio Clips to Replace:");
	    EditorGUI.indentLevel++;
	    for (int i = 0; i < audioClipsToReplace.Count; i++)
	    {
		    audioClipsToReplace[i] = EditorGUILayout.ObjectField("Audio Clip " + i, audioClipsToReplace[i], typeof(AudioClip), false) as AudioClip;
	    }
	    EditorGUI.indentLevel--;
	    if (GUILayout.Button("Add Audio Clip"))
	    {
		    audioClipsToReplace.Add(null);
	    }
	    if (GUILayout.Button("Remove Last Audio Clip") && audioClipsToReplace.Count > 0)
	    {
		    audioClipsToReplace.RemoveAt(audioClipsToReplace.Count - 1);
	    }
    }

    private void DuplicateAndReplace()
    {
	    if (prefabToDuplicate == null)
	    {
		    Debug.LogError("Prefab to duplicate is not selected!");
		    return;
	    }

	    if (audioClipsToReplace.Count == 0)
	    {
		    Debug.LogError("No audio clips selected for replacement!");
		    return;
	    }

	    foreach (AudioClip audioClip in audioClipsToReplace)
	    {
		    if (audioClip == null)
		    {
			    Debug.LogError("One or more audio clips are not selected!");
			    return;
		    }
	    }

	    foreach (AudioClip audioClip in audioClipsToReplace)
	    {
		    GameObject duplicatedPrefab = PrefabUtility.InstantiatePrefab(prefabToDuplicate) as GameObject;
		    if (duplicatedPrefab != null)
		    {
			    AudioSource audioSource = duplicatedPrefab.GetComponent<AudioSource>();
			    if (audioSource != null)
			    {
				    audioSource.clip = audioClip;
			    }
			    else
			    {
				    Debug.LogWarning("AudioSource component not found on duplicated prefab!");
			    }

			    string prefabName = audioClip.name + ".prefab";
			    string pathToSave = savePath + prefabName;
			    PrefabUtility.SaveAsPrefabAsset(duplicatedPrefab, pathToSave);
			    DestroyImmediate(duplicatedPrefab);
			    Debug.Log("Prefab duplicated and saved at: " + pathToSave);
		    }
		    else
		    {
			    Debug.LogError("Failed to duplicate prefab!");
		    }
	    }
    }

    private void OpenSavePathPicker()
    {
	    string newSavePath = EditorUtility.OpenFolderPanel("Choose Save Path", "", "");
	    if (!string.IsNullOrEmpty(newSavePath))
	    {
		    savePath = newSavePath + "/";
	    }
    }

    private void CreatePrefabForAudioClip(AudioClip audioClip, string baseSavePath)
    {
	    GameObject duplicatedPrefab = PrefabUtility.InstantiatePrefab(prefabToDuplicate) as GameObject;
	    if (duplicatedPrefab != null)
	    {
		    AudioSource audioSource = duplicatedPrefab.GetComponent<AudioSource>();
		    if (audioSource != null)
		    {
			    audioSource.clip = audioClip;
		    }
		    else
		    {
			    Debug.LogWarning("AudioSource component not found on duplicated prefab!");
		    }

		    string relativePath = AssetDatabase.GetAssetPath(audioClip);
		    string directoryPath = Path.GetDirectoryName(relativePath)?.Replace("Audio Files", baseSavePath);

		    if (!Directory.Exists(directoryPath))
		    {
			    Directory.CreateDirectory(directoryPath);
		    }

		    string prefabName = Path.GetFileNameWithoutExtension(audioClip.name) + ".prefab";
		    string pathToSave = Path.Combine(directoryPath, prefabName);
		    PrefabUtility.SaveAsPrefabAsset(duplicatedPrefab, pathToSave);
		    DestroyImmediate(duplicatedPrefab);
		    Debug.Log("Prefab duplicated and saved at: " + pathToSave);
	    }
	    else
	    {
		    Debug.LogError("Failed to duplicate prefab!");
	    }
    }

    private void FindAndCreatePrefabsForAllAudioClips()
    {
	    if (prefabToDuplicate == null)
	    {
		    Debug.LogError("Prefab to duplicate is not selected!");
		    return;
	    }

	    string[] audioClipGUIDs = AssetDatabase.FindAssets("t:AudioClip");
	    foreach (string guid in audioClipGUIDs)
	    {
		    string path = AssetDatabase.GUIDToAssetPath(guid);
		    AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
		    if (audioClip == null) continue;
		    if (PrefabAlreadyExistsForAudioFile(audioClip.name) == false)
		    {
			    CreatePrefabForAudioClip(audioClip, "Prefabs/");
		    }
		    else
		    {
			    Debug.Log($"Prefab already exists for {audioClip.name}.. Skipping..");
		    }
	    }
    }

    private bool PrefabAlreadyExistsForAudioFile(string audioClip)
    {
	    string[] prefabGUIDs = AssetDatabase.FindAssets(audioClip + " t:Prefab");
	    foreach (string prefabGuid in prefabGUIDs)
	    {
		    string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
		    if (Path.GetFileNameWithoutExtension(prefabPath) != audioClip) continue;
		    return true;
	    }
	    return false;
    }

}
#endif