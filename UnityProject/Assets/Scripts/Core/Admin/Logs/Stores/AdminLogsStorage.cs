using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Admin.Logs.Interfaces;
using Core.Editor.Attributes;
using Initialisation;
using Logs;
using NUnit.Framework;
using SecureStuff;
using Shared.Managers;
using UnityEngine;

namespace Core.Admin.Logs.Stores
{
	public class AdminLogsStorage : SingletonManager<AdminLogsStorage>, IAdminStorage
	{
		private Queue<HumanLogEntry> entries = new Queue<HumanLogEntry>();
		private bool readyForQueue = true;

		public const int ENTRY_PAGE_SIZE = 45;

		[SerializeField, SerializeReference, SelectImplementation(typeof(IAdminLogEntryConverter<string>))]
		private IAdminLogEntryConverter<string> EntryConverter;
		public IAdminLogEntryConverter<string> Converter => EntryConverter;

		public override void Start()
		{
			base.Start();
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
			if (AccessFile.Exists(filePath, true, FolderType.Logs, false) == false)
			{
				AccessFile.Save(filePath, "", FolderType.Logs, false);
			}
		}

		private void WriteToLogsFile(string filePath, string newLog)
		{
			try
			{
				AccessFile.AppendAllText(filePath, newLog, FolderType.Logs, false);
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

		public static void AddToEntryList(ref List<LogEntry> entries, string logLine)
		{
			LogEntry logEntry = Instance.Converter.ConvertBackSingle(logLine);
			if (logEntry != null)
			{
				entries.Add(logEntry);
			}
			else
			{
				Loggy.LogError($"[AdminLogsStorage/FetchLogsPaginated()] - Failed to convert log line to LogEntry: {logLine}");
			}
		}

		public static async Task<List<LogEntry>> FetchAllLogs(string fileName)
		{
			List<LogEntry> logEntries = new List<LogEntry>();
			string filePath = Path.Combine("Admin", fileName);
			try
			{
				if (AccessFile.Exists(filePath, true, FolderType.Logs, false) == false)
				{
					Loggy.LogError($"[AdminLogsStorage/FetchLogs()] - File not found: {filePath}");
				}
				string fileContent = await Task.Run(() => AccessFile.Load(filePath, FolderType.Logs, false));
				string[] logLines = fileContent.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string logLine in logLines)
				{
					try
					{
						AddToEntryList(ref logEntries, logLine);
					}
					catch (Exception e)
					{
						Loggy.LogError($"[AdminLogsStorage/FetchLogs()] - Exception during log entry conversion: {e}");
					}
				}
			}
			catch (Exception e)
			{
				Loggy.LogError($"[AdminLogsStorage/FetchLogs()] - Exception during file read: {e}");
			}
			return logEntries;
		}

		public static async Task<List<LogEntry>> FetchLogsPaginated(string fileName, int pageNumber, int pageSize = ENTRY_PAGE_SIZE)
		{
			async Task<string> LoadData(string filePath)
			{
				var data = "";
				try
				{
					data = await Task.Run(() => AccessFile.Load(filePath, FolderType.Logs, false));
				}
				catch (Exception e)
				{
					Loggy.LogError($"[AdminLogsStorage/FetchLogsPaginated()] - Exception during file read: {e}");
				}
				return data;
			}

			if (pageNumber <= 0) pageNumber = 1;
			List<LogEntry> logEntries = new List<LogEntry>();
			string filePath = Path.Combine("Admin", fileName);
			try
			{
				if (AccessFile.Exists(filePath, true, FolderType.Logs, false) == false)
				{
					Loggy.LogError($"[AdminLogsStorage/FetchLogsPaginated()] - File not found: {filePath}");
				}
				string fileContent = await LoadData(filePath);
				LoadManager.DoInMainThread(() => Loggy.Log("Moving back to main thread."));
				if (string.IsNullOrEmpty(fileContent)) return logEntries;
				string[] logLines = fileContent.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
				int skip = (pageNumber - 1) * pageSize;
				int take = pageSize;
				var paginatedLogLines = logLines.Skip(skip).Take(take);
				foreach (string logLine in paginatedLogLines)
				{
					try
					{
						AddToEntryList(ref logEntries, logLine);
					}
					catch (Exception e)
					{
						Loggy.LogError($"[AdminLogsStorage/FetchLogsPaginated()] - Exception during log entry conversion: {e}");
					}
				}
			}
			catch (Exception e)
			{
				Loggy.LogError($"[AdminLogsStorage/FetchLogsPaginated()] - Exception during file read: {e}");
			}
			return logEntries;
		}

		public static async Task<int> GetTotalPages(string fileName, int pageSize = ENTRY_PAGE_SIZE)
		{
			string filePath = Path.Combine("Admin", fileName);
			int totalEntries = 0;
			try
			{
				if (AccessFile.Exists(filePath, true, FolderType.Logs, false) == false)
				{
					Loggy.LogError($"[AdminLogsStorage/GetTotalPages()] - File not found: {filePath}");
					return totalEntries;
				}
				string fileContent = await Task.Run(() => AccessFile.Load(filePath, FolderType.Logs, false));
				LoadManager.DoInMainThread(() => Loggy.Log("Moving back to main thread."));
				string[] logLines = fileContent.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
				totalEntries = logLines.Length;
			}
			catch (Exception e)
			{
				Loggy.LogError($"[AdminLogsStorage/GetTotalPages()] - Exception during file read: {e}");
				return 0;
			}
			return (int)Math.Ceiling((double)totalEntries / pageSize);
		}

		public static List<string> GetAllLogFiles()
		{
			List<string> totalEntries = new List<string>();
			if (AccessFile.Exists("Admin", false, FolderType.Logs, false) == false)
			{
				Loggy.LogError($"[AdminLogsStorage/GetTotalPages()] - Logs folder not found.");
				return totalEntries;
			}
			string[] files = AccessFile.DirectoriesOrFilesIn("Admin", FolderType.Logs, false);
			foreach (string file in files)
			{
				totalEntries.Add(file);
			}
			return totalEntries;
		}
	}
}