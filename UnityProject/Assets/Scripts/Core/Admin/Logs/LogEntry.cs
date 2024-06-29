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
