using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Logs;
using UnityEditor;
using UnityEngine;

public class LogLevels : EditorWindow
{
	private LoggerPreferences loggerPrefs;
	private Vector2 scrollPosition;
	private Dictionary<Category, bool> selected = new Dictionary<Category, bool>();
	private LogLevel selectionLogLevel;

	[MenuItem("Logger/Adjust Log Levels")]
	public static void OpenWindow()
	{
		GetWindow<LogLevels>();
	}

	public void Awake()
	{
		CheckCategories();
	}

	void OnEnable()
	{
		Loggy.levelChange += CheckCategories;
	}

	void OnDisable()
	{
		Loggy.levelChange -= CheckCategories;
	}

	private void CheckCategories()
	{
		var path = Path.Combine(Application.streamingAssetsPath,
			"LogLevelDefaults/");

		if (!File.Exists(Path.Combine(path, "custom.json")))
		{
			var data = File.ReadAllText(Path.Combine(path, "default.json"));
			File.WriteAllText(Path.Combine(path, "custom.json"), data);
		}

		LoggerPreferences savedPrefs = JsonUtility.FromJson<LoggerPreferences>(File.ReadAllText(Path.Combine(path, "custom.json")));

		LoggerPreferences generatedPrefs = new LoggerPreferences();
		foreach (Category cat in Enum.GetValues(typeof(Category)))
		{
			generatedPrefs.logOverrides.Add(new LogOverridePref() { category = cat });
		}

		if (savedPrefs != null)
		{
			foreach (LogOverridePref pref in generatedPrefs.logOverrides)
			{
				var index = savedPrefs.logOverrides.FindIndex(x => x.category == pref.category);
				if (index != -1)
				{
					pref.logLevel = savedPrefs.logOverrides[index].logLevel;
				}
			}
		}

		loggerPrefs = generatedPrefs;
		Repaint();
	}

	private void SavePrefs()
	{
		var path = Path.Combine(Application.streamingAssetsPath,
			"LogLevelDefaults/");
		File.WriteAllText(Path.Combine(path, "custom.json"), JsonUtility.ToJson(loggerPrefs));

		if (Application.isPlaying)
		{
			Loggy.RefreshPreferences();
		}
	}

	void OnGUI()
	{
		DrawMenu();
		DrawSelectors();
	}

	private void DrawMenu()
	{
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Select All"))
		{
			foreach (var key in selected.Keys.ToArray())
			{
				selected[key] = true;
			}
		}
		if (GUILayout.Button("Deselect All"))
		{
			foreach (var key in selected.Keys.ToArray())
			{
				selected[key] = false;
			}
		}
		if (GUILayout.Button("Invert selection"))
		{
			foreach (var key in selected.Keys.ToArray())
			{
				selected[key] ^= true;
			}
		}
		EditorGUILayout.EndHorizontal();
		DrawSetSelected();
	}

	private void DrawSetSelected()
	{
		EditorGUILayout.BeginHorizontal();
		selectionLogLevel = (LogLevel)EditorGUILayout.EnumPopup("Log Level:", selectionLogLevel);
		if (GUILayout.Button("Set selected"))
		{
			bool dirty = false;
			foreach (var selection in selected.Where(s => s.Value))
			{
				var pref = loggerPrefs.logOverrides.Single(l => l.category == selection.Key);
				if (pref.logLevel != selectionLogLevel)
				{
					pref.logLevel = selectionLogLevel;
					dirty = true;
				}
			}

			if (dirty)
			{
				SavePrefs();
			}
		}
		EditorGUILayout.EndHorizontal();
	}

	void DrawSelectors()
	{
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		GUILayout.Space(10f);
		foreach (LogOverridePref pref in loggerPrefs.logOverrides.OrderBy(X => X.category.ToString()))
		{
			EditorGUILayout.BeginHorizontal();
			LogLevel logLevel = pref.logLevel;
			LogLevel checkLevel = logLevel;
			checkLevel = (LogLevel)EditorGUILayout.EnumPopup(pref.category.ToString(), checkLevel);
			selected[pref.category] = EditorGUILayout.Toggle(GetValueOrDefault(selected, pref.category) ?? false);
			GUILayout.Space(2f);
			if (checkLevel != logLevel)
			{
				pref.logLevel = checkLevel;
				SavePrefs();
			}
			EditorGUILayout.EndHorizontal();
		}
		GUILayout.EndScrollView();
	}

	static U? GetValueOrDefault<T, U>(Dictionary<T, U> dic, T key) where U : struct
	{
		if (dic.TryGetValue(key, out var value))
		{
			return value;
		}

		return null;
	}
}