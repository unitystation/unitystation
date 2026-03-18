using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class AudioPrefabCreator : EditorWindow
{
	private enum SoundType { Spatial, Global }

	private AudioClip droppedClip;
	private string prefabName = "";
	private bool nameManuallyEdited;
	private SoundType soundType = SoundType.Spatial;
	private int groupIndex;
	private List<string> groupNames;
	private string outputFolder = "Assets/Prefabs/";
	private Object lastCreatedPrefab;

	[MenuItem("Tools/Audio Prefab Creator")]
	public static void ShowWindow()
	{
		var window = GetWindow<AudioPrefabCreator>("Audio Prefab Creator");
		window.minSize = new Vector2(400, 350);
	}

	private void OnEnable()
	{
		RefreshGroupNames();
	}

	private void RefreshGroupNames()
	{
		var settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings == null)
		{
			groupNames = new List<string> { "(No Addressable Settings)" };
			return;
		}
		groupNames = settings.groups.Select(g => g.Name).ToList();
	}

	private void OnGUI()
	{
		EditorGUILayout.LabelField("Audio Prefab Creator", EditorStyles.boldLabel);
		EditorGUILayout.Space(5);

		DrawDropArea();
		EditorGUILayout.Space(5);

		DrawSettings();
		EditorGUILayout.Space(10);

		DrawButtons();
		DrawLastCreated();
	}

	private void DrawDropArea()
	{
		var dropArea = GUILayoutUtility.GetRect(0, 80, GUILayout.ExpandWidth(true));
		var style = new GUIStyle(GUI.skin.box)
		{
			alignment = TextAnchor.MiddleCenter,
			fontSize = 14,
			fontStyle = FontStyle.Bold
		};

		string label = droppedClip != null
			? $"Clip: {droppedClip.name}"
			: "Drag & Drop Audio Clip Here";
		GUI.Box(dropArea, label, style);

		var currentEvent = Event.current;
		if (dropArea.Contains(currentEvent.mousePosition) == false) return;

		switch (currentEvent.type)
		{
			case EventType.DragUpdated:
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				currentEvent.Use();
				break;

			case EventType.DragPerform:
				DragAndDrop.AcceptDrag();
				foreach (var obj in DragAndDrop.objectReferences)
				{
					if (obj is AudioClip clip)
					{
						droppedClip = clip;
						if (nameManuallyEdited == false)
						{
							prefabName = clip.name;
						}
					}
				}
				currentEvent.Use();
				break;
		}
	}

	private void DrawSettings()
	{
		EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

		EditorGUI.BeginChangeCheck();
		prefabName = EditorGUILayout.TextField("Prefab Name", prefabName);
		if (EditorGUI.EndChangeCheck())
		{
			nameManuallyEdited = true;
		}

		soundType = (SoundType)EditorGUILayout.EnumPopup("Sound Type", soundType);

		groupIndex = EditorGUILayout.Popup("Addressable Group", groupIndex, groupNames.ToArray());

		EditorGUILayout.BeginHorizontal();
		outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
		if (GUILayout.Button("...", GUILayout.MaxWidth(30)))
		{
			string selected = EditorUtility.OpenFolderPanel("Select output folder", Application.dataPath, "Prefabs");
			if (string.IsNullOrEmpty(selected) == false)
			{
				if (selected.Contains(Application.dataPath))
				{
					outputFolder = selected.Replace(Application.dataPath, "Assets") + "/";
				}
				else
				{
					EditorUtility.DisplayDialog("Error", "Folder must be inside the Assets directory.", "OK");
				}
			}
		}
		EditorGUILayout.EndHorizontal();

		string presetInfo = soundType == SoundType.Spatial
			? "Spatial: spatialBlend=1, maxDistance=15, dopplerLevel=1"
			: "Global: spatialBlend=0, maxDistance=500, dopplerLevel=0";
		EditorGUILayout.HelpBox($"Preset: {presetInfo}\nVolume: 0.4 | Rolloff: Logarithmic | Mixer: Master", MessageType.Info);
	}

	private void DrawButtons()
	{
		GUI.enabled = droppedClip != null && string.IsNullOrEmpty(prefabName) == false;
		if (GUILayout.Button("Create", GUILayout.Height(35)))
		{
			CreateAudioPrefab();
		}
		GUI.enabled = true;
	}

	private void CreateAudioPrefab()
	{
		if (droppedClip == null)
		{
			Debug.LogError("[AudioPrefabCreator] No audio clip selected.");
			return;
		}

		if (string.IsNullOrWhiteSpace(prefabName))
		{
			Debug.LogError("[AudioPrefabCreator] Prefab name cannot be empty.");
			return;
		}

		if (AssetDatabase.IsValidFolder(outputFolder.TrimEnd('/')) == false)
		{
			Debug.LogError($"[AudioPrefabCreator] Output folder does not exist: {outputFolder}");
			return;
		}

		var go = new GameObject(prefabName);
		var audioSource = go.AddComponent<AudioSource>();
		ApplyAudioSourcePreset(audioSource, soundType, droppedClip);

		string savePath = $"{outputFolder.TrimEnd('/')}/{prefabName}.prefab";
		var prefab = PrefabUtility.SaveAsPrefabAsset(go, savePath);
		DestroyImmediate(go);

		if (prefab == null)
		{
			Debug.LogError($"[AudioPrefabCreator] Failed to create prefab at: {savePath}");
			return;
		}

		var settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings != null && groupIndex < settings.groups.Count)
		{
			string guid = AssetDatabase.AssetPathToGUID(savePath);
			var group = settings.groups[groupIndex];
			settings.CreateOrMoveEntry(guid, group);
			Debug.Log($"[AudioPrefabCreator] Added to addressable group: {group.Name}");
		}

		AssetDatabase.SaveAssets();
		lastCreatedPrefab = prefab;
		Debug.Log($"[AudioPrefabCreator] Created {soundType} audio prefab at: {savePath}");

		droppedClip = null;
		prefabName = "";
		nameManuallyEdited = false;
	}

	private void DrawLastCreated()
	{
		if (lastCreatedPrefab == null) return;

		EditorGUILayout.Space(5);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField($"Last: {lastCreatedPrefab.name}", EditorStyles.miniLabel);
		if (GUILayout.Button("Select", GUILayout.MaxWidth(60)))
		{
			EditorGUIUtility.PingObject(lastCreatedPrefab);
			Selection.activeObject = lastCreatedPrefab;
		}
		EditorGUILayout.EndHorizontal();
	}

	private static void ApplyAudioSourcePreset(AudioSource source, SoundType type, AudioClip clip)
	{
		source.clip = clip;
		source.playOnAwake = false;
		source.loop = false;
		source.volume = 0.4f;
		source.pitch = 1f;
		source.priority = 128;
		source.rolloffMode = AudioRolloffMode.Logarithmic;
		source.bypassEffects = false;
		source.bypassListenerEffects = false;
		source.bypassReverbZones = false;

		string[] mixerGuids = AssetDatabase.FindAssets("t:AudioMixer");
		foreach (string guid in mixerGuids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			var mixer = AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>(path);
			if (mixer == null) continue;
			var groups = mixer.FindMatchingGroups("Master");
			if (groups.Length > 0)
			{
				source.outputAudioMixerGroup = groups[0];
				break;
			}
		}

		if (type == SoundType.Spatial)
		{
			source.spatialBlend = 1f;
			source.minDistance = 1f;
			source.maxDistance = 15f;
			source.dopplerLevel = 1f;
			source.spread = 0f;
			source.spatialize = false;
		}
		else
		{
			source.spatialBlend = 0f;
			source.minDistance = 1f;
			source.maxDistance = 500f;
			source.dopplerLevel = 0f;
			source.spread = 0f;
			source.spatialize = false;
		}
	}
}
