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

    private void CheckCategories()
    {
        LoggerPreferences savedPrefs = null;
        if (PlayerPrefs.HasKey("logprefs"))
        {
            savedPrefs = JsonUtility.FromJson<LoggerPreferences>(PlayerPrefs.GetString("logprefs"));
        }

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
    }

    private void SavePrefs()
    {
        PlayerPrefs.SetString("logprefs", JsonUtility.ToJson(loggerPrefs));
        PlayerPrefs.Save();
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