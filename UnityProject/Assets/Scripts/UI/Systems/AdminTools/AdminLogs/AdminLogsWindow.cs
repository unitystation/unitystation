using System;
using System.Collections.Generic;
using System.IO;
using Core.Admin.Logs;
using Logs;
using Messages.Client.Admin.Logs;
using TMPro;
using UnityEngine;

namespace UI.Systems.AdminTools.AdminLogs
{
	public class AdminLogsWindow : MonoBehaviour
	{
		private List<LogEntry> logEntries = new List<LogEntry>();
		private List<AdminLogEntryUI> entriesUI = new List<AdminLogEntryUI>();
		[SerializeField] private AdminLogEntryUI logEntryBase;
		[SerializeField] private Transform logsTranform;
		[SerializeField] private TMP_Dropdown logFilesDropdown;
		private int lastPageNumber = 1;
		public int NumberOfPagesAvaliable = 0;

		private void OnEnable()
		{
			RequestLogAvaliablePages();
			RequestAllLogFileNames();
		}

		private string GetLogFileName()
		{
			return Path.Combine("Admin", $"{DateTime.Now:yyyy-MM-dd} - {GameManager.RoundID}.txt");
		}

		private void RequestLogPage(string logFileName, int page)
		{
			logFileName ??= GetLogFileName();
			RequestLogFilePageEntries.Send(page, logFileName);
		}

		private void RequestLogAvaliablePages(string logFileName = null)
		{
			logFileName ??= GetLogFileName();
			RequestLogFilePagesNumber.Send(logFileName);
		}

		private void RequestAllLogFileNames()
		{
			RequestLogFilesNames.Send(new RequestLogFilesNames.NetMessage());
		}

		public void UpdateLogFileDropdown(List<string> logFileNames)
		{
			logFilesDropdown.ClearOptions();
			logFilesDropdown.AddOptions(logFileNames);
		}

		public void UpdateAvaliablePagesNumber(int pageNumber)
		{
			NumberOfPagesAvaliable = pageNumber;
			RequestLogPage(null, lastPageNumber);
		}

		public void UpdateLogEntries(List<LogEntry> newEntries)
		{
			logEntries = newEntries;
			foreach (var oldEntries in entriesUI)
			{
				Destroy(oldEntries.gameObject);
			}
			entriesUI.Clear();
			foreach (LogEntry newEntry in newEntries)
			{
				AdminLogEntryUI newEntryUI = Instantiate(logEntryBase, logsTranform, false);
				newEntryUI.Setup(newEntry);
			}
		}

		public void ShowErrorNoLogFound()
		{
			Loggy.LogError($"{GetLogFileName()} not found.");
		}
	}
}