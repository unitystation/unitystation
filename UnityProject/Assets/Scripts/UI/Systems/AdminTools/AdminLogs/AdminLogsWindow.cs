using System;
using System.Collections.Generic;
using System.IO;
using Core.Admin.Logs;
using Messages.Client.Admin.Logs;
using TMPro;
using UnityEngine;

namespace UI.Systems.AdminTools.AdminLogs
{
	public class AdminLogsWindow : MonoBehaviour
	{
		private List<LogEntry> logEntries = new List<LogEntry>();
		private List<AdminLogEntryUI> entriesUI = new List<AdminLogEntryUI>();
		[SerializeField] private Transform logEntryBaseTransform;
		[SerializeField] private Transform logsTranform;
		[SerializeField] private TMP_Dropdown logFilesDropdown;
		private int lastPageNumber = 1;
		public int NumberOfPagesAvaliable = 0;

		private void OnEnable()
		{
			UpdateManager.Add(UpdateMe, 4f);
			RequestLogAvaliablePages();
		}

		private void UpdateMe()
		{

		}

		private void RequestLogPage(string logFileName, int page)
		{
			logFileName ??= Path.Combine("Admin", $"{DateTime.Now:yyyy-MM-dd} - {GameManager.RoundID}.txt");
		}

		private void RequestLogAvaliablePages(string logFileName = null)
		{
			logFileName ??= Path.Combine("Admin", $"{DateTime.Now:yyyy-MM-dd} - {GameManager.RoundID}.txt");
			RequestLogFilePagesNumber.Send(logFileName);
		}

		public void UpdateAvaliablePagesNumber(int pageNumber)
		{
			NumberOfPagesAvaliable = pageNumber;
			RequestLogPage(null, lastPageNumber);
		}
	}
}