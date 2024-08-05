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
		[SerializeField] private TMP_Text AvaliablePagesText;
		[SerializeField] private TMP_InputField CurrentSelectedPageInput;
		private int lastPageNumber = 1;
		public int NumberOfPagesAvaliable = 0;

		private void OnEnable()
		{
			RequestAllLogFileNames();
		}

		private void RequestLogPage(string logFileName, int page)
		{
			RequestLogFilePageEntries.Send(page, logFileName);
		}

		private void RequestLogAvaliablePages(string logFileName)
		{
			if (logFileName == null)
			{
				Loggy.LogError("HEY SHITASS, NO FUCKING FILE NAME.");
				return;
			}
			RequestLogFilePagesNumber.Send(logFileName);
		}

		private void RequestAllLogFileNames()
		{
			RequestLogFilesNames.Send(new RequestLogFilesNames.NetMessage());
		}

		public void UpdateLogFileDropdown(List<string> logFileNames)
		{
			Loggy.Log(logFileNames.Count.ToString());
			logFilesDropdown.ClearOptions();
			logFilesDropdown.AddOptions(logFileNames);
			logFilesDropdown.value = logFilesDropdown.options.Count - 1;
			RequestLogAvaliablePages(logFilesDropdown.captionText.text);
		}

		public void UpdateAvaliablePagesNumber(int pageNumber)
		{
			NumberOfPagesAvaliable = pageNumber;
			lastPageNumber = pageNumber;
			AvaliablePagesText.text = pageNumber.ToString();
			CurrentSelectedPageInput.text = pageNumber.ToString();
			RequestLogPage(logFilesDropdown.captionText.text, lastPageNumber);
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

		public void OnLogFilesDropdownValueChange()
		{
			RequestLogAvaliablePages(logFilesDropdown.captionText.text);
		}

		public void OnPageSelectorValueChange(string newValue)
		{
			if (int.TryParse(newValue, out int pageNumber) == false)
			{
				CurrentSelectedPageInput.text = "";
				return;
			}
			if (pageNumber > NumberOfPagesAvaliable)
			{
				CurrentSelectedPageInput.text = NumberOfPagesAvaliable.ToString();
			}
		}

		public void OnGoToPageButtonClick()
		{
			RequestLogPage(logFilesDropdown.captionText.text, int.Parse(CurrentSelectedPageInput.text));
		}
	}
}