using System;
using System.Collections.Generic;
using HealthV2;
using Shared.Managers;
using UnityEngine;

namespace Core.Admin.Logs
{
	public class AdminLogsManager : SingletonManager<AdminLogsManager>
	{
		private HashSet<LogEntry> recordedEntries = new HashSet<LogEntry>();
		public static Action<LogEntry> OnNewLog;

		public override void Awake()
		{
			base.Awake();
			OnNewLog += RecordEntry;
		}

		private void RecordEntry(LogEntry entry)
		{
			recordedEntries.Add(entry);
		}

		public static void AddNewLog(LogEntry entry)
		{
			OnNewLog?.Invoke(entry);
		}

		public static void TrackKill(GameObject perp, LivingHealthMasterBase victim)
		{
			var log = perp == null ? $"{victim} died." : $"{perp} has caused the death of {victim}.";
			LogEntry entry = new LogEntry
			{
				AdminActions = new List<AdminActionToTake>(),
				Log = log,
				LogImportance = Severity.DEATH,
			};
			OnNewLog?.Invoke(entry);
		}
	}
}