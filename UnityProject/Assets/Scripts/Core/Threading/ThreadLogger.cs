using System.Collections.Generic;
using UnityEngine;

namespace Core.Threading
{
	public class ThreadLogger : MonoBehaviour
	{
		public static List<string> otherThreadLogs = new List<string>();

		// Update is called once per frame
		void Update()
		{
			lock (otherThreadLogs)
			{
				foreach (var log in otherThreadLogs)
				{
					Logger.LogError(log);
				}
				otherThreadLogs.Clear();
			}
		}

		public static void AddLog(string msg, Category category = Category.Unknown)
		{
			lock (otherThreadLogs)
			{
				otherThreadLogs.Add(msg + " on the thread " + category.ToString());
			}
		}
	}
}
