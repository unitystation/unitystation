using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace US13.Core.Editor.Tools.UI
{
	public class ConvertLegacyTextToTmpPro : EditorWindow
	{
		private SearchScope _scope = SearchScope.ActiveScene;
		private TMP_FontAsset _tmpFont = null;
		private bool _previewOnly = true;
		private bool _preserveLayout = true;
		private bool _convertDisabled = true;
		private bool _backupPrefabs = true;
		private string _filterName = "";

		private List<Text> _found = new List<Text>();
		private List<ConversionRecord> _log = new List<ConversionRecord>();
		private Vector2 _foundScroll;
		private Vector2 _logScroll;
		private int _tab;

		private GUIStyle _headerStyle;
		private GUIStyle _sectionStyle;
		private GUIStyle _successStyle;
		private GUIStyle _errorStyle;
		private GUIStyle _previewBannerStyle;
		private bool _stylesBuilt;

		[MenuItem("Tools/Text → TMP Converter")]
		public static void Open()
		{
			GetWindow<ConvertLegacyTextToTmpPro>("Text → TMP Converter").minSize = new Vector2(520, 600);
		}

		// ─────────────────────────────────────────────────────────────────────
		private void BuildStyles()
		{
			if (_stylesBuilt) return;
			_stylesBuilt = true;

			_headerStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 14,
				alignment = TextAnchor.MiddleLeft
			};

			_sectionStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 11
			};

			_successStyle = new GUIStyle(EditorStyles.label)
			{
				normal = { textColor = new Color(0.2f, 0.8f, 0.4f) },
				fontStyle = FontStyle.Bold
			};

			_errorStyle = new GUIStyle(EditorStyles.label)
			{
				normal = { textColor = new Color(1f, 0.35f, 0.35f) },
				fontStyle = FontStyle.Bold
			};

			_previewBannerStyle = new GUIStyle(EditorStyles.helpBox)
			{
				fontSize = 11,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleCenter,
				normal = { textColor = new Color(1f, 0.85f, 0.2f) }
			};
		}

		private void OnGUI()
		{
			BuildStyles();

			EditorGUILayout.Space(8);
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Space(8);
				GUILayout.Label("✦  Legacy Text  →  TextMeshPro  Converter", _headerStyle);
			}

			EditorGUILayout.Space(4);
			Divider();
			EditorGUILayout.Space(4);

			_tab = GUILayout.Toolbar(_tab,
				new[] { "⚙  Settings & Scan", "📋  Results", "🗒  Log" },
				GUILayout.Height(28));
			EditorGUILayout.Space(6);

			switch (_tab)
			{
				case 0: DrawSettings(); break;
				case 1: DrawResults(); break;
				case 2: DrawLog(); break;
			}
		}

		private void DrawSettings()
		{
			GUILayout.Label("Search Scope", _sectionStyle);
			using (new EditorGUI.IndentLevelScope(1))
			{
				_scope = (SearchScope)EditorGUILayout.EnumPopup("Scope", _scope);
				_filterName = EditorGUILayout.TextField("Name Filter (optional)", _filterName);
				_convertDisabled = EditorGUILayout.Toggle("Include Disabled GameObjects", _convertDisabled);
			}

			EditorGUILayout.Space(8);
			GUILayout.Label("TMP Font", _sectionStyle);
			using (new EditorGUI.IndentLevelScope(1))
			{
				_tmpFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
					"Font Asset", _tmpFont, typeof(TMP_FontAsset), false);
				if (_tmpFont == null)
					EditorGUILayout.HelpBox(
						"No font selected. A default TMP font will be used if one is available in the project.",
						MessageType.Info);
			}

			EditorGUILayout.Space(8);
			GUILayout.Label("Conversion Options", _sectionStyle);
			using (new EditorGUI.IndentLevelScope(1))
			{
				_preserveLayout = EditorGUILayout.Toggle("Preserve Layout Component", _preserveLayout);
				_backupPrefabs = EditorGUILayout.Toggle("Backup Prefabs Before Edit", _backupPrefabs);
			}

			EditorGUILayout.Space(12);
			Divider();
			EditorGUILayout.Space(8);

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("🔍  Scan for Legacy Text", GUILayout.Height(32), GUILayout.Width(200)))
				{
					Scan();
					_tab = 1;
				}

				GUILayout.Space(8);

				using (new EditorGUI.DisabledScope(_found.Count == 0))
				{
					var label = _previewOnly ? "👁  Preview Conversion" : "⚡  Convert Now";
					if (GUILayout.Button(label, GUILayout.Height(32), GUILayout.Width(200)))
					{
						if (_previewOnly)
							PreviewConversion();
						else
							RunConversion();
						_tab = 2;
					}
				}

				GUILayout.FlexibleSpace();
			}

			EditorGUILayout.Space(6);

			// ── Preview toggle ────────────────────────────────────────────────
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				_previewOnly = EditorGUILayout.ToggleLeft(
					"Preview only (no changes applied)", _previewOnly, GUILayout.Width(260));
				GUILayout.FlexibleSpace();
			}

			if (!_previewOnly)
			{
				EditorGUILayout.Space(4);
				EditorGUILayout.HelpBox(
					"⚠  LIVE MODE — changes will be applied to your scene/prefabs.\n" +
					"Make sure your project is under version control before proceeding.",
					MessageType.Warning);
			}
		}


		private void DrawResults()
		{
			if (_found.Count == 0)
			{
				EditorGUILayout.HelpBox("No legacy Text components found yet. Run a scan first.", MessageType.Info);
				return;
			}

			GUILayout.Label($"Found {_found.Count} legacy Text component(s):", _sectionStyle);
			EditorGUILayout.Space(4);

			_foundScroll = EditorGUILayout.BeginScrollView(_foundScroll);
			foreach (var t in _found)
			{
				if (t == null) continue;
				using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
				{
					EditorGUILayout.ObjectField(t, typeof(Text), true);
					GUILayout.Label($"size:{t.fontSize}  \"{Truncate(t.text, 24)}\"",
						EditorStyles.miniLabel, GUILayout.Width(200));

					if (GUILayout.Button("Select", GUILayout.Width(56)))
					{
						Selection.activeGameObject = t.gameObject;
						EditorGUIUtility.PingObject(t.gameObject);
					}
				}
			}

			EditorGUILayout.EndScrollView();

			EditorGUILayout.Space(6);
			Divider();
			EditorGUILayout.Space(4);

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				if (_previewOnly)
					GUILayout.Label("▶  Switch to Settings tab and click \"Preview Conversion\"",
						EditorStyles.centeredGreyMiniLabel);
				else
					GUILayout.Label("▶  Switch to Settings tab and click \"Convert Now\"",
						EditorStyles.centeredGreyMiniLabel);

				GUILayout.FlexibleSpace();
			}
		}

		private void DrawLog()
		{
			if (_log.Count == 0)
			{
				EditorGUILayout.HelpBox("No conversion log yet.", MessageType.Info);
				return;
			}

			int ok = _log.Count(r => r.success);
			int fail = _log.Count(r => !r.success);

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Label($"✔  {ok} succeeded", _successStyle);
				GUILayout.Space(16);
				GUILayout.Label($"✖  {fail} failed", _errorStyle);
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Export CSV", GUILayout.Width(90)))
					ExportCSV();

				if (GUILayout.Button("Clear", GUILayout.Width(60)))
					_log.Clear();
			}

			Divider();
			EditorGUILayout.Space(4);

			_logScroll = EditorGUILayout.BeginScrollView(_logScroll);
			foreach (var r in _log)
			{
				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						GUILayout.Label(r.success ? "✔" : "✖",
							r.success ? _successStyle : _errorStyle, GUILayout.Width(18));
						GUILayout.Label(r.gameObjectPath, EditorStyles.boldLabel);
					}

					EditorGUILayout.LabelField("Text", Truncate(r.originalText, 60), EditorStyles.miniLabel);
					EditorGUILayout.LabelField("Font", $"{r.fontName}  {r.fontSize}pt", EditorStyles.miniLabel);
					EditorGUILayout.LabelField("Color", ColorUtility.ToHtmlStringRGBA(r.color), EditorStyles.miniLabel);

					if (!r.success)
						EditorGUILayout.LabelField("Error", r.error, _errorStyle);
				}

				EditorGUILayout.Space(2);
			}

			EditorGUILayout.EndScrollView();
		}

		private void Scan()
		{
			_found.Clear();
			var all = CollectTexts();

			foreach (var t in all)
			{
				if (t == null) continue;
				if (_convertDisabled == false && t.gameObject.activeInHierarchy == false) continue;
				if (_scope != SearchScope.Prefabs)
				{
					if (string.IsNullOrEmpty(_filterName) == false &&
					    t.gameObject.name.Contains(_filterName, StringComparison.InvariantCultureIgnoreCase) == false) continue;
				}
				else
				{
					if (string.IsNullOrEmpty(_filterName) == false)
					{
						var path = GetPath(t.gameObject);
						if (path.Contains(_filterName, StringComparison.InvariantCultureIgnoreCase) == false)
							continue;
					}
				}
				_found.Add(t);
			}

			Debug.Log($"[TMP Converter] Scan complete. Found {_found.Count} legacy Text component(s).");
		}

		private void PreviewConversion()
		{
			_log.Clear();
			foreach (var t in _found)
			{
				if (t == null) continue;
				_log.Add(BuildRecord(t, true, null));
			}
		}

		private void RunConversion()
		{
			if (!EditorUtility.DisplayDialog("Convert to TMP",
				    $"This will convert {_found.Count} Text component(s) to TextMeshProUGUI.\n\n" +
				    "This action modifies scene/prefab data. Ensure your work is saved/backed up.",
				    "Convert", "Cancel"))
				return;

			_log.Clear();

			foreach (var t in _found.ToList())
			{
				if (t == null) continue;
				ConversionRecord record = null;
				try
				{
					record = Convert(t);
				}
				catch (Exception ex)
				{
					record = BuildRecord(t, false, ex.Message);
				}

				_log.Add(record);
			}

			foreach (var scene in GetOpenScenes())
				EditorSceneManager.MarkSceneDirty(scene);

			Debug.Log($"[TMP Converter] Done. {_log.Count(r => r.success)} converted, " +
			          $"{_log.Count(r => !r.success)} failed.");
		}

		private ConversionRecord Convert(Text src)
		{
			var go = src.gameObject;

			// Capture properties
			string text = src.text;
			float fontSize = src.fontSize;
			Color color = src.color;
			var bold = src.fontStyle == FontStyle.Bold || src.fontStyle == FontStyle.BoldAndItalic;
			var italic = src.fontStyle == FontStyle.Italic || src.fontStyle == FontStyle.BoldAndItalic;
			bool richText = src.supportRichText;
			bool raycast = src.raycastTarget;
			bool autoSize = src.resizeTextForBestFit;
			float minSize = src.resizeTextMinSize;
			float maxSize = src.resizeTextMaxSize;
			TextAnchor anchor = src.alignment;
			var fontName = src.font != null ? src.font.name : "Arial";

			LayoutElement le = _preserveLayout ? go.GetComponent<LayoutElement>() : null;
			Undo.RegisterFullObjectHierarchyUndo(go, "Convert Text → TMP");
			Undo.DestroyObjectImmediate(src);

			var tmp = Undo.AddComponent<TextMeshProUGUI>(go);
			tmp.text = text;
			tmp.fontSize = fontSize;
			tmp.color = color;
			tmp.richText = richText;
			tmp.raycastTarget = raycast;
			tmp.enableAutoSizing = autoSize;
			tmp.fontSizeMin = minSize;
			tmp.fontSizeMax = maxSize;

			FontStyles style = FontStyles.Normal;
			if (bold) style |= FontStyles.Bold;
			if (italic) style |= FontStyles.Italic;
			tmp.fontStyle = style;

			tmp.alignment = MapAlignment(anchor);

			TMP_FontAsset fa = _tmpFont ?? FindDefaultTMPFont();
			if (fa != null) tmp.font = fa;

			if (PrefabUtility.IsPartOfPrefabInstance(go))
				PrefabUtility.RecordPrefabInstancePropertyModifications(tmp);

			return BuildRecord(go.name, GetPath(go), text, fontName, fontSize, color, bold, italic, anchor, true, null);
		}

		private IEnumerable<Text> CollectTexts()
		{
			switch (_scope)
			{
				case SearchScope.AllOpenScenes:
					return GetOpenScenes()
						.SelectMany(s => s.GetRootGameObjects())
						.SelectMany(go => go.GetComponentsInChildren<Text>(true));

				case SearchScope.Prefabs:
					return AssetDatabase.FindAssets("t:Prefab")
						.Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(
							AssetDatabase.GUIDToAssetPath(guid)))
						.Where(p => p != null)
						.SelectMany(p => p.GetComponentsInChildren<Text>(true));

				default: // ActiveScene
					var scene = EditorSceneManager.GetActiveScene();
					return scene.GetRootGameObjects()
						.SelectMany(go => go.GetComponentsInChildren<Text>(true));
			}
		}

		private static IEnumerable<Scene> GetOpenScenes()
		{
			for (var i = 0; i < EditorSceneManager.sceneCount; i++)
				yield return EditorSceneManager.GetSceneAt(i);
		}

		private static TextAlignmentOptions MapAlignment(TextAnchor a)
		{
			return a switch
			{
				TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
				TextAnchor.UpperCenter => TextAlignmentOptions.Top,
				TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
				TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
				TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
				TextAnchor.MiddleRight => TextAlignmentOptions.Right,
				TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
				TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
				TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
				_ => TextAlignmentOptions.Center
			};
		}

		private static TMP_FontAsset FindDefaultTMPFont()
		{
			var guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" });
			if (guids.Length > 0)
				return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
					AssetDatabase.GUIDToAssetPath(guids[0]));

			return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
		}

		private static string GetPath(GameObject go)
		{
			var parts = new List<string>();
			var t = go.transform;
			while (t != null)
			{
				parts.Insert(0, t.name);
				t = t.parent;
			}

			return string.Join("/", parts);
		}

		private static string Truncate(string s, int max)
		{
			return string.IsNullOrEmpty(s) ? ""
				: s.Length <= max ? s
				: s.Substring(0, max) + "…";
		}

		private ConversionRecord BuildRecord(Text src, bool success, string error)
		{
			return BuildRecord(
				src.gameObject.name, GetPath(src.gameObject),
				src.text,
				src.font != null ? src.font.name : "Arial",
				src.fontSize, src.color,
				src.fontStyle == FontStyle.Bold || src.fontStyle == FontStyle.BoldAndItalic,
				src.fontStyle == FontStyle.Italic || src.fontStyle == FontStyle.BoldAndItalic,
				src.alignment, success, error);
		}

		private static ConversionRecord BuildRecord(
			string name, string path, string text, string font,
			float fontSize, Color color, bool bold, bool italic,
			TextAnchor align, bool success, string error)
		{
			return new ConversionRecord
			{
				gameObjectPath = path,
				originalText = text,
				fontName = font,
				fontSize = fontSize,
				color = color,
				bold = bold,
				italic = italic,
				alignment = align,
				success = success,
				error = error ?? ""
			};
		}

		private void ExportCSV()
		{
			var path = EditorUtility.SaveFilePanel("Export Log", "", "tmp_conversion_log", "csv");
			if (string.IsNullOrEmpty(path)) return;

			var sb = new StringBuilder();
			sb.AppendLine("Status,Path,Text,Font,FontSize,Color,Bold,Italic,Alignment,Error");

			foreach (var r in _log)
				sb.AppendLine(string.Join(",",
					r.success ? "OK" : "FAIL",
					$"\"{r.gameObjectPath}\"",
					$"\"{r.originalText.Replace("\"", "'")}\"",
					r.fontName, r.fontSize,
					ColorUtility.ToHtmlStringRGBA(r.color),
					r.bold, r.italic, r.alignment,
					$"\"{r.error}\""));

			File.WriteAllText(path, sb.ToString());
			Debug.Log($"[TMP Converter] Log exported to {path}");
		}

		private static void Divider()
		{
			var rect = EditorGUILayout.GetControlRect(false, 1);
			EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
		}
	}

[Serializable]
public class ConversionRecord
{
	public string scenePath;
	public string gameObjectPath;
	public string originalText;
	public string fontName;
	public float fontSize;
	public Color color;
	public bool bold;
	public bool italic;
	public TextAnchor alignment;
	public bool success;
	public string error;
}

public enum SearchScope
{
	ActiveScene,
	AllOpenScenes,
	Prefabs
}

}