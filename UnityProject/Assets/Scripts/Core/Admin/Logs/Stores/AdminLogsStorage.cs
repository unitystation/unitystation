using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.Admin.Logs.Interfaces;
using Core.Editor.Attributes;
using Logs;
using SecureStuff;
using UnityEngine;

namespace Core.Admin.Logs.Stores
{
	public class AdminLogsStorage : MonoBehaviour, IAdminStorage
	{
		private Queue<HumanLogEntry> entries = new Queue<HumanLogEntry>();
		private bool readyForQueue = true;

		[SerializeField, SerializeReference, SelectImplementation(typeof(IAdminLogEntryConverter<string>))]
		private IAdminLogEntryConverter<string> EntryConverter;

		private void Start()
		{
			AdminLogsManager.OnNewLog += QueueLog;
		}

		private void Update()
		{
			if (entries.Count == 0) return;
			if (readyForQueue == false) return;
			Store(entries.Dequeue());
		}

		private void QueueLog(LogEntry newEntry)
		{
			if (GameManager.Instance.CurrentRoundState == RoundState.PreRound) return;
			entries.Enqueue(new HumanLogEntry(newEntry));
		}

		public async Task Store(object entry)
		{
			readyForQueue = false;
			string newLog = "\n";
			try
			{
				newLog = EntryConverter.Convert(entry);
				if (newLog == null)
				{
					Loggy.LogError("[AdminLogsStorage/Store()] - Recevied a null entry when attempting to convert logs into a human readable version.");
					readyForQueue = true;
					return;
				}
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
				readyForQueue = true;
				return;
			}
			//TODO: Update this to have operations be IAdminLogEntryConverter specific to allow for things like easy SQLite integretions
			string filePath = Path.Combine("Admin", $"{DateTime.Now:yyyy-MM-dd} - {GameManager.RoundID}.txt");
			CheckForDirectory(filePath);
			await Task.Run(() =>
			{
				WriteToLogsFile(filePath, newLog);
			});
			readyForQueue = true;
		}

		private void CheckForDirectory(string filePath)
		{
			if (AccessFile.Exists(filePath, true, FolderType.Logs, Application.isEditor == false) == false)
			{
				AccessFile.Save(filePath, "", FolderType.Logs, Application.isEditor == false);
			}
		}

		private void WriteToLogsFile(string filePath, string newLog)
		{
			try
			{
				AccessFile.AppendAllText(filePath, newLog, FolderType.Logs, Application.isEditor == false);
			}
			catch (UnauthorizedAccessException uae)
			{
				Loggy.Log("Access to the path is denied: " + uae);
			}
			catch (PathTooLongException ptle) //windows reeeeeEEEEEEEEEEE
			{
				Loggy.Log("The specified path, file name, or both are too long: " + ptle);
			}
			catch (IOException ioe)
			{
				Loggy.Log("An I/O error occurred while opening the file: " + ioe);
			}
			catch (Exception ex)
			{
				Loggy.Log("An unexpected error occurred: " + ex);
			}
		}
	}
}