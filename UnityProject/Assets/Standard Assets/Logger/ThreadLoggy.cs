using System;
using System.Collections.Generic;
using UnityEngine;

namespace Logs
{
	public sealed class ThreadLoggy : MonoBehaviour
	{
		public static List<string> otherThreadLogs = new List<string>();

		// Update is called once per frame
		void Update()
		{
			lock (otherThreadLogs)
			{
				foreach (var log in otherThreadLogs)
				{
					Loggy.LogError(log);
				}

				otherThreadLogs.Clear();
			}
		}

		public static void AddLog(string msg, Category category = Category.Unknown)
		{
			lock (otherThreadLogs)
			{
				otherThreadLogs.Add(msg + " on the thread " + category.ToString()  + "\n" + Environment.StackTrace);
			}
		}
	}
}