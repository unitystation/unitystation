using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Admin.Logs
{
	public class LogEntry
	{
		public DateTime LogTime { get; } = DateTime.UtcNow;
		public List<LogInfo> Log = new List<LogInfo>();
		public List<AdminActionToTake> AdminActions = null;
		public Severity LogImportance;
		public LogCategory Category;
	}

	public static class LogUtilities
	{

		public static LogInfo GetLogInfo(this GameObject go)
		{

			var LogInfo = new LogInfo();

			var StoredIn = go.GetRootGameObject();

			PlayerInfo WasControlledByPlayer = null;
			if (go == StoredIn)
			{
				StoredIn = null;
			}
			else
			{
				WasControlledByPlayer = StoredIn.GetComponentCustom<PlayerScript>()?.PlayerInfo;
			}

			LogInfo.CoreObject = go;
			LogInfo.WasControlledByPlayer = WasControlledByPlayer;
			LogInfo.WasStoredInObject = StoredIn;
			LogInfo.Info = go.ExpensiveName();
			LogInfo.WasAtPositionWorld = go.AssumedWorldPosServer();

			return LogInfo;
		}

		public static LogInfo GetLogInfo(this string Info)
		{
			var LogInfo = new LogInfo();
			LogInfo.Info = Info;
			return LogInfo;
		}

	}


	public struct LogInfo
	{
		public GameObject CoreObject;
		public GameObject WasStoredInObject;
		public PlayerInfo WasControlledByPlayer;
		public Vector3 WasAtPositionWorld;
		public string Info;
		public string SerialisedInfo;

	}

	public class LongTermLogEntry
	{
		public DateTime LogTime;
		public string Log;
		public string LogImportance;
		public string Perpetrator;
		public string Category;

		public LongTermLogEntry(LogEntry entry)
		{
			throw new NotImplementedException();
		}
	}

	public class AdminActionToTake
	{
		public string Name;
		public Color Color = Color.gray;
		public int ActionID;
	}

	public enum Severity
	{
		MISC,
		ANNOYING,
		SUSPICOUS,
		DEATH,
		IMMEDIATE_ATTENTION,
	}

	public enum LogCategory
	{
		MISC,
		Connections,
		Technical,
		MobDamage,
		ObjectDamage,
		Ghost,
		NPC,
		Interaction,
		Admin,
		World,
		RoundFlow,
	}
}
