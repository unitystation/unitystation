using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace US13.Core.Editor.Tools
{
	public class SpriteImportTool : EditorWindow
	{
		private readonly List<Texture2D> droppedTextures = new();
		private float secondDelay = 0.09f;
		private Vector2 scrollPosition;
		private string spriteDataName = "";
		private bool nameManuallyEdited;

		private TextAsset animationJson;
		private List<float> parsedDelays;
		private ReorderableList reorderableList;
		private bool isSpriteSheet;
		private int frameWidth = 32;

		[MenuItem("Tools/Sprites/Sprite Import Tool")]
		public static void ShowWindow()
		{
			var window = GetWindow<SpriteImportTool>("Sprite Import Tool");
			window.minSize = new Vector2(400, 500);
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Sprite Import Tool", EditorStyles.boldLabel);
			EditorGUILayout.Space(5);

			DrawDropArea();
			EditorGUILayout.Space(5);

			DrawTextureList();
			EditorGUILayout.Space(5);

			DrawSettings();
			EditorGUILayout.Space(10);

			DrawButtons();
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
			GUI.Box(dropArea, "Drag & Drop Textures / Animation JSON Here", style);

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
					foreach (var draggedObject in DragAndDrop.objectReferences)
					{
						if (draggedObject is Texture2D texture && droppedTextures.Contains(texture) == false)
						{
							droppedTextures.Add(texture);
							RebuildReorderableList();
							if (nameManuallyEdited == false && droppedTextures.Count == 1)
							{
								spriteDataName = texture.name;
							}
						}
						else if (draggedObject is TextAsset jsonAsset)
						{
							TryParseAnimationJson(jsonAsset);
						}
					}
					currentEvent.Use();
					break;
			}
		}

		private void TryParseAnimationJson(TextAsset jsonAsset)
		{
			var path = AssetDatabase.GetAssetPath(jsonAsset);
			if (path.EndsWith(".json") == false)
			{
				Debug.LogWarning("[SpriteImportTool] Dropped file is not a .json file, ignoring.");
				return;
			}

			try
			{
				var delays = ParseDelaysFromJson(jsonAsset.text);
				if (delays == null || delays.Count == 0)
				{
					Debug.LogWarning("[SpriteImportTool] JSON does not contain valid Delays data.");
					return;
				}

				animationJson = jsonAsset;
				parsedDelays = delays;
				Debug.Log($"[SpriteImportTool] Loaded animation JSON with {delays.Count} delay(s).");
			}
			catch (System.Exception e)
			{
				Debug.LogError($"[SpriteImportTool] Failed to parse animation JSON: {e.Message}");
			}
		}

		/// <summary>
		/// Parses the "Delays" field from DMI exporter JSON.
		/// Format: {"Delays": [[0.1, 0.2, 0.3], [0.1, 0.2, 0.3]], ...}
		/// Flattens all variant arrays into a single list.
		/// </summary>
		private static List<float> ParseDelaysFromJson(string json)
		{
			var match = Regex.Match(json, @"""Delays""\s*:\s*\[([\s\S]*)\]\s*,", RegexOptions.None);
			if (match.Success == false) return null;

			var numberMatches = Regex.Matches(match.Groups[1].Value, @"[\d]+\.[\d]+|[\d]+");
			var delays = new List<float>();
			foreach (Match numMatch in numberMatches)
			{
				if (float.TryParse(numMatch.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
				{
					delays.Add(val);
				}
			}

			return delays;
		}

		private void RebuildReorderableList()
		{
			reorderableList = new ReorderableList(droppedTextures, typeof(Texture2D), true, true, false, true)
			{
				drawHeaderCallback = rect =>
				{
					EditorGUI.LabelField(rect, $"Textures ({droppedTextures.Count})");
				},
				drawElementCallback = (rect, index, _, _) =>
				{
					var texture = droppedTextures[index];
					var previewRect = new Rect(rect.x, rect.y, rect.height, rect.height);
					var labelRect = new Rect(rect.x + rect.height + 4, rect.y, rect.width - rect.height - 4, rect.height);
					EditorGUI.DrawPreviewTexture(previewRect, texture, null, ScaleMode.ScaleToFit);
					EditorGUI.LabelField(labelRect, texture.name);
				},
				elementHeight = 32
			};
		}

		private void DrawTextureList()
		{
			if (droppedTextures.Count == 0)
			{
				EditorGUILayout.LabelField($"Textures ({droppedTextures.Count}):", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox("No textures added yet. Drag and drop textures above.", MessageType.Info);
				return;
			}

			if (reorderableList == null)
			{
				RebuildReorderableList();
			}

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(200));
			reorderableList?.DoLayoutList();
			EditorGUILayout.EndScrollView();
		}

		private void DrawSettings()
		{
			EditorGUILayout.LabelField("Settings:", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();
			spriteDataName = EditorGUILayout.TextField("SpriteDataSO Name", spriteDataName);
			if (EditorGUI.EndChangeCheck())
			{
				nameManuallyEdited = true;
			}

			isSpriteSheet = EditorGUILayout.Toggle("Sprite Sheet Mode", isSpriteSheet);
			if (isSpriteSheet)
			{
				frameWidth = EditorGUILayout.IntField("Frame Width (px)", frameWidth);
				if (frameWidth < 1) frameWidth = 1;
			}

			bool hasJson = parsedDelays != null;

			if (droppedTextures.Count > 1)
			{
				if (hasJson)
				{
					GUI.enabled = false;
					EditorGUILayout.FloatField("Frame Delay (seconds)", secondDelay);
					GUI.enabled = true;

					EditorGUILayout.HelpBox(
						$"Timings read from JSON: {animationJson.name} ({parsedDelays.Count} delay values)",
						MessageType.Info);

					int delayCount = parsedDelays.Count;
					if (delayCount > droppedTextures.Count)
					{
						EditorGUILayout.HelpBox(
							$"JSON has {delayCount} timing values but only {droppedTextures.Count} sprites are queued. Extra timings will be ignored.",
							MessageType.Warning);
					}
				}
				else
				{
					secondDelay = EditorGUILayout.FloatField("Frame Delay (seconds)", secondDelay);
				}
			}

			if (hasJson)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField($"Animation JSON: {animationJson.name}", EditorStyles.miniLabel);
				if (GUILayout.Button("Remove", GUILayout.Width(60)))
				{
					animationJson = null;
					parsedDelays = null;
				}
				EditorGUILayout.EndHorizontal();
			}

			var spriteMode = isSpriteSheet ? "Multiple (sprite sheet)" : "Single";
			EditorGUILayout.HelpBox(
				$"Import mode: {spriteMode}\n" +
				"PPU: 32 | Filter: Point | Format: RGBA 32 bit\n" +
				"Mesh Type: Full Rect | Read/Write: Enabled",
				MessageType.None);
		}

		private void DrawButtons()
		{
			EditorGUILayout.BeginHorizontal();

			GUI.enabled = droppedTextures.Count > 0;
			if (GUILayout.Button("Go", GUILayout.Height(35)))
			{
				ProcessTextures();
			}
			GUI.enabled = true;

			if (GUILayout.Button("Clear", GUILayout.Height(35)))
			{
				droppedTextures.Clear();
				spriteDataName = "";
				nameManuallyEdited = false;
				animationJson = null;
				parsedDelays = null;
				reorderableList = null;
				isSpriteSheet = false;
			}

			EditorGUILayout.EndHorizontal();
		}

		private void ProcessTextures()
		{
			if (droppedTextures.Count == 0)
			{
				Debug.LogError("[SpriteImportTool] No textures to process.");
				return;
			}

			ApplyImportSettings();
			CreateSpriteDataSO();
		}

		private void ApplyImportSettings()
		{
			foreach (var texture in droppedTextures)
			{
				string path = AssetDatabase.GetAssetPath(texture);
				var importer = AssetImporter.GetAtPath(path) as TextureImporter;
				if (importer == null)
				{
					Debug.LogWarning($"[SpriteImportTool] Could not get TextureImporter for: {path}");
					continue;
				}

				importer.textureType = TextureImporterType.Sprite;
				importer.spriteImportMode = isSpriteSheet
					? SpriteImportMode.Multiple
					: SpriteImportMode.Single;
				importer.spritePixelsPerUnit = 32;
				importer.filterMode = FilterMode.Point;
				importer.isReadable = true;
				importer.mipmapEnabled = false;

				var meshSettings = new TextureImporterSettings();
				importer.ReadTextureSettings(meshSettings);
				meshSettings.spriteMeshType = SpriteMeshType.FullRect;
				importer.SetTextureSettings(meshSettings);

				if (isSpriteSheet)
				{
					int texWidth = texture.width;
					int texHeight = texture.height;
					int frameCount = texWidth / frameWidth;
					var spriteSheet = new SpriteMetaData[frameCount];
					for (int i = 0; i < frameCount; i++)
					{
						spriteSheet[i] = new SpriteMetaData
						{
							name = $"{texture.name}_{i}",
							rect = new Rect(i * frameWidth, 0, frameWidth, texHeight),
							alignment = 0,
							pivot = new Vector2(0.5f, 0.5f)
						};
					}
					importer.spritesheet = spriteSheet;
				}

				var platformSettings = importer.GetDefaultPlatformTextureSettings();
				platformSettings.format = TextureImporterFormat.RGBA32;
				platformSettings.overridden = true;
				importer.SetPlatformTextureSettings(platformSettings);

				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
			}

			Debug.Log($"[SpriteImportTool] Applied import settings to {droppedTextures.Count} texture(s).");
		}

		private void CreateSpriteDataSO()
		{
			var sortedTextures = droppedTextures.ToList();

			var sprites = new List<Sprite>();
			foreach (var texture in sortedTextures)
			{
				string path = AssetDatabase.GetAssetPath(texture);
				var assets = AssetDatabase.LoadAllAssetsAtPath(path);
				foreach (var asset in assets)
				{
					if (asset is Sprite sprite)
					{
						sprites.Add(sprite);
					}
				}
			}

			if (sprites.Count == 0)
			{
				Debug.LogError("[SpriteImportTool] No sprites found after import. Check that textures are set to Sprite type.");
				return;
			}

			var delays = parsedDelays;

			var frames = new List<SpriteDataSO.Frame>();
			for (int i = 0; i < sprites.Count; i++)
			{
				float delay = delays != null && i < delays.Count ? delays[i] : secondDelay;
				frames.Add(new SpriteDataSO.Frame
				{
					sprite = sprites[i],
					secondDelay = delay
				});
			}

			var spriteDataSo = CreateInstance<SpriteDataSO>();
			spriteDataSo.Variance = new List<SpriteDataSO.Variant>
			{
				new() { Frames = frames }
			};

			spriteDataSo.ForceSetID();

			string firstTexturePath = AssetDatabase.GetAssetPath(sortedTextures[0]);
			string directory = System.IO.Path.GetDirectoryName(firstTexturePath);
			string assetName = string.IsNullOrEmpty(spriteDataName)
				? sortedTextures[0].name
				: spriteDataName;
			assetName = assetName.Trim();
			string savePath = $"{directory}/{assetName}.asset";

			AssetDatabase.CreateAsset(spriteDataSo, savePath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			EditorGUIUtility.PingObject(spriteDataSo);
			Debug.Log($"[SpriteImportTool] Created SpriteDataSO at: {savePath} with {sprites.Count} frame(s).");
		}

	}
}
