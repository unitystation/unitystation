using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;

/// Logger with categories support.
/// If you want to debug some category, make a new LogOverride entry and set Level to trace, like this:
/// [Category.Transform] = Level.Trace
public static class Logger
{
	private static LoggerPreferences loggerPrefs;
	/// Default Log level
	public static readonly LogLevel LogLevel = LogLevel.Info;
	private static Dictionary<Category, LogLevel> LogOverrides = new Dictionary<Category, LogLevel>();

	public static void RefreshPreferences()
	{
		var path = Path.Combine(Application.streamingAssetsPath,
			"LogLevelDefaults/");

		if (!File.Exists(Path.Combine(path, "custom.json")))
		{
			var data = File.ReadAllText(Path.Combine(path, "default.json"));
			File.WriteAllText(Path.Combine(path, "custom.json"), data);
		}

		loggerPrefs = JsonUtility.FromJson<LoggerPreferences>(File.ReadAllText(Path.Combine(path, "custom.json")));

		LogOverrides.Clear();

		foreach (LogOverridePref pref in loggerPrefs.logOverrides)
		{
			LogOverrides.Add(pref.category, pref.logLevel);
		}
	}

	public static void SetLogLevel(Category _category, LogLevel level)
	{
		Log($"Log category {_category.ToString()} is now set to {level.ToString()}", Category.DebugConsole);

		var index = loggerPrefs.logOverrides.FindIndex(x => x.category == _category);
		if (index != -1)
		{
			loggerPrefs.logOverrides[index].logLevel = level;
		}
		else
		{
			loggerPrefs.logOverrides.Add(new LogOverridePref() { category = _category, logLevel = level });
		}

		SaveLogOverrides();
		RefreshPreferences();
		EventManager.Broadcast(EVENT.LogLevelAdjusted);
	}

	public static void SaveLogOverrides()
	{
		var path = Path.Combine(Application.streamingAssetsPath,
			"LogLevelDefaults/");
		File.WriteAllText(Path.Combine(path, "custom.json"), JsonUtility.ToJson(loggerPrefs));
	}

	/// <inheritdoc cref="LogTrace"/>
	/// <inheritdoc cref="LogFormat"/>
	[StringFormatMethod("msg")]
	public static void LogTraceFormat(string msg, Category category = Category.Unknown, params object[] args)
	{
		TryLog(msg, LogLevel.Trace, category, args);
	}

	/// LogFormats won't format string if it's not getting printed, therefore perform better.
	/// This is most useful for Trace level that is rarely enabled.
	[StringFormatMethod("msg")]
	public static void LogFormat(string msg, Category category = Category.Unknown, params object[] args)
	{
		TryLog(msg, LogLevel.Info, category, args);
	}

	/// <inheritdoc cref="LogWarning"/>
	/// <inheritdoc cref="LogFormat"/>
	[StringFormatMethod("msg")]
	public static void LogWarningFormat(string msg, Category category = Category.Unknown, params object[] args)
	{
		TryLog(msg, LogLevel.Warning, category, args);
	}

	/// <inheritdoc cref="LogWarning"/>
	/// <inheritdoc cref="LogFormat"/>
	[StringFormatMethod("msg")]
	public static void LogErrorFormat(string msg, Category category = Category.Unknown, params object[] args)
	{
		TryLog(msg, LogLevel.Error, category, args);
	}

	/// Try printing Trace level entry. Most verbose logs that should only be enabled when debugging something that is broken.
	public static void LogTrace(string msg, Category category = Category.Unknown)
	{
		TryLog(msg, LogLevel.Trace, category);
	}

	/// Try printing Info level entry.
	public static void Log(string msg, Category category = Category.Unknown)
	{
		TryLog(msg, LogLevel.Info, category);
	}

	/// Try printing Warning level entry.
	public static void LogWarning(string msg, Category category = Category.Unknown)
	{
		TryLog(msg, LogLevel.Warning, category);
	}

	/// Try printing Error level entry.
	public static void LogError(string msg, Category category = Category.Unknown)
	{
		TryLog(msg, LogLevel.Error, category);
	}

	private static void TryLog(string message, LogLevel messageLevel, Category category = Category.Unknown, params object[] args)
	{
		if (category == Category.Unknown)
		{
			Debug.Log(message);
			return;
		}

		LogLevel referenceLevel = LogLevel;
		if (LogOverrides.ContainsKey(category))
		{
			referenceLevel = LogOverrides[category];
		}

		if (referenceLevel < messageLevel)
		{
			return;
		}

		string categoryPrefix = category == Category.Unknown ? "" : "[" + category + "] ";

		string msg = categoryPrefix + message;
		if (args.Length > 0)
		{
			switch (messageLevel)
			{
				case LogLevel.Off:
					break;
				case LogLevel.Error:
					Debug.LogErrorFormat(msg, args);
					break;
				case LogLevel.Warning:
					Debug.LogWarningFormat(msg, args);
					break;
				case LogLevel.Info:
					Debug.LogFormat(msg, args);
					break;
				case LogLevel.Trace:
					Debug.LogFormat(msg, args);
					break;
			}
		}
		else
		{
			switch (messageLevel)
			{
				case LogLevel.Off:
					break;
				case LogLevel.Error:
					Debug.LogError(msg);
					break;
				case LogLevel.Warning:
					Debug.LogWarning(msg);
					break;
				case LogLevel.Info:
					Debug.Log(msg);
					break;
				case LogLevel.Trace:
					Debug.Log(msg);
					break;
			}
		}
	}
}

public enum LogLevel
{
	Off = -1,
	Error = 0,
	Warning = 1,
	Info = 2,
	Trace = 3
}

public enum Category
{
	Unknown,
	Security,
	Connections,
	Threading,
	Matrix,
	Transform,
	Movement,
	NetMessage,
	UI,
	ItemSpawn,
	Inventory,
	Equipment,
	Steam,
	DmMetadata,
	Light2D,
	NetUI,
	Health,
	Atmos,
	Telecoms,
	Shutters,
	Doors,
	Jobs,
	PushPull,
	Lighting,
	Firearms,
	Power,
	Throwing,
	Containers,
	Chemistry,
	SunVox,
	Rcon,
	Audio,
	Research,
	TileMaps,
	Construction,
	DatabaseAPI,
	PlayerSprites,
	Electrical,
	RightClick,
	Lerp,
	Keybindings,
	Round,
	DebugConsole,
	Camera,
	Exploits, //Used when (potentially illegal/invalid) actions occur that are likely caused due to exploits, cheats or hacking
	Graphics,
	Server,
	Tests,
	Editor,
	VariableViewer,
	Themes,
	SpriteHandler,
	GameMode,
	Chat,
	Interaction,
	Antags,
	Hub,
	SpatialRelationship,
	MLAgents,
	Direction,
	Admin,
	BuckledMovement,
	ProgressAction,
	Botany,
	SoundFX,
	Character,
	SubScenes,
	Spells,
	UIAction,
	Cooldowns
}

[Serializable]
public class LoggerPreferences
{
	public List<LogOverridePref> logOverrides = new List<LogOverridePref>();
}

[Serializable]
public class LogOverridePref
{
	public Category category;
	public LogLevel logLevel = LogLevel.Info;

}