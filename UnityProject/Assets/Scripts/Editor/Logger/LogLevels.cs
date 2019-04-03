using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class LogLevels : EditorWindow
{
    private LoggerPreferences loggerPrefs;
    private Vector2 scrollPosition;

    [MenuItem("Logger/Adjust Log Levels")]
    public static void OpenWindow()
    {
        LogLevels window = GetWindow<LogLevels>();
        window.titleContent = new GUIContent("Adjust Log Levels");
        window.CheckCategories();
    }

    void OnEnable()
    {
        EventManager.AddHandler(EVENT.LogLevelAdjusted, CheckCategories);
    }

    void OnDisable()
    {
        EventManager.RemoveHandler(EVENT.LogLevelAdjusted, CheckCategories);
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
            Logger.RefreshPreferences();
        }
    }

    void OnGUI()
    {
        DrawSelectors();
    }

    void DrawSelectors()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        GUILayout.Space(10f);
        foreach (LogOverridePref pref in loggerPrefs.logOverrides)
        {
            LogLevel logLevel = pref.logLevel;
            LogLevel checkLevel = logLevel;
            checkLevel = (LogLevel)EditorGUILayout.EnumPopup(pref.category.ToString(), checkLevel);
            GUILayout.Space(2f);
            if (checkLevel != logLevel)
            {
                pref.logLevel = checkLevel;
                SavePrefs();
            }
        }
        GUILayout.EndScrollView();
    }
}