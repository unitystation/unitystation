using UnityEngine;
using System;

namespace Rcon
{
	public class RconConsole : MonoBehaviour
	{
		protected static string ServerLog { get; private set; }
		protected static string LastLog { get; private set; }

		public static void AddLog(string msg){
			msg = DateTime.UtcNow + ":    " + msg + "<br>";
			ServerLog += msg;
			LastLog = msg;
		}
	}
}
