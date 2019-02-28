using System.Collections;
using System.Collections.Generic;
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

			if (!EscapeShuttle.Instance.spawnedIn)
			{
				Logger.Log("Called Escape shuttle from DebugConsole.", Category.DebugConsole);
				EscapeShuttle.Instance.CallEscapeShuttle();
			}
			else
			{
				Logger.Log("Escape shuttle already called.", Category.DebugConsole);
			}
		}
	}
}