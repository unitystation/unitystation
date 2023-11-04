using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;

namespace Logs
{
	/// Loggy with categories support.
	/// If you want to debug some category, make a new LogOverride entry and set Level to trace, like this:
	/// [Category.Transform] = Level.Trace
	public static class Loggy
	{
		public static Action levelChange;


		private static LoggerPreferences loggerPrefs;

		/// Default Log level
		public static readonly LogLevel LogLevel = LogLevel.Info;

		private static Dictionary<Category, LogLevel> LogOverrides = new Dictionary<Category, LogLevel>();

		public static Thread MainGameThread;


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
				loggerPrefs.logOverrides.Add(new LogOverridePref() {category = _category, logLevel = level});
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

		private static void TryLog(string message, LogLevel messageLevel, Category category = Category.Unknown,
			params object[] args)
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
			if (Thread.CurrentThread != MainGameThread && MainGameThread != null)
			{
				ThreadLoggy.AddLog(msg);
			}


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
		/// Category for the log isn't known or doesn't exist
		/// </summary>
		Unknown,

		//Core Functionality
		/// <summary>
		/// Logs relating to the programs threading behavior
		/// </summary>
		Threading,

		/// <summary>
		/// Logs relating to the Addressables System
		/// </summary>
		Addressables,

		/// <summary>
		/// Logs relating to the DatabaseAPI, logging in to, creating, and verifying user accounts
		/// </summary>
		DatabaseAPI,

		/// <summary>
		/// Logs relating to Steam integration
		/// </summary>
		Steam,

		//Servers and Admin
		/// <summary>
		/// Logs relating to general server functionality
		/// </summary>
		Server,

		/// <summary>
		/// Logs relating to client-server connections
		/// </summary>
		Connections,

		/// <summary>
		/// Logs relating to the Remote Console
		/// </summary>
		Rcon,

		/// <summary>
		/// Logs relating to admins, admin commands and verification
		/// </summary>
		Admin,

		/// <summary>
		/// Logs relating the client attempting illegal/invalid actions that could be caused by cheating, hacking, or exploits
		/// </summary>
		Exploits,

		//Sound and Audio
		/// <summary>
		/// Logs relating to Sound Effects and Music
		/// </summary>
		Audio,

		/// <summary>
		/// Logs relating to the SunVox music studio integration
		/// </summary>
		SunVox,

		//Sprites and Particles
		/// <summary>
		/// Logs relating to Sprites and the SpriteHandler
		/// </summary>
		Sprites,

		/// <summary>
		/// Logs relating to Particles and the Particle System
		/// </summary>
		Particles,

		//Tiles and Location
		/// <summary>
		/// Logs relating to Matrices and Tile Metadata
		/// </summary>
		Matrix,

		/// <summary>
		/// Logs relating to the generating and altering tilemaps
		/// </summary>
		TileMaps,

		/// <summary>
		/// Logs relating to the spatial relationships of Register Tiles
		/// </summary>
		SpatialRelationship,

		//In-Game Systems
		/// <summary>
		/// Logs relating to the damage System
		/// </summary>
		Damage,

		/// <summary>
		/// Logs relating to the lighting system
		/// </summary>
		Lighting,

		/// <summary>
		/// Logs relating to the electricity system
		/// </summary>
		Electrical,

		/// <summary>
		/// Logs relating to the radiation system
		/// </summary>
		Radiation,

		/// <summary>
		/// Logs relating to the shuttle system
		/// </summary>
		Shuttles,

		//Interface and Controls
		/// <summary>
		/// Logs relating to displaying the general user interface
		/// </summary>
		UI,

		/// <summary>
		/// Logs relating to the NetUI (in-game tabs and windows)
		/// </summary>
		NetUI,

		/// <summary>
		/// Logs relating registering keystrokes and mouse clicks
		/// </summary>
		UserInput,

		/// <summary>
		/// Logs relating to the keybinding settings
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
		/// Logs related to in-game chat and headsets
		/// </summary>
		Chat,

		//Player and Mob Features
		/// <summary>
		/// Logs relating to Player Character settings and appearance
		/// </summary>
		Character,

		/// <summary>
		/// Logs relating to spawning players, mobs, and objects with inventories into the game
		/// </summary>
		EntitySpawn,

		/// <summary>
		/// Logs relating to the autonomous actions of non-player characters
		/// </summary>
		Mobs,

		/// <summary>
		/// Logs relating to player and mob conditions and health
		/// </summary>
		Health,

		/// <summary>
		/// Logs relating to Player Ghosts and AGhosts
		/// </summary>
		Ghosts,

		//Interaction and Movement
		/// <summary>
		/// Logs relating to players and mobs interacting with the in-game environment
		/// </summary>
		Interaction,

		/// <summary>
		/// Logs relating to player, mob and object movement
		/// </summary>
		Movement,

		/// <summary>
		/// Logs relating to the Push/Pull interaction and movement
		/// </summary>
		PushPull,

		/// <summary>
		/// Logs relating to construction and crafting in game
		/// </summary>
		Construction,

		//Items and Inventory
		/// <summary>
		/// Logs relating to spawning items into the game
		/// </summary>
		ItemSpawn,

		/// <summary>
		/// Logs relating to item storage and item slots
		/// </summary>
		Inventory,

		/// <summary>
		/// Logs relating specifically to player inventory
		/// </summary>
		PlayerInventory,

		/// <summary>
		/// Logs relating to projectile weapons
		/// </summary>
		Firearms,

		//Roles and Jobs
		/// <summary>
		/// Logs relating to job selection and assignment
		/// </summary>
		Jobs,

		/// <summary>
		/// Logs relating to general antagonist roles and objectives
		/// </summary>
		Antags,

		/// <summary>
		/// Logs relating to Wizard spells
		/// </summary>
		Spells,

		/// <summary>
		/// Logs relating to the Blob Antag role
		/// </summary>
		Blob,

		/// <summary>
		/// Logs relating to the Changeling Antag role
		/// </summary>
		Changeling,

		//Role Related Systems
		/// <summary>
		/// Logs relating to the Botany system
		/// </summary>
		Botany,

		/// <summary>
		/// Logs relating to the chemistry system
		/// </summary>
		Chemistry,

		/// <summary>
		/// Logs relating to the research system
		/// </summary>
		Research,

		/// <summary>
		/// Logs relating to the cargo system
		/// </summary>
		Cargo,

		/// <summary>
		/// Logs relating to the atmospheric system, gases, and gas containers
		/// </summary>
		Atmos,

		/// <summary>
		/// Logs related to the mentor system
		/// </summary>
		Mentor,

		//Object Specific Logs
		/// <summary>
		/// Logs relating to metadata for objects and the object pool
		/// </summary>
		Objects,

		/// <summary>
		/// Logs relating to machines and interactable structures
		/// </summary>
		Machines,

		/// <summary>
		/// Logs relating to Doors
		/// </summary>
		Doors,

		/// <summary>
		/// Logs relating to Pipes
		/// </summary>
		Pipes,

		/// <summary>
		/// Logs relating to directional objects such as Windoors
		/// </summary>
		Directionals,

		/// <summary>
		/// Logs relating to VariableViewer.cs, books, pages, bookshelves
		/// </summary>
		VariableViewer,

		//Game Rounds
		/// <summary>
		/// Logs relating to setting up, progressing, and ending game rounds
		Round,

		/// <summary>
		/// Logs relating to the round's game mode
		/// </summary>
		GameMode,

		/// <summary>
		/// Logs relating to random events that take place during a round
		/// </summary>
		Event,

		//General Debugging and Editor logs
		/// <summary>
		/// Logs relating to the Debug Console itself
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

		/// <summary>
		/// Logs that describe work of memory cleanup actions
		/// </summary>
		MemoryCleanup
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
}