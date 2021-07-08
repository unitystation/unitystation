using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;
using DiscordWebhook;
using DatabaseAPI;
using Messages.Server.LocalGuiMessages;
using Objects.Command;
using UnityEngine.SceneManagement;

namespace Antagonists
{
	public class AntagManager : MonoBehaviour
	{
		/// <summary>
		/// The main static instance of this manager, use it for all operations
		/// </summary>
		public static AntagManager Instance;

		[SerializeField] [Tooltip("Stores all antag and objective data.")]
		private AntagData antagData = null;

		/// <summary>
		/// All active antagonists
		/// </summary>
		private List<SpawnedAntag> activeAntags = new List<SpawnedAntag>();

		public List<SpawnedAntag> ActiveAntags => activeAntags;

		/// <summary>
		/// Keeps track of which players have already been targeted for objectives
		/// </summary>
		[NonSerialized] public List<PlayerScript> TargetedPlayers = new List<PlayerScript>();

		/// <summary>
		/// Keeps track of which items have already been targeted for objectives
		/// </summary>
		[NonSerialized] public List<GameObject> TargetedItems = new List<GameObject>();

		public static int SyndiNukeCode;

		public GameObject blobPlayerViewer = null;

		private void Awake()
		{
			if ( Instance == null )
			{
				Instance = this;
			}
			else
			{
				Destroy(gameObject);
			}
		}

		void OnEnable()
		{
			SceneManager.activeSceneChanged += OnSceneChange;
			EventManager.AddHandler(Event.RoundEnded, OnRoundEnd);
		}

		void OnDisable()
		{
			SceneManager.activeSceneChanged -= OnSceneChange;
			EventManager.RemoveHandler(Event.RoundEnded, OnRoundEnd);
		}

		void OnSceneChange(Scene oldScene, Scene newScene)
		{
			SyndiNukeCode = Nuke.CodeGenerator();
		}

		void OnRoundEnd()
		{
			ResetAntags();
		}

		/// <summary>
		/// Returns the number of active antags
		/// </summary>
		public int AntagCount => activeAntags.Count;

		/// <summary>
		/// Server only. Spawn the joined viewer as the indicated antag, includes creating their player object
		/// and transferring them to it. This is used as an alternative
		/// to PlayerSpawn.ServerSpawnPlayer when an antag should be spawned.
		/// </summary>
		/// <param name="chosenAntag">antag to spawn as</param>
		/// <param name="spawnRequest">player's requested spawn</param>
		public void ServerSpawnAntag(Antagonist chosenAntag, PlayerSpawnRequest spawnRequest)
		{
			//spawn the antag using their custom spawn logic
			ConnectedPlayer spawnedPlayer = chosenAntag.ServerSpawn(spawnRequest).Player();

			ServerFinishAntag(chosenAntag, spawnedPlayer);
		}

		public IEnumerator ServerRespawnAsAntag(ConnectedPlayer connectedPlayer, Antagonist antagonist)
		{
			var antagOccupation = antagonist.AntagOccupation;

			if (antagOccupation != null)
			{
				connectedPlayer.Script.mind.occupation = antagonist.AntagOccupation;
			}

			//Can be null if respawning spectator ghost as they dont have an occupation and their antag occupation is null too
			if (connectedPlayer.Script.mind.occupation == null)
			{
				yield break;
			}

			if (antagonist.AntagJobType == JobType.SYNDICATE)
			{
				yield return StartCoroutine(SubSceneManager.Instance.LoadSyndicate());
				yield return WaitFor.EndOfFrame;
			}

			if (antagonist.AntagJobType == JobType.WIZARD)
			{
				yield return StartCoroutine(SubSceneManager.Instance.LoadWizard());
				yield return WaitFor.EndOfFrame;
			}

			PlayerSpawn.ServerRespawnPlayer(connectedPlayer.Script.mind);
			ServerFinishAntag(antagonist, connectedPlayer);
		}

		private SpawnedAntag SetAntagDetails(Antagonist chosenAntag, ConnectedPlayer connectedPlayer)
		{
			// Generate objectives for this antag
			List<Objective> objectives = antagData.GenerateObjectives(connectedPlayer.Script, chosenAntag);
			// Set the antag
			var spawnedAntag = SpawnedAntag.Create(chosenAntag, connectedPlayer.Script.mind, objectives);
			connectedPlayer.Script.mind.SetAntag(spawnedAntag);
			return spawnedAntag;
		}

		public void ServerFinishAntag(Antagonist chosenAntag, ConnectedPlayer connectedPlayer)
		{
			var spawnedAntag = SetAntagDetails(chosenAntag, connectedPlayer);
			activeAntags.Add(spawnedAntag);
			ShowAntagBanner(connectedPlayer, chosenAntag);
			chosenAntag.AfterSpawn(connectedPlayer);

			Logger.Log(
				$"Created new antag. Made {connectedPlayer.Name} a {chosenAntag.AntagName} with objectives:\n{spawnedAntag.GetObjectivesForLog()}",
				Category.Antags);
		}

		/// <summary>
		/// Searches for the first PDA on the given player and installs an uplink.
		/// </summary>
		/// <param name="player">The player that should receive an uplink in the first PDA found on them.</param>
		/// <param name="tcCount">The amount of telecrystals the uplink should be given.</param>
		public static void TryInstallPDAUplink(ConnectedPlayer player, int tcCount, bool isNukeOps)
		{
			foreach (ItemSlot slot in player.Script.DynamicItemStorage.GetItemSlotTree())
			{
				if (slot.IsEmpty) continue;
				if (slot.Item.TryGetComponent<Items.PDA.PDALogic>(out var pda))
				{
					pda.InstallUplink(player, tcCount, isNukeOps);
				}
			}
		}

		/// <summary>
		/// Sends a message to the antag player and tells it to start the antag banner animation.
		/// </summary>
		/// <param name="player">Who</param>
		/// <param name="antag">What antag data</param>
		private static void ShowAntagBanner(ConnectedPlayer player, Antagonist antag)
		{
			SpawnBannerMessage.Send(
				player.GameObject,
				antag.AntagName,
				antag.SpawnSound.AssetAddress,
				antag.TextColor,
				antag.BackgroundColor,
				antag.PlaySound);
		}

		/// <summary>
		/// Remind all antagonists of their objectives.
		/// </summary>
		public void RemindAntags()
		{
			foreach (var activeAntag in activeAntags)
			{
				activeAntag.Owner?.ShowObjectives();
			}
		}

		/// <summary>
		/// Show the end of round antag status report with their objectives, grouped by antag type.
		/// </summary>
		public void ShowAntagStatusReport()
		{
			StringBuilder statusSB = new StringBuilder();

			var message = $"";

			if (activeAntags.Count > 0)
			{
				// Group all the antags by type and list them together
				foreach (var antagType in activeAntags.GroupBy(t => t.GetType()))
				{
					statusSB.AppendLine($"<size=48>The <b>{antagType.First().Antagonist.AntagName}s</b> were:\n</size>");
					message += $"The {antagType.First().Antagonist.AntagName}s were:\n";
					foreach (var antag in antagType)
					{
						message += $"\n{antag.GetObjectiveStatusNonRich()}\n";
						statusSB.AppendLine(antag.GetObjectiveStatus());
					}
				}
			}
			else
			{
				message += $"\nThere were no antagonists!\n";
				statusSB.AppendLine("<size=48>There were no antagonists!</size>");
			}

			if (PlayerList.Instance.ConnectionCount == 1)
			{
				message += $"\n There is 1 player online.\n";
			}
			else
			{
				message += $"\n There are {PlayerList.Instance.ConnectionCount} players online.\n";
			}

			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAnnouncementURL, message, "");

			// Send the message
			Chat.AddGameWideSystemMsgToChat(statusSB.ToString());
		}

		/// <summary>
		/// Clears all active antagonists and targets
		/// </summary>
		public void ResetAntags()
		{
			activeAntags.Clear();
			TargetedPlayers.Clear();
			TargetedItems.Clear();
		}

	}
}
