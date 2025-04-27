using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Admin.Logs
{
	public class LogEntry
	{
		public DateTime LogTime { get; } = DateTime.UtcNow;
		public LogInfo[] Log;
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
			LogInfo.CoreObjectName = go.name;
			LogInfo.WasControlledByPlayer = WasControlledByPlayer;
			LogInfo.WasStoredInObject = StoredIn;
			LogInfo.WasStoredInObjectName = StoredIn?.name;
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

	public enum LogMarker
	{
		Core,
		StoredIn,
		ControlledBy,
		Position,
		Info
	}

	public struct LogInfo
	{
		public GameObject CoreObject;
		public string CoreObjectName;
		public GameObject WasStoredInObject;
		public string WasStoredInObjectName;
		public PlayerInfo WasControlledByPlayer;
		public Vector3 WasAtPositionWorld;
		public string Info;

		public LongTermLogEntry.LogItems SerialiseVersion()
		{
			return new LongTermLogEntry.LogItems()
			{
				CoreObject = CoreObject.NetIdCommonComponents(),
				WasStoredInObject = WasStoredInObject.NetIdCommonComponents(),
				WasControlledByPlayerAccountId = WasControlledByPlayer?.AccountId,
				Info = Info
			};
		}

	}

	public struct LongTermLogEntry
	{
		public DateTime LogTime;
		public LogItems[] Log;
		public string LogImportance;
		public string Category;

		public LongTermLogEntry(LogEntry entry)
		{
			LogTime = entry.LogTime;
			Log = entry.Log.Select(x => x.SerialiseVersion()).ToArray();
			LogImportance = entry.LogImportance.ToString();
			Category = entry.Category.ToString();


		}

		public struct LogItems
		{
			public uint CoreObject;
			public uint WasStoredInObject;
			public string WasControlledByPlayerAccountId;
			public Vector3 WasAtPositionWorld;
			public string Info;
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
