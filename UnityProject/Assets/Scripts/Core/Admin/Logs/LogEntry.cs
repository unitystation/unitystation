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

		public StoredLogEntry.LogItems SerialiseVersion()
		{
			return new StoredLogEntry.LogItems()
			{
				Object = CoreObject.NetIdCommonComponents(),
				ObjectName = CoreObjectName,
				StoredIn = WasStoredInObject.NetIdCommonComponents(),
				StoredInName = WasStoredInObjectName,
				PlayerAccountID = WasControlledByPlayer?.AccountId,
				PositionWorld = WasAtPositionWorld.ToSerialiseString(),
				Info = Info
			};
		}

	}

	public struct StoredLogEntry
	{
		public DateTime LogTime;
		public LogItems[] Log;
		public string LogImportance;
		public string Category;

		public StoredLogEntry(LogEntry entry)
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
			public string ObjectName;
			public uint Object;
			public string StoredInName;
			public uint StoredIn;
			public string PlayerAccountID;
			public string PositionWorld;
			public string Info;

			//CoreObjectName -> ObjectName -> ObjName=x
		//		CoreObject -> Object -> Obj=x
	//			WasStoredInObjectName -> StoredInName -> StoredInName=x
//				WasStoredInObject -> StoredIn-> StoredIn=x/
			//	WasControlledByPlayerAccountId -> PlayerAccountID -> PlayerAccount=x
			//	WasAtPositionWorld ->  PositionWorld -> Position=x
			//	Info -> Info -> Info=x
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
