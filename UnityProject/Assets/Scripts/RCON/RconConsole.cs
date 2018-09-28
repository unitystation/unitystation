using UnityEngine;


public class RconConsole : MonoBehaviour
	{
		protected static string ServerLog { get; private set; }
		protected static string LastLog { get; private set; }

		protected static string ChatLog { get; private set; }
		protected static string ChatLastLog { get; private set; }

		protected static void AmendLog(string msg){
			ServerLog += msg;
			LastLog = msg;
		}

		protected static void AmendChatLog(string msg)
		{
			ChatLog += msg;
			ChatLastLog = msg;
		}

		protected static void ExecuteCommand(string command){
			command = command.Substring(1, command.Length - 1);
			Logger.Log("TODO remote command execution. command: " + command);
		}
	}

