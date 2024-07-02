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

		public static void AddNewLog(GameObject perp, string info, LogCatagory catagory, Severity severity = Severity.MISC)
		{
			LogEntry entry = new LogEntry
			{
				AdminActions = new List<AdminActionToTake>(),
				Log = info,
				LogImportance = severity,
				Perpetrator = perp,
			};
			AddNewLog(entry);
		}

		public static void TrackKill(GameObject perp, LivingHealthMasterBase victim)
		{
			var log = perp == null ?
				$"{victim.playerScript.playerName} (as {victim.playerScript.visibleName}) died."
				: $"{perp.ExpensiveName()} has caused the death of {victim.playerScript.playerName} (as {victim.playerScript.visibleName}).";
			LogEntry entry = new LogEntry
			{
				AdminActions = new List<AdminActionToTake>(),
				Log = log,
				LogImportance = Severity.DEATH,
				Perpetrator = perp,
				Catagory = LogCatagory.MobDamage
			};
			OnNewLog?.Invoke(entry);
		}

		public static void TrackDamage(GameObject perp, LivingHealthMasterBase victim, DamageInfo info)
		{
			if (info.Damage < 0.3) return;
			var log = perp == null ?
				$"{victim.playerScript.playerName} (as {victim.playerScript.visibleName}) received {info.Damage} ({info.DamageType}) damage."
				: $"{perp.ExpensiveName()} damaged {victim.playerScript.playerName} (as {victim.playerScript.visibleName}) for {info.Damage} ({info.DamageType}).";
			LogEntry entry = new LogEntry
			{
				AdminActions = new List<AdminActionToTake>(),
				Log = log,
				LogImportance = Severity.MISC,
				Perpetrator = perp,
				Catagory = LogCatagory.MobDamage
			};
			OnNewLog?.Invoke(entry);
		}

		public static void TrackDamage(GameObject perp, Integrity victim, string info)
		{
			var log = perp == null ?
				$"{info} from an undefined source."
				: $"{info} from {perp.ExpensiveName()}).";
			LogEntry entry = new LogEntry
			{
				AdminActions = new List<AdminActionToTake>(),
				Log = log,
				LogImportance = Severity.MISC,
				Perpetrator = perp,
				Catagory = LogCatagory.ObjectDamage
			};
			OnNewLog?.Invoke(entry);
		}
	}
}