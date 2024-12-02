using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Logs
{
	public sealed class ThreadLoggy : MonoBehaviour
	{
		public record LogMessageData(LogLevel Level, string Msg, Category Category)
		{
			public LogLevel Level { get; } = Level;
			public string Msg { get; } = Msg;

			public Category Category { get; } = Category;
			public string StackTrace { get; set; } = "";
		}

		private static readonly ConcurrentQueue<LogMessageData> LogQueue = new();

		private static void ProcessLog(LogMessageData log)
		{
			switch (log.Level)
			{
				case LogLevel.Error:
					string msg = $"{log.Msg}{log.StackTrace}";
					Loggy.Error(msg, log.Category);
					break;
				case LogLevel.Warning:
					Loggy.Warning(log.Msg, log.Category);
					break;
				case LogLevel.Info:
					Loggy.Info(log.Msg, log.Category);
					break;
				case LogLevel.Off:
					break;
				case LogLevel.Trace:
				default:
					Loggy.Trace(log.Msg, log.Category);
					break;
			}
		}


		private void Update()
		{
			lock (LogQueue)
			{
				if (LogQueue.TryDequeue(out LogMessageData log) == false) return;
				ProcessLog(log);
			}
		}

		public static void QueueLog(LogLevel level, string msg, Category category = Category.Unknown)
		{
			lock (LogQueue)
			{
				LogMessageData log = new(level, msg, category);
				if (level == LogLevel.Error)
				{
					string stackTrace = $"\n{Environment.StackTrace}";
					log.StackTrace = stackTrace;
				}
				LogQueue.Enqueue(log);
			}
		}
	}
}