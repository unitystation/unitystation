using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace Logs
{
	/// <summary>
	/// Loggy with categories support.
	/// If you want to debug some category, make a new LogOverride entry and set Level to trace, like this:
	/// [Category.Transform] = Level.Trace
	/// </summary>
	public static class Loggy
	{
		public static Action levelChange;

		private static LoggerPreferences loggerPrefs;

		/// Default Log level
		public static readonly LogLevel LogLevel = LogLevel.Info;

		private static readonly Dictionary<Category, LogLevel> LogOverrides = new Dictionary<Category, LogLevel>();

		public static Thread MainGameThread;

		public static void RefreshPreferences()
		{
			var path = Path.Combine(Application.streamingAssetsPath, "LogLevelDefaults/");

			if (!File.Exists(Path.Combine(path, "custom.json")))
			{
				var data = File.ReadAllText(Path.Combine(path, "default.json"));
				File.WriteAllText(Path.Combine(path, "custom.json"), data);
			}

			loggerPrefs = JsonUtility.FromJson<LoggerPreferences>(File.ReadAllText(Path.Combine(path, "custom.json")));

			LogOverrides.Clear();

			foreach (LogOverridePref pref in loggerPrefs.logOverrides)
			{
				LogOverrides[pref.category] = pref.logLevel;
			}
		}

		public static void SetLogLevel(Category category, LogLevel level)
		{
			Info($"Log category {category} is now set to {level}", Category.DebugConsole);
			var index = loggerPrefs.logOverrides.FindIndex(x => x.category == category);
			if (index != -1)
			{
				loggerPrefs.logOverrides[index].logLevel = level;
			}
			else
			{
				loggerPrefs.logOverrides.Add(new LogOverridePref() { category = category, logLevel = level });
			}

			SaveLogOverrides();
			RefreshPreferences();
			levelChange?.Invoke();
		}

		public static void SaveLogOverrides()
		{
			var path = Path.Combine(Application.streamingAssetsPath, "LogLevelDefaults/");
			File.WriteAllText(Path.Combine(path, "custom.json"), JsonUtility.ToJson(loggerPrefs));
		}

		private static void LogMessage(LogLevel level, string msg, string methodName, string filePath, int lineNumber, Category category = Category.Unknown)
		{
			if (level == LogLevel.Off)
			{
				return;
			}

			LogLevel referenceLevel = LogLevel;

			if (category != Category.Unknown && LogOverrides.TryGetValue(category, out LogLevel overrideLevel))
			{
				referenceLevel = overrideLevel;
			}

			if (referenceLevel < level)
			{
				return;
			}

			//2018-10-25 11:56:35,008 INFO: [Atmos] message [MethodName::h:\UnityProject\Scripts\MyScript.cs::13]
			DateTime now = DateTime.Now;
			string formattedDate = now.ToString("yyyy-MM-dd HH:mm:ss,fff");
			const string messageFormat = "{0} {1}: [{2}] {3} [{4}::{5}]::{6}]";
			msg = string.Format(messageFormat,
				formattedDate,
				level.ToString().ToUpper(),
				category,
				msg,
				methodName, filePath, lineNumber);

			if (Thread.CurrentThread != MainGameThread && MainGameThread != null)
			{
				ThreadLoggy.QueueLog(level, msg);
			}

			switch (level)
			{
				case LogLevel.Error:
					//error level includes stacktrace like always
					Debug.LogError(msg);
					break;
				case LogLevel.Warning:
					Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, "{0}", msg);
					break;
				default:
					Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", msg);
					break;
			}
		}

		public static FluentFormatter Trace(string msg = null, Category category = Category.Unknown, [CallerMemberName] string methodName = null, [CallerFilePath] string fileName = null, [CallerLineNumber] int lineNo = -1)
		{
			FluentFormatter formatter = new(LogLevel.Info, methodName, fileName, lineNo);
			if (string.IsNullOrEmpty(msg) == false)
			{
				LogMessage(LogLevel.Trace, msg, methodName, fileName, lineNo, category);
			}

			return formatter;
		}

		public static FluentFormatter Info(string msg = null, Category category = Category.Unknown, [CallerMemberName] string methodName = null, [CallerFilePath] string fileName = null, [CallerLineNumber] int lineNo = -1)
		{
			FluentFormatter formatter = new(LogLevel.Info, methodName, fileName, lineNo);
			if (string.IsNullOrEmpty(msg) == false)
			{
				LogMessage(LogLevel.Info, msg, methodName, fileName, lineNo, category);
			}

			return formatter;
		}

		public static FluentFormatter Warning(string msg = null, Category category = Category.Unknown, [CallerMemberName] string methodName = null, [CallerFilePath] string fileName = null, [CallerLineNumber] int lineNo = -1)
		{
			FluentFormatter formatter = new(LogLevel.Info, methodName, fileName, lineNo);
			if (string.IsNullOrEmpty(msg) == false)
			{
				LogMessage(LogLevel.Warning, msg, methodName, fileName, lineNo, category);
			}

			return formatter;
		}

		public static FluentFormatter Error(string msg = null, Category category = Category.Unknown, [CallerMemberName] string methodName = null, [CallerFilePath] string fileName = null, [CallerLineNumber] int lineNo = -1)
		{
			FluentFormatter formatter = new(LogLevel.Info, methodName, fileName, lineNo);
			if (string.IsNullOrEmpty(msg) == false)
			{
				LogMessage(LogLevel.Error, msg, methodName, fileName, lineNo, category);
			}

			return formatter;
		}

		public class FluentFormatter
		{
			private readonly LogLevel _level;
			private readonly string _methodName;
			private readonly string _fileName;
			private readonly int _lineNo;

			public FluentFormatter(LogLevel level, string methodName, string fileName, int lineNumber)
			{
				_level = level;
				_methodName = methodName;
				_fileName = fileName;
				_lineNo = lineNumber;
			}

			public void Format(string msg, Category category = Category.Unknown, params object[] args)
			{
				msg = string.Format(msg, args);
				LogMessage(_level, msg, _methodName, _fileName, _lineNo, category);
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

		// Core Functionality
		Threading,
		Addressables,
		DatabaseAPI,
		Steam,

		// Servers and Admin
		Server,
		Connections,
		Rcon,
		Admin,
		Exploits,

		// Sound and Audio
		Audio,
		SunVox,

		// Sprites and Particles
		Sprites,
		Particles,

		// Tiles and Location
		Matrix,
		TileMaps,
		SpatialRelationship,

		// In-Game Systems
		Damage,
		Lighting,
		Electrical,
		Radiation,
		Shuttles,

		// Interface and Controls
		UI,
		NetUI,
		UserInput,
		Keybindings,
		Themes,
		ProgressAction,
		Chat,

		// Player and Mob Features
		Character,
		EntitySpawn,
		Mobs,
		Health,
		Ghosts,

		// Interaction and Movement
		Interaction,
		Movement,
		PushPull,
		Construction,

		// Items and Inventory
		ItemSpawn,
		Inventory,
		PlayerInventory,
		Firearms,

		// Roles and Jobs
		Jobs,
		Antags,
		Spells,
		Blob,
		Changeling,

		// Role Related Systems
		Botany,
		Chemistry,
		Research,
		Cargo,
		Atmos,
		Mentor,

		// Object Specific Logs
		Objects,
		Machines,
		Doors,
		Pipes,
		Directionals,
		VariableViewer,

		// Game Rounds
		Round,
		GameMode,
		Event,

		// General Debugging and Editor logs
		DebugConsole,
		Tests,
		Editor,
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