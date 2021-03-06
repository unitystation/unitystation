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
	public static Action levelChange;


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
		levelChange?.Invoke();
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
			SendLog(message, messageLevel, args);
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
		SendLog(message, messageLevel, args);
	}

	private static void SendLog(string msg, LogLevel messageLevel, params object[] args)
	{
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

/// <summary>
/// Categories for sorting and filtering logs
/// </summary>
public enum Category
{
	/// <summary>
	/// Category for the log isn't known or doesn't exist (Needs TLC)
	/// </summary>
	Unknown,

	//Tiles and Location
		/// <summary>
		/// Logs relating to Matrices (Needs TLC)
		/// </summary>
		Matrix,
		/// <summary>
		/// Logs relating to the generating and altering tilemaps (Needs TLC)
		/// </summary>
		TileMaps,
		/// <summary>
		/// Logs relating to the spatial relationships of RegisterTiles (Needs TLC)
		/// </summary>		
		SpatialRelationship,

	//Player and Mob Features
		/// <summary>
		/// Logs relating to player and mob movement (Needs TLC)
		/// </summary>
		Movement,
		/// <summary>
		/// Logs relating to Player and Mob injury and health (Needs TLC)
		/// </summary>
		Health,
	
	//Interface and Controls
		/// <summary>
		/// Logs relating to displaying the user interface (Needs TLC)
		/// </summary>
		UI,
		/// <summary>
		/// Logs relating to the keybinding settings (Needs TLC)
		/// </summary>
		Keybindings,
		/// <summary>
		/// Logs relating to UI Themes
		/// </summary>
		Themes,
		/// <summary>
		/// Logs related to the progress bar
		/// </summary>
		ProgressAction,
		/// <summary>
		/// Logs related to in-game chat (Needs TLC)
		/// </summary>
		Chat,

	//Items and Inventory
		/// <summary>
		/// Logs relating to spawning items into the game (Needs TLC)
		/// </summary>
		ItemSpawn,
		/// <summary>
		/// Logs relating to player inventory (Needs TLC)
		/// </summary>
		Inventory,
		/// <summary>
		/// Logs relating to projectile weapons (Needs TLC)
		/// </summary>
		Firearms,

	//Roles and Jobs
		/// <summary>
		/// Logs relating to job selection and assignment (Needs TLC)
		/// </summary>
		Jobs,
		/// <summary>
		/// Logs relating to antagonist roles in general (Needs TLC)
		/// </summary>
		Antags,

	//Interaction
		/// <summary>
		/// Logs relating to the Push/Pull interaction and transforming objects from pushing/pulling (Needs TLC)
		/// </summary>
		PushPull,
		/// <summary>
		/// Logs relating to players interacting with the in game environment(Needs TLC)
		/// </summary>
		Interaction,
		/// <summary>
		/// Logs relating to construction and crafting in game
		/// </summary>
		Construction,

	//Role Related Systems
		/// <summary>
		/// Logs relating to the Botany system
		/// </summary>
		Botany,
		/// <summary>
		/// Logs relating to the chemistry system (Needs TLC)
		/// </summary>
		Chemistry,
		/// <summary>
		/// Logs relating to the research system (Needs TLC)
		/// </summary>
		Research,

	//In-Game Systems
		/// <summary>
		/// Logs relating to the lighting system (Needs TLC)
		/// </summary>
		Lighting,
		/// <summary>
		/// Logs relating to the electricity system (Needs TLC)
		/// </summary>
		Electrical,
		/// <summary>
		/// Logs relating to the radiation system
		/// </summary>
		Radiation,
		/// <summary>
		/// Logs relating to Doors
		/// </summary>
		Doors,

	//Game Rounds
		/// <summary>
		/// Logs relating to setting up, progressing, and ending game rounds (Needs TLC)
		/// </summary>
		Round,
		/// <summary>
		/// Logs relating to the round's game mode (Needs TLC)
		/// </summary>
		GameMode,
		/// <summary>
		/// Logs relating to random events that take place during a round
		/// </summary>
		Event,

	//Sprites
		/// <summary>
		/// Logs relating to the sprite handler itself
		/// </summary>
		SpriteHandler,

	//General Debugging and Editor logs
		/// <summary>
		/// Logs relating to the Debug Console itself (Needs TLC)
		/// </summary>
		DebugConsole,
		/// <summary>
		/// Logs relating to debugging and tests
		/// </summary>
		Tests,
		/// <summary>
		/// Logs for use in the editor
		/// </summary>
		Editor,
	
	//Security
		/// <summary>
		/// Logs relating the client attempting illegal/invalid actions that could be caused by cheating, hacking, or exploits (Needs TLC)
		/// </summary>
		Exploits,
	

	//Metaroles
		/// <summary>
		/// Logs related to the mentor system
		/// </summary>
		Mentor,
		/// <summary>
		/// Catch all category for anything admin related (Needs TLC)
		/// </summary>
		Admin,

	//Core Functionality
		/// <summary>
		/// Logs relating to the programs threading behavior
		/// </summary>
		Threading,
		/// <summary>
		/// Logs relating to Steam integration
		/// </summary>
		Steam,
	
	//Sound and Audio*
		Addressables,
		Audio,
		SoundFX,
		/// <summary>
		/// Logs relating to the SunVox music studio integration
		/// </summary>
		SunVox,

	//Server Logs
		/// <summary>
		/// Logs relating to the Server's Net UI (Needs TLC)
		/// </summary>
		NetUI,
		/// <summary>
		/// Logs relating to client-server connections (Needs TLC)
		/// </summary>
		Connections,
		/// <summary>
		/// Logs relating to communication between clients and server (Needs TLC)
		/// </summary>
		NetMessage,
		DatabaseAPI,
		Server,
		Hub,
		/// <summary>
		/// Logs relating to the Remote Console
		/// </summary>
		Rcon,

	//Unused
	DmMetadata,
	Power,
	Throwing,
	RightClick,
	Shutters,


	//A very special case
	VariableViewer,

	//Depricate
	Equipment,
	Transform,
	Atmos,
	Telecoms,
	Containers,
	PlayerSprites,
	Lerp,
	Camera,
	Graphics,
	MLAgents,
	Direction,
	BuckledMovement,
	UIAction,
	Character,
	SubScenes,
	Spells,
	Cooldowns,
	Security
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
