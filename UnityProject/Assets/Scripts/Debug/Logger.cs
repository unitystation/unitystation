using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

/// Logger with categories support.
/// If you want to debug some category, make a new LogOverride entry and set Level to trace, like this:
/// [Category.Transform] = Level.Trace
public static class Logger
{
	/// Default Log level
	private static readonly Level LogLevel = Level.Info;

	/// Log level overrides for categories. Default log level will be ignored for these:
	private static readonly Dictionary<Category, Level> LogOverrides = new Dictionary<Category, Level>{
		[Category.Unknown]  = Level.Info,
		[Category.Movement] = Level.Warning,
		[Category.DmMetadata] = Level.Off,
		[Category.Light2D] = Level.Off,
		[Category.RightClick] = Level.Off,
		[Category.PushPull] = Level.Info,
		[Category.PlayerSprites] = Level.Error,
		[Category.Lerp] = Level.Off,
//		[Category.NetUI] = Level.Trace,
	};

	private enum Level{
		Off = -1,
		Error = 0,
		Warning = 1,
		Info = 2,
		Trace = 3
	}

	/// <inheritdoc cref="LogTrace"/>
	/// <inheritdoc cref="LogFormat"/>
	[StringFormatMethod("msg")]
	public static void LogTraceFormat( string msg, Category category = Category.Unknown, params object[] args ) {
		TryLog( msg, Level.Trace, category, args );
	}

	/// LogFormats won't format string if it's not getting printed, therefore perform better.
	/// This is most useful for Trace level that is rarely enabled.
	[StringFormatMethod("msg")]
	public static void LogFormat( string msg, Category category = Category.Unknown, params object[] args ) {
		TryLog( msg, Level.Info, category, args );
	}

	/// <inheritdoc cref="LogWarning"/>
	/// <inheritdoc cref="LogFormat"/>
	[StringFormatMethod("msg")]
	public static void LogWarningFormat( string msg, Category category = Category.Unknown, params object[] args ) {
		TryLog( msg, Level.Warning, category, args );
	}

	/// <inheritdoc cref="LogWarning"/>
	/// <inheritdoc cref="LogFormat"/>
	[StringFormatMethod("msg")]
	public static void LogErrorFormat( string msg, Category category = Category.Unknown, params object[] args ) {
		TryLog( msg, Level.Error, category, args );
	}

	/// Try printing Trace level entry. Most verbose logs that should only be enabled when debugging something that is broken.
	public static void LogTrace( string msg, Category category = Category.Unknown ){
		TryLog( msg, Level.Trace, category );
	}

	/// Try printing Info level entry.
	public static void Log( string msg, Category category = Category.Unknown ){
		TryLog( msg, Level.Info, category );
	}

	/// Try printing Warning level entry.
	public static void LogWarning( string msg, Category category = Category.Unknown ){
		TryLog( msg, Level.Warning, category );
	}

	/// Try printing Error level entry.
	public static void LogError( string msg, Category category = Category.Unknown ){
		TryLog( msg, Level.Error, category );
	}

	private static void TryLog( string message, Level messageLevel, Category category = Category.Unknown, params object[] args ){
		Level referenceLevel = LogLevel;
		if ( LogOverrides.ContainsKey( category ) ){
			referenceLevel = LogOverrides[category];
		}

		if ( referenceLevel < messageLevel ){
			return;
		}

		string categoryPrefix = category == Category.Unknown ? "" : "[" + category + "] ";

		string msg = categoryPrefix + message;
		if ( args.Length > 0 ) {
			switch ( messageLevel ) {
				case Level.Off:
					break;
				case Level.Error:
					Debug.LogErrorFormat( msg, args );
					break;
				case Level.Warning:
					Debug.LogWarningFormat( msg, args );
					break;
				case Level.Info:
					Debug.LogFormat( msg, args );
					break;
				case Level.Trace:
					Debug.LogFormat( msg, args );
					break;
			}
		} else {
			switch ( messageLevel ) {
				case Level.Off:
					break;
				case Level.Error:
					Debug.LogError( msg );
					break;
				case Level.Warning:
					Debug.LogWarning( msg );
					break;
				case Level.Info:
					Debug.Log( msg );
					break;
				case Level.Trace:
					Debug.Log( msg );
					break;
			}
		}
	}
}

public enum Category {
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
	Keybindings
}