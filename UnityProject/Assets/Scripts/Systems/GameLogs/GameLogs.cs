using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


namespace Systems.GameLogs
{
	public class GameLogs : Managers.SingletonManager<GameLogs>
	{
		private string logPath = "/Server/Logs/"; //Where are logs stored?
		private string path = ""; //The path used to fetch and update logs; updated on start().

		private void Start()
		{
			path = $"{Application.persistentDataPath}{logPath}";
			Logger.Log(path);
			if (Directory.Exists(path) == false)
			{
				Directory.CreateDirectory(path);
			}
		}

		public void Log(string ThingToLog, LogType logType = LogType.General)
		{
			if(ThingToLog == null || path == null) return;
			if (FileExists(logType) == false)
			{
				File.Create($"{path}{nameof(logType)}.txt").Dispose();
			}

			string log = $"[{DateTime.UtcNow}] - {ThingToLog}";
			using (StreamWriter sw = File.AppendText($"{path}{logType.ToString()}.txt"))
			{
				sw.WriteLine($"{log}");
				sw.Dispose();
			}
		}

		private bool FileExists(LogType logType)
		{
			return File.Exists($"{path}{logType.ToString()}.txt");
		}

		public string[] ReteriveAllLogsOfType(LogType logType)
		{
			if (FileExists(logType) == false)
			{
				string[] error = new[] {"no logs found"};
				return error;
			}
			return File.ReadAllLines($"{path}{logType.ToString()}.txt");
		}
	}

	public enum LogType
	{
		General,
		Admin,
		Combat,
		Explosion,
		Chemistry,
		Shuttle,
		Destruction,
		Antag,
		Interactions,
		Chat
	}

}