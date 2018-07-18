using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

///Logger with categories support.
///Example of how to call Logger.Log("Message",Category.#Something#);
public static class Logger
{
	/// Default Log level
	private static readonly Level LogLevel = Level.Warning;

	/// Log level overrides for categories
	private static readonly Dictionary<Category, Level> LogOverrides = new Dictionary<Category, Level>{
		[Category.Movement] = Level.Error,
		[Category.UniCloth] = Level.Off,
		[Category.Unknown] = Level.Log
	};

	private enum Level{
		Off = -1,
		Error = 0,
		Warning = 1,
		Log = 2,
		Trace = 3
	}

	public static void LogTrace( string msg, Category category = Category.Unknown, [CallerMemberName] string memberName = "" )
	{
		TryLog( msg, Level.Trace, category, memberName );
	}

	public static void Log( string msg, Category category = Category.Unknown, [CallerMemberName] string memberName = "" )
	{
		TryLog( msg, Level.Log, category, memberName );
	}

	public static void LogWarning( string msg, Category category = Category.Unknown, [CallerMemberName] string memberName = "" )
	{
		TryLog( msg, Level.Warning, category, memberName );
	}

	public static void LogError( string msg, Category category = Category.Unknown, [CallerMemberName] string memberName = "" )
	{
		TryLog( msg, Level.Error, category, memberName );
	}

	private static void TryLog( string message, Level messageLevel, Category category = Category.Unknown, string memberName = "" )
	{
		Level referenceLevel = LogLevel;
		if ( LogOverrides.ContainsKey( category ) )
		{
			referenceLevel = LogOverrides[category];
		}

		if ( referenceLevel < messageLevel )
		{
			return;
		}

		Log( '[' + category + "]: " + message + '\n' + memberName, messageLevel );
	}

	private static void Log( string msg, Level level )
	{
		switch ( level )
		{
			case Level.Off:
				break;
			case Level.Error:
				Debug.LogError( msg );
				break;
			case Level.Warning:
				Debug.LogWarning( msg );
				break;
			case Level.Log:
				Debug.Log( msg );
				break;
			case Level.Trace:
				Debug.Log( msg );
				break;
		}
	}
}

public enum Category {
	Unknown,
	Atmospherics,
	Movement,
	MapLoad,
	RoomControl,
	ItemList,
	ItemEntry,
	DmiIconData,
	UI,
	MatrixManager,
	Equipment,
	NetworkManager,
	UniCloth,
	NetworkTabManager,
	ItemFactory,
	Light2D,
	PlayerList,
	PoolManager,
	SpriteManager,
	PlayerNetworkActions,
	Steam,
	TabUISystem,
	Telecoms,
	PlayerJobManager,
	PushPull,
	NetSpriteImage,
	Shutters,
	ShipNavigation,
	Lighting,
	Electrical
}