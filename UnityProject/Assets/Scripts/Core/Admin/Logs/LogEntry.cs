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
			if (StoredIn != null)
			{
				WasControlledByPlayer = StoredIn.GetComponentCustom<PlayerScript>()?.PlayerInfo;
			}
			else
			{
				WasControlledByPlayer = go.Player();
			}

			if (go == StoredIn)
			{
				StoredIn = null;
			}



			LogInfo.CoreObject = go;
			LogInfo.CoreObjectName = go?.name;
			LogInfo.WasControlledByPlayer = WasControlledByPlayer;
			LogInfo.WasStoredInObject = StoredIn;
			LogInfo.WasStoredInObjectName = StoredIn?.name;
			LogInfo.Info = "";

			if (go == null)
			{
				LogInfo.WasAtPositionWorld = Vector3.zero;
			}
			else
			{
				LogInfo.WasAtPositionWorld = go.AssumedWorldPosServer();
			}



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
				CoreObjectName = CoreObjectName,
				WasStoredInObject = WasStoredInObject.NetIdCommonComponents(),
				WasStoredInObjectName = WasStoredInObjectName,
				WasControlledByPlayerAccountId = WasControlledByPlayer?.AccountId,
				WasAtPositionWorld = WasAtPositionWorld.ToSerialiseString(),
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
			if (entry.Log != null)
			{
				Log = entry.Log.Select(x => x.SerialiseVersion()).ToArray();
			}
			else
			{
				Log = Array.Empty<LogItems>();
			}

			LogImportance = entry.LogImportance.ToString();
			Category = entry.Category.ToString();
		}

		public struct LogItems
		{
			public string CoreObjectName;
			public uint CoreObject;
			public string WasStoredInObjectName;
			public uint WasStoredInObject;
			public string WasControlledByPlayerAccountId;
			public string WasAtPositionWorld;
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
