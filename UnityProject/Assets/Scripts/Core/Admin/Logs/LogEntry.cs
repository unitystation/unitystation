using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Admin.Logs
{
	public class LogEntry
	{
		public DateTime LogTime { get; } = DateTime.UtcNow;
		public string Log;
		public List<AdminActionToTake> AdminActions = new List<AdminActionToTake>();
		public Severity LogImportance;
		public GameObject Perpetrator;
	}

	public class HumanLogEntry
	{
		public DateTime LogTime;
		public string Log;
		public string LogImportance;
		public string Perpetrator;

		public HumanLogEntry(LogEntry entry)
		{
			LogTime = entry.LogTime;
			Log = entry.Log;
			LogImportance = entry.LogImportance.ToString();
			Perpetrator = entry.Perpetrator?.ToString();
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
}
