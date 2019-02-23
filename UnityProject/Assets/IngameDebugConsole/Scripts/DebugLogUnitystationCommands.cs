using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IngameDebugConsole
{
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

		[ConsoleMethod("damage-self", "Usage:\ndamage-self <bodyPart> <brute amount> <burn amount>\nExample: damage-self LeftArm 40 20")]
		public static void RunDamageSelf(string bodyPartString, int burnDamage, int bruteDamage)
		{
			
			
			bool success = BodyPartType.TryParse(bodyPartString, true, out BodyPartType bodyPart);

			if (success)
			{
				bool playerSpawned = (PlayerManager.LocalPlayer != null);
				if (!playerSpawned)
				{
					Logger.LogError("Cannot damage player. Player has not spawned.", Category.DebugConsole);

				}
				else
				{
					Logger.Log("Debugger inflicting " + burnDamage + " burn damage and " + bruteDamage + " brute damage on " + bodyPart + " of " + PlayerManager.LocalPlayer.name, Category.DebugConsole);
					HealthBodyPartMessage.Send(PlayerManager.LocalPlayer, PlayerManager.LocalPlayer, bodyPart, burnDamage, bruteDamage);
				}
			}
			else
			{
				Logger.LogError("Invalid body part '"+ bodyPartString+ "'", Category.DebugConsole);
			}
		}

		[ConsoleMethod("restart-round", "restarts the round")]
		public static void RunRestartRound()
		{
				Logger.Log("Round restart triggered by DebugConsole.", Category.DebugConsole);
				GameManager.Instance.RestartRound();
		}

	}
}
