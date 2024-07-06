using System;
using System.Collections.Generic;
using Core.Admin.Logs;
using UnityEngine;

namespace UI.Systems.AdminTools.AdminLogs
{
	public class AdminLogsWindow : MonoBehaviour
	{
		private List<LogEntry> _logEntries = new List<LogEntry>();

		private void OnEnable()
		{
			UpdateManager.Add(UpdateMe, 4f);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}


		private void UpdateMe()
		{

		}
	}
}