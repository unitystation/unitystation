using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Systems.Atmospherics;
using Systems.Cargo;
using Random = UnityEngine.Random;
using DatabaseAPI;
using Items;
using Messages.Client;
using Messages.Server;
using Messages.Server.HealthMessages;
using ScriptableObjects;

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
				Logger.Log("Cannot commit suicide. Player has not spawned.", Category.DebugConsole);

			}
			else
			{
				SuicideMessage.Send(null);
			}
		}

		[ConsoleMethod("myid", "Prints your uuid for your player account")]
		public static void RunPrintUID()
		{
			Logger.Log($"{ServerData.UserID}", Category.DebugConsole);
		}

		[ConsoleMethod("copyid", "Copies your uuid to your clipboard.")]
		public static void CopyUserID()
		{
			TextUtils.CopyTextToClipboard($"{ServerData.UserID}");
			Logger.Log($"UUID Copied to clipboard.", Category.DebugConsole);
		}

		[ConsoleMethod("damage-self", "Server only cmd.\nUsage:\ndamage-self <bodyPart> <brute amount> <burn amount>\nExample: damage-self LeftArm 40 20.Insert")]
		public static void RunDamageSelf(string bodyPartString, int burnDamage, int bruteDamage)
		{
			if (CustomNetworkManager.Instance._isServer == false)
			{
				Logger.Log("Can only execute command from server.", Category.DebugConsole);
				return;
			}

			bool success = BodyPartType.TryParse(bodyPartString, true, out BodyPartType bodyPart);
			if (success == false)
			{
				Logger.Log("Invalid body part '" + bodyPartString + "'", Category.DebugConsole);
				return;
			}

			bool playerSpawned = (PlayerManager.LocalPlayer != null);
			if (playerSpawned == false)
			{
				Logger.Log("Cannot damage player. Player has not spawned.", Category.DebugConsole);
				return;
			}

			Logger.Log($"Debugger inflicting {burnDamage} burn damage and {bruteDamage} brute damage on {bodyPart} of {PlayerManager.LocalPlayer.name}", Category.DebugConsole);
			HealthBodyPartMessage.Send(PlayerManager.LocalPlayer, PlayerManager.LocalPlayer, bodyPart, burnDamage, bruteDamage);
		}

#if UNITY_EDITOR
		[MenuItem("Networking/Restart round")]
#endif
		[ConsoleMethod("restart-round", "restarts the round immediately. Server only cmd.")]
		public static void RunRestartRound()
		{
			if (CustomNetworkManager.Instance._isServer == false)
			{
				Logger.Log("Can only execute command from server.", Category.DebugConsole);
				return;
			}

			Logger.Log("Triggered round restart from DebugConsole.", Category.DebugConsole);
			VideoPlayerMessage.Send(VideoType.RestartRound);
			GameManager.Instance.RoundEndTime = 5f;
			GameManager.Instance.EndRound();
		}

#if UNITY_EDITOR
		[MenuItem("Networking/End round")]
#endif
		[ConsoleMethod("end-round", "ends the round, triggering normal round end logic, letting people see their greentext. Server only cmd.")]
		public static void RunEndRound()
		{
			if (CustomNetworkManager.Instance._isServer == false)
			{
				Logger.Log("Can only execute command from server.", Category.DebugConsole);
				return;
			}

			Logger.Log("Triggered round end from DebugConsole.", Category.DebugConsole);
			VideoPlayerMessage.Send(VideoType.RestartRound);
			GameManager.Instance.EndRound();
		}

#if UNITY_EDITOR
		[MenuItem("Networking/Start now")]
#endif
		[ConsoleMethod("start-now", "Bypass start countdown and start immediately. Server only cmd.")]
		public static void StartNow()
		{
			if (CustomNetworkManager.Instance._isServer == false)
			{
				Logger.Log("Can only execute command from server.", Category.DebugConsole);
				return;
			}

			if (GameManager.Instance.CurrentRoundState == RoundState.PreRound && GameManager.Instance.waitForStart)
			{
				Logger.Log("Triggered round countdown skip (start now) from DebugConsole.", Category.DebugConsole);
				GameManager.Instance.StartRound();
			}
			else
			{
				Logger.Log("Can only execute during pre-round / countdown.", Category.DebugConsole);
				return;
			}

		}

#if UNITY_EDITOR
		[MenuItem("Networking/Call shuttle")]
#endif
		[ConsoleMethod("call-shuttle", "Calls the escape shuttle. Server only command")]
		public static void CallEscapeShuttle()
		{
			if (CustomNetworkManager.Instance._isServer == false)
			{
				Logger.Log("Can only execute command from server.", Category.DebugConsole);
				return;
			}

			if (GameManager.Instance.PrimaryEscapeShuttle.Status == EscapeShuttleStatus.DockedCentcom)
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
#if UNITY_EDITOR
		[MenuItem("Networking/Push everyone up")]
#endif
		private static void PushEveryoneUp()
		{
			foreach (ConnectedPlayer player in PlayerList.Instance.InGamePlayers)
			{
				player.GameObject.GetComponent<PlayerScript>().PlayerSync.Push(Vector2Int.up);
			}
		}
#if UNITY_EDITOR
		[MenuItem("Networking/Spawn some meat")]
#endif
		private static void SpawnMeat()
		{
			foreach (ConnectedPlayer player in PlayerList.Instance.InGamePlayers) {
				Vector3 playerPos = player.Script.WorldPos;
				Vector3 spawnPos = playerPos + new Vector3( 0, 2, 0 );
				GameObject mealPrefab = CraftingManager.Meals.FindOutputMeal("Meat Steak");
				var slabs = new List<CustomNetTransform>();
				for ( int i = 0; i < 5; i++ ) {
					slabs.Add( Spawn.ServerPrefab(mealPrefab, spawnPos).GameObject.GetComponent<CustomNetTransform>() );
				}
				for ( var i = 0; i < slabs.Count; i++ ) {
					Vector3 vector3 = i%2 == 0 ? new Vector3(i,-i,0) : new Vector3(-i,i,0);
					slabs[i].ForceDrop( spawnPos + vector3/10 );
				}
			}
		}
#if UNITY_EDITOR
		[MenuItem("Networking/Print player positions")]
#endif
		private static void PrintPlayerPositions()
		{
			//For every player in the connected player list (this list is serverside-only)
			foreach (ConnectedPlayer player in PlayerList.Instance.InGamePlayers) {
				//Printing this the pretty way, example:
				//Bob (CAPTAIN) is located at (77,0, 52,0, 0,0)
				Logger.LogFormat( "{0} ({1)} is located at {2}.", Category.DebugConsole, player.Name, player.Job, player.Script.WorldPos );
			}

		}
#if UNITY_EDITOR
		[MenuItem("Networking/Spawn dummy player")]
#endif
		[ConsoleMethod("spawn-dummy", "Spawn dummy player (Server)")]
		private static void SpawnDummyPlayer() {
			PlayerSpawn.ServerSpawnDummy();
		}

#if UNITY_EDITOR
		[MenuItem("Networking/Transform Waltz (Server)")]
		private static void MoveAll()
		{
			CustomNetworkManager.Instance.MoveAll();
		}
#endif

#if UNITY_EDITOR
		[MenuItem("Networking/Gib All (Server)")]
#endif
		[ConsoleMethod("gib-all", "Gib All (Server)")]
		private static void GibAll()
		{
			GibMessage.Send();
		}
#if UNITY_EDITOR
		[MenuItem("Networking/Reset round time")]
#endif
		[ConsoleMethod("reset-time", "Reset round time")]
		private static void ExtendRoundTime()
		{
			GameManager.Instance.ResetRoundTime();
		}
#if UNITY_EDITOR
		[MenuItem("Networking/Kill local player (Server only)")]
#endif
		[ConsoleMethod("suicide", "Kill local player (Server only)")]
		private static void KillLocalPlayer()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				PlayerManager.LocalPlayerScript.playerHealth.ApplyDamageToBodyPart(null, 99999f, AttackType.Internal, DamageType.Brute);
			}
		}
#if UNITY_EDITOR
		[MenuItem("Networking/Respawn local player (Server only)")]
#endif
		[ConsoleMethod("respawn", "Respawn local player (Server only)")]
		private static void RespawnLocalPlayer()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				PlayerSpawn.ServerRespawnPlayer(PlayerManager.LocalPlayerScript.mind);
			}
		}

		private static HashSet<MatrixInfo> usedMatrices = new HashSet<MatrixInfo>();
		private static Tuple<MatrixInfo, Vector3> lastUsedMatrix;
#if UNITY_EDITOR
		[MenuItem("Networking/Crash random matrix into station")]
#endif
		private static void CrashIntoStation()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				StopLastCrashed();

				Vector2 appearPos = new Vector2Int(-50, 37);
				var usedMatricesCount = usedMatrices.Count;

				var matrices = MatrixManager.Instance.MovableMatrices;
				//limit to shuttles if you wish
//					.Where( matrix => matrix.GameObject.name.ToLower().Contains( "shuttle" )
//								   || matrix.GameObject.name.ToLower().Contains( "pod" ) );

				foreach ( var movableMatrix in matrices )
				{
					if ( movableMatrix.GameObject.name.ToLower().Contains( "verylarge" ) )
					{
						continue;
					}

					if ( usedMatrices.Contains( movableMatrix ) )
					{
						continue;
					}

					usedMatrices.Add( movableMatrix );
					lastUsedMatrix = new Tuple<MatrixInfo, Vector3>(movableMatrix, movableMatrix.MatrixMove.ServerState.Position);
					var mm = movableMatrix.MatrixMove;
					mm.SetPosition( appearPos );
					mm.RequiresFuel = false;
					mm.SafetyProtocolsOn = false;
					mm.SteerTo( Orientation.Right );
					mm.SetSpeed( 15 );
					mm.StartMovement();

					break;
				}

				if ( usedMatricesCount == usedMatrices.Count && usedMatricesCount > 0 )
				{ //ran out of unused matrices - doing it again
					usedMatrices.Clear();
					CrashIntoStation();
				}
			}
		}
#if UNITY_EDITOR
		[MenuItem("Networking/Stop last crashed matrix")]
#endif
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
#if UNITY_EDITOR
		[MenuItem("Networking/Make players EVA-ready")]
#endif
		private static void MakeEvaReady()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				foreach ( ConnectedPlayer player in PlayerList.Instance.InGamePlayers )
				{
					foreach (var itemSlot in player.Script.DynamicItemStorage.GetNamedItemSlots(NamedSlot.head))
					{


						var helmet = Spawn.ServerPrefab("MiningHardsuitHelmet").GameObject;
						Inventory.ServerAdd(helmet,itemSlot, ReplacementStrategy.DropOther);
					}

					foreach (var itemSlot in player.Script.DynamicItemStorage.GetNamedItemSlots(NamedSlot.outerwear))
					{
						var suit = Spawn.ServerPrefab("MiningHardsuit").GameObject;
						Inventory.ServerAdd(suit,itemSlot, ReplacementStrategy.DropOther);
					}


					foreach (var itemSlot in player.Script.DynamicItemStorage.GetNamedItemSlots(NamedSlot.mask))
					{
						var mask = Spawn.ServerPrefab(CommonPrefabs.Instance.Mask).GameObject;
						Inventory.ServerAdd(mask,itemSlot, ReplacementStrategy.DropOther);
					}

					foreach (var itemSlot in player.Script.DynamicItemStorage.GetPocketsSlots())
					{
						var oxyTank = Spawn.ServerPrefab(CommonPrefabs.Instance.EmergencyOxygenTank).GameObject;
						Inventory.ServerAdd(oxyTank,itemSlot, ReplacementStrategy.DropOther);
					}

					foreach (var itemSlot in player.Script.DynamicItemStorage.GetNamedItemSlots(NamedSlot.feet))
					{
						var MagBoots = Spawn.ServerPrefab("MagBoots").GameObject;
						Inventory.ServerAdd(MagBoots,itemSlot, ReplacementStrategy.DropOther);
					}

					player.Script.Equipment.IsInternalsEnabled = true;
				}

			}
		}

#if UNITY_EDITOR
		[MenuItem("Networking/Give me some AA!")]
#endif
		private static void MakeAA()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				foreach ( ConnectedPlayer player in PlayerList.Instance.InGamePlayers )
				{
					foreach (var itemSlot in player.Script.DynamicItemStorage.GetNamedItemSlots(NamedSlot.id))
					{
						var ID = Spawn.ServerPrefab("IDCardCaptainsSpare").GameObject;
						Inventory.ServerAdd(ID,itemSlot, ReplacementStrategy.DropOther);
					}
				}
			}
		}

#if UNITY_EDITOR
		[MenuItem("Networking/Give me some goddamn Gloves!")]
#endif
		private static void GiveGloves()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				foreach ( ConnectedPlayer player in PlayerList.Instance.InGamePlayers )
				{
					foreach (var itemSlot in player.Script.DynamicItemStorage.GetNamedItemSlots(NamedSlot.hands))
					{
						var InsulatedGloves = Spawn.ServerPrefab("InsulatedGloves").GameObject;
						Inventory.ServerAdd(InsulatedGloves,itemSlot, ReplacementStrategy.DropOther);
					}
				}

			}
		}

#if UNITY_EDITOR
		[MenuItem("Networking/Incinerate local player")]
#endif
		private static void Incinerate()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				var playerScript = PlayerManager.LocalPlayerScript;
				var matrix = MatrixManager.Get(playerScript.registerTile.Matrix);

				foreach (var worldPos in playerScript.WorldPos.BoundsAround().allPositionsWithin)
				{
					var localPos = MatrixManager.WorldToLocalInt(worldPos, matrix);
					var gasMix = matrix.MetaDataLayer.Get(localPos).GasMix;
					gasMix.AddGas(Gas.Plasma, 100);
					gasMix.AddGas(Gas.Oxygen, 100);
					matrix.ReactionManager.ExposeHotspot(localPos, 500);
				}
			}
		}

#if UNITY_EDITOR
		[MenuItem("Networking/Heal up local player")]
#endif
		private static void HealUp()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				var playerScript = PlayerManager.LocalPlayerScript;
				var health = playerScript.playerHealth;
				health.ResetDamageAll();
				playerScript.registerTile.ServerStandUp();
			}
		}

#if UNITY_EDITOR
		[MenuItem("Networking/Spawn Rods")]
#endif
		private static void SpawnRods()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				Spawn.ServerPrefab("Rods", PlayerManager.LocalPlayerScript.WorldPos + Vector3Int.up, cancelIfImpassable: true);
			}
		}
#if UNITY_EDITOR
		[MenuItem("Networking/Slip Local Player")]
#endif
		private static void SlipPlayer()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				PlayerManager.LocalPlayerScript.registerTile.ServerSlip( true );
			}
		}
		// TODO: Removing this capability at the moment because some antags require an actual spawn (such as
		// syndicate. and can't just be assigned an antag after they've already spawned. If there really is
		// an actual need to be able to do this it will require refactoring GameMode system to support late reassignment.
		// [ConsoleMethod("spawn-antag", "Spawns a random antag. Server only command")]
		// public static void SpawnAntag()
		// {
		// 	if (CustomNetworkManager.Instance._isServer == false)
		// 	{
		// 		Logger.LogError("Can only execute command from server.", Category.DebugConsole);
		// 		return;
		// 	}
		//
		// 	Antagonists.AntagManager.Instance.CreateAntag();
		// }
		[ConsoleMethod("antag-status", "System wide message, reports the status of all antag objectives to ALL players. Server only command")]
		public static void ShowAntagObjectives()
		{
			if (CustomNetworkManager.Instance._isServer == false)
			{
				Logger.Log("Can only execute command from server.", Category.DebugConsole);
				return;
			}

			Antagonists.AntagManager.Instance.ShowAntagStatusReport();
		}

		[ConsoleMethod("antag-remind", "Remind all antags of their own objectives. Server only command")]
		public static void RemindAntagObjectives()
		{
			if (CustomNetworkManager.Instance._isServer == false)
			{
				Logger.Log("Can only execute command from server.", Category.DebugConsole);
				return;
			}

			Antagonists.AntagManager.Instance.RemindAntags();
		}

#if UNITY_EDITOR
		[MenuItem("Networking/Trigger Stranded Ending")]
#endif
		private static void PlayStrandedEnding()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				//blow up the engines to trigger stranded ending for everyone
				var escapeShuttle = GameObject.FindObjectOfType<EscapeShuttle>();
				if (escapeShuttle != null)
				{
					foreach (var thruster in escapeShuttle.GetComponentsInChildren<ShipThruster>())
					{
						thruster.GetComponent<Integrity>().ApplyDamage(99999999, AttackType.Internal, DamageType.Brute);
					}
				}
			}
		}
#if UNITY_EDITOR
		[MenuItem("Networking/Spam chat")]
#endif
		private static void SpamChat()
		{
			if (!Application.isPlaying || !CustomNetworkManager.Instance._isServer)
			{
				return;
			}
			isSpamming = true;
			Chat.Instance.StartCoroutine(SpamChatCoroutine());
			Chat.Instance.StartCoroutine(StopSpam());
		}

		private static IEnumerator StopSpam()
		{
			yield return WaitFor.Seconds(12);
			isSpamming = false;
		}

		private static bool isSpamming = false;

		private static IEnumerator SpamChatCoroutine()
		{
			if (!isSpamming)
			{
				yield break;
			}

			yield return WaitFor.Seconds(Random.Range(0.00001f, 0.01f));
			switch (Random.Range(1,4))
			{
				case 1:
					Chat.AddExamineMsgToClient(DateTime.Now.ToFileTimeUtc().ToString());
					break;
				case 2:
					Chat.AddChatMsgToChat(ConnectedPlayer.Invalid, DateTime.Now.ToFileTimeUtc().ToString(), ChatChannel.OOC, Loudness.NORMAL);
					break;
				default:
					Chat.AddLocalMsgToChat(DateTime.Now.ToFileTimeUtc().ToString(), new Vector2(Random.value*100,Random.value*100), null);
					break;
			}

			Chat.Instance.StartCoroutine(SpamChatCoroutine());
		}


		[ConsoleMethod("add-admin", "Promotes a user to admin using a user's account ID\nUsage: add-admin <account-id>")]
		public static void AddAdmin(string userIDToPromote)
		{
			if (CustomNetworkManager.Instance._isServer == false)
			{
				Logger.Log("Can only execute command from server.", Category.DebugConsole);
				return;
			}

			PlayerList.Instance.ProcessAdminEnableRequest(ServerData.UserID, userIDToPromote);
		}
	}
}
