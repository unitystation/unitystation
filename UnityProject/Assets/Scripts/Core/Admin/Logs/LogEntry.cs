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



			return GetRootGameObject(go, IsInGameItem).transform.position;
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

	public class HumanLogEntry
	{
		public DateTime LogTime;
		public string Log;
		public string LogImportance;
		public string Perpetrator;
		public string Category;

		public HumanLogEntry(LogEntry entry)
		{
			LogTime = entry.LogTime;
			Log = entry.Log;
			LogImportance = entry.LogImportance.ToString();
			Perpetrator = entry.Perpetrator?.ToString();
			Category = entry.Category.ToString();
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
