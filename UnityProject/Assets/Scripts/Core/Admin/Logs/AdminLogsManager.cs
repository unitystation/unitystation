using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

		public static void AddNewLog(GameObject Tracking1, string info1, GameObject Tracking2,string info2, GameObject Tracking3,string info3 ,LogCategory category,
			Severity severity = Severity.MISC)
		{
			AddNewLogInternal(new List<LogInfo>() {Tracking1.GetLogInfo(), info1.GetLogInfo(), Tracking2.GetLogInfo(), info2.GetLogInfo(), Tracking3.GetLogInfo(), info3.GetLogInfo()}, category, severity);

		}

		public static void AddNewLog(GameObject Tracking1, string info1, GameObject Tracking2,string info2, GameObject Tracking3,LogCategory category,
			Severity severity = Severity.MISC)
		{
			AddNewLogInternal(new List<LogInfo>() {Tracking1.GetLogInfo(), info1.GetLogInfo(), Tracking2.GetLogInfo(), info2.GetLogInfo(), Tracking3.GetLogInfo()}, category, severity);
		}


		public static void AddNewLog(GameObject Tracking1, string info1, GameObject Tracking2,string info2,LogCategory category,
			Severity severity = Severity.MISC)
		{
			AddNewLogInternal(new List<LogInfo>() {Tracking1.GetLogInfo(), info1.GetLogInfo(), Tracking2.GetLogInfo(), info2.GetLogInfo()}, category, severity);
		}


		public static void AddNewLog(GameObject Tracking1, string info1, GameObject Tracking2,LogCategory category,
			Severity severity = Severity.MISC)
		{
			AddNewLogInternal(new List<LogInfo>() {Tracking1.GetLogInfo(), info1.GetLogInfo(), Tracking2.GetLogInfo()}, category, severity);
		}

		public static void AddNewLog(string info1, GameObject Tracking1,string info2, GameObject Tracking2,string info3, GameObject Tracking3, LogCategory category,
			Severity severity = Severity.MISC)
		{
			AddNewLogInternal(new List<LogInfo>() {info1.GetLogInfo(), Tracking1.GetLogInfo(), info2.GetLogInfo(), Tracking2.GetLogInfo(), info3.GetLogInfo(),  Tracking3.GetLogInfo() }, category, severity);

		}


		public static void AddNewLog(string info1, GameObject Tracking1,string info2, GameObject Tracking2,string info3, LogCategory category,
			Severity severity = Severity.MISC)
		{
			AddNewLogInternal(new List<LogInfo>() {info1.GetLogInfo(), Tracking1.GetLogInfo(), info2.GetLogInfo(), Tracking2.GetLogInfo(), info3.GetLogInfo()}, category, severity);
		}

		public static void AddNewLog(string info1, GameObject Tracking1,string info2, GameObject Tracking2, LogCategory category,
			Severity severity = Severity.MISC)
		{
			AddNewLogInternal(new List<LogInfo>() {info1.GetLogInfo(), Tracking1.GetLogInfo(), info2.GetLogInfo(), Tracking2.GetLogInfo()}, category, severity);
		}


		public static void AddNewLog(string info1, GameObject Tracking1,string info2, LogCategory category,
			Severity severity = Severity.MISC)
		{
			AddNewLogInternal(new List<LogInfo>() {info1.GetLogInfo(), Tracking1.GetLogInfo(), info2.GetLogInfo()}, category, severity);
		}


		public static void AddNewLog(string info1, GameObject Tracking1, LogCategory category,
			Severity severity = Severity.MISC)
		{
			AddNewLogInternal(new List<LogInfo>() {info1.GetLogInfo(), Tracking1.GetLogInfo()}, category, severity);
		}


		public static void AddNewLog(string info1, LogCategory category,
			Severity severity = Severity.MISC)
		{
			AddNewLogInternal(new List<LogInfo>() {info1.GetLogInfo()}, category, severity);
		}

		public static void AddNewLog(GameObject Tracking1, string info1, LogCategory category, Severity severity = Severity.MISC)
		{
			AddNewLogInternal(new List<LogInfo>() {Tracking1.GetLogInfo(), info1.GetLogInfo()}, category, severity);
		}
		private static void AddNewLogInternal(List<LogInfo> Info,  LogCategory category, Severity severity = Severity.MISC)
		{
			if (CustomNetworkManager.IsServer == false) return;

			LogEntry entry = new LogEntry
			{
				AdminActions = new List<AdminActionToTake>(),
				Log = Info,
				LogImportance = severity,
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
				//Log = log,
				LogImportance = Severity.DEATH,
				//Perpetrator = GetPerpString(perp),
				Category = LogCategory.MobDamage
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
				//Log = log,
				LogImportance = Severity.MISC,
				//Perpetrator = GetPerpString(perp),
				Category = LogCategory.MobDamage
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
				//Log = log,
				LogImportance = Severity.MISC,
				//Perpetrator = GetPerpString(perp),
				Category = LogCategory.ObjectDamage
			};
			OnNewLog?.Invoke(entry);
		}

		public static string GetPerpString(GameObject perp)
		{
			if (perp == null) return "[]";
			if (perp.NetId() == global::NetId.Invalid) return perp.ExpensiveName();
			return $"{perp.ExpensiveName()}->{perp.NetId()}";
		}

		public static uint GetPerpIdFromString(string input)
		{
			string pattern =  @"->(\d+)";

			Regex regex = new Regex(pattern);
			Match match = regex.Match(input);
			if (match.Success)
			{
				string extractedValue = match.Groups[1].Value;
				if (uint.TryParse(extractedValue, out uint result))
				{
					return result;
				}
				else
				{
					return global::NetId.Invalid;
				}
			}
			else
			{
				return global::NetId.Invalid;
			}
		}
	}
}