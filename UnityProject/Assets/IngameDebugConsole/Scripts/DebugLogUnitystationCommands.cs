using System;
using System.Collections;
using System.Collections.Generic;
using PathFinding;
using UnityEngine;
using UnityEngine.Networking;

namespace IngameDebugConsole
{
	/// <summary>
	/// Contains all the custom defined commands for the IngameDebugLogger
	/// </summary>
	public class DebugLogUnitystationCommands
	{
		[ConsoleMethod("suicide", "kill yo' self")]
		public static void RunSuicide()
		{
			bool playerSpawned = (PlayerManager.LocalPlayer != null);
			if (!playerSpawned)
			{
				Logger.LogError("Cannot commit suicide. Player has not spawned.", Category.DebugConsole);

			}
			else
			{
				SuicideMessage.Send(null);
			}
		}

		[ConsoleMethod("damage-self", "Server only cmd.\nUsage:\ndamage-self <bodyPart> <brute amount> <burn amount>\nExample: damage-self LeftArm 40 20.Insert")]
		public static void RunDamageSelf(string bodyPartString, int burnDamage, int bruteDamage)
		{
			if (CustomNetworkManager.Instance._isServer == false)
			{
				Logger.LogError("Can only execute command from server.", Category.DebugConsole);
				return;
			}

			bool success = BodyPartType.TryParse(bodyPartString, true, out BodyPartType bodyPart);
			if (success == false)
			{
				Logger.LogError("Invalid body part '" + bodyPartString + "'", Category.DebugConsole);
				return;
			}

			bool playerSpawned = (PlayerManager.LocalPlayer != null);
			if (playerSpawned == false)
			{
				Logger.LogError("Cannot damage player. Player has not spawned.", Category.DebugConsole);
				return;
			}

			Logger.Log("Debugger inflicting " + burnDamage + " burn damage and " + bruteDamage + " brute damage on " + bodyPart + " of " + PlayerManager.LocalPlayer.name, Category.DebugConsole);
			HealthBodyPartMessage.Send(PlayerManager.LocalPlayer, PlayerManager.LocalPlayer, bodyPart, burnDamage, bruteDamage);
		}

		[ConsoleMethod("restart-round", "restarts the round. Server only cmd.")]
		public static void RunRestartRound()
		{
			if (CustomNetworkManager.Instance._isServer == false)
			{
				Logger.LogError("Can only execute command from server.", Category.DebugConsole);
				return;
			}

			Logger.Log("Triggered round restart from DebugConsole.", Category.DebugConsole);
			GameManager.Instance.RestartRound();
		}

		[ConsoleMethod("call-shuttle", "Calls the escape shuttle. Server only command")]
		public static void CallEscapeShuttle()
		{
			if (CustomNetworkManager.Instance._isServer == false)
			{
				Logger.LogError("Can only execute command from server.", Category.DebugConsole);
				return;
			}

			if (GameManager.Instance.PrimaryEscapeShuttle.Status == ShuttleStatus.DockedCentcom)
			{
				GameManager.Instance.PrimaryEscapeShuttle.CallShuttle(out var result, 40);
				Logger.Log("Called Escape shuttle from DebugConsole: "+result, Category.DebugConsole);
			}
			else
			{
				Logger.Log("Escape shuttle isn't docked at centcom to be called.", Category.DebugConsole);
			}
		}

		[ConsoleMethod("log", "Adjust individual log levels\nUsage:\nloglevel <category> <level> \nExample: loglevel Health 0\n-1 = Off \n0 = Error \n1 = Warning \n2 = Info \n 3 = Trace")]
		public static void SetLogLevel(string logCategory, int level)
		{
			bool catFound = false;
			Category category = Category.Unknown;
			foreach (Category c in Enum.GetValues(typeof(Category)))
			{
				if (c.ToString().ToLower() == logCategory.ToLower())
				{
					catFound = true;
					category = c;
				}
			}

			if (!catFound)
			{
				Logger.Log("Category not found", Category.DebugConsole);
				return;
			}

			LogLevel logLevel = LogLevel.Info;

			if (level > (int)LogLevel.Trace)
			{
				logLevel = LogLevel.Trace;
			}
			else
			{
				logLevel = (LogLevel)level;
			}

			Logger.SetLogLevel(category, logLevel);
		}
	}
}