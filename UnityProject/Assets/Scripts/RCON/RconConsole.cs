using UnityEngine;
using System;

namespace Rcon
{
	public class RconConsole : MonoBehaviour
	{
		private static string ServerLog;
		protected static string LastLog { get; private set; }

		public static void AddLog(string msg){
			msg = DateTime.UtcNow + ":    " + msg + "\n";
			ServerLog += msg;
			LastLog = msg;
		}
	}
}
