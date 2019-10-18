using System;
using System.Collections;
using System.Collections.Generic;
using PathFinding;
using UnityEngine;
using Mirror;
using UnityEditor;
using Random = UnityEngine.Random;

namespace IngameDebugConsole
{
	/// <summary>
	/// Contains all the custom defined commands for the IngameDebugLogger
	/// </summary>
	public class DebugLogUnitystationCommands : MonoBehaviour
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

		[MenuItem("Networking/Restart round")]
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

		[MenuItem("Networking/Push everyone up")]
		private static void PushEveryoneUp()
		{
			foreach (ConnectedPlayer player in PlayerList.Instance.InGamePlayers)
			{
				player.GameObject.GetComponent<PlayerScript>().PlayerSync.Push(Vector2Int.up);
			}
		}
		[MenuItem("Networking/Spawn some meat")]
		private static void SpawnMeat()
		{
			foreach (ConnectedPlayer player in PlayerList.Instance.InGamePlayers) {
				Vector3 playerPos = player.GameObject.GetComponent<PlayerScript>().PlayerSync.ServerState.WorldPosition;
				Vector3 spawnPos = playerPos + new Vector3( 0, 2, 0 );
				GameObject mealPrefab = CraftingManager.Meals.FindOutputMeal("Meat Steak");
				var slabs = new List<CustomNetTransform>();
				for ( int i = 0; i < 5; i++ ) {
					slabs.Add( PoolManager.PoolNetworkInstantiate(mealPrefab, spawnPos).GetComponent<CustomNetTransform>() );
				}
				for ( var i = 0; i < slabs.Count; i++ ) {
					Vector3 vector3 = i%2 == 0 ? new Vector3(i,-i,0) : new Vector3(-i,i,0);
					slabs[i].ForceDrop( spawnPos + vector3/10 );
				}
			}
		}
		[MenuItem("Networking/Print player positions")]
		private static void PrintPlayerPositions()
		{
			//For every player in the connected player list (this list is serverside-only)
			foreach (ConnectedPlayer player in PlayerList.Instance.InGamePlayers) {
				//Printing this the pretty way, example:
				//Bob (CAPTAIN) is located at (77,0, 52,0, 0,0)
				Logger.LogFormat( "{0} ({1)} is located at {2}.", Category.Server, player.Name, player.Job, player.Script.WorldPos );
			}

		}

		[MenuItem("Networking/Spawn dummy player")]
		[ConsoleMethod("spawn-dummy", "Spawn dummy player (Server)")]
		private static void SpawnDummyPlayer() {
			SpawnHandler.SpawnDummyPlayer( JobType.ASSISTANT );
		}

		[MenuItem("Networking/Transform Waltz (Server)")]
		private static void MoveAll()
		{
			CustomNetworkManager.Instance.MoveAll();
		}

		[MenuItem("Networking/Gib All (Server)")]
		[ConsoleMethod("gib-all", "Gib All (Server)")]
		private static void GibAll()
		{
			GibMessage.Send();
		}

		[MenuItem("Networking/Reset round time")]
		[ConsoleMethod("reset-time", "Reset round time")]
		private static void ExtendRoundTime()
		{
			GameManager.Instance.ResetRoundTime();
		}

		[MenuItem("Networking/Kill local player (Server only)")]
		[ConsoleMethod("suicide", "Kill local player (Server only)")]
		private static void KillLocalPlayer()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				PlayerManager.LocalPlayerScript.playerHealth.ApplyDamage(null, 99999f, AttackType.Internal, DamageType.Brute);
			}
		}

		[MenuItem("Networking/Respawn local player (Server only)")]
		[ConsoleMethod("respawn", "Respawn local player (Server only)")]
		private static void RespawnLocalPlayer()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRespawnPlayer();
			}
		}

		private static HashSet<MatrixInfo> usedMatrices = new HashSet<MatrixInfo>();
		private static Tuple<MatrixInfo, Vector3> lastUsedMatrix;

		[MenuItem("Networking/Crash random matrix into station")]
		private static void CrashIntoStation()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				StopLastCrashed();

				Vector2 appearPos = new Vector2Int(-37, 37);
				foreach ( var movableMatrix in MatrixManager.Instance.MovableMatrices )
				{
					if ( movableMatrix.GameObject.name.ToLower().Contains( "large" ) )
					{
						continue;
					}

					if ( usedMatrices.Contains( movableMatrix ) )
					{
						continue;
					}

					usedMatrices.Add( movableMatrix );
					lastUsedMatrix = new Tuple<MatrixInfo, Vector3>(movableMatrix, movableMatrix.MatrixMove.State.Position);
					var mm = movableMatrix.MatrixMove;
					mm.SetPosition( appearPos );
					mm.RequiresFuel = false;
					mm.SafetyProtocolsOn = false;
					mm.RotateTo( Orientation.Right );
					mm.SetSpeed( 4 );
					mm.StartMovement();

					break;
				}
			}
		}
		[MenuItem("Networking/Stop last crashed matrix")]
		private static void StopLastCrashed()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				if ( lastUsedMatrix != null )
				{
					lastUsedMatrix.Item1.MatrixMove.StopMovement();
					lastUsedMatrix.Item1.MatrixMove.SetPosition( lastUsedMatrix.Item2 );
					lastUsedMatrix = null;
				}
			}
		}
	}
}