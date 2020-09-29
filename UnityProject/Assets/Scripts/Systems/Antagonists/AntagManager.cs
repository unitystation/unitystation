using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;
using DiscordWebhook;
using DatabaseAPI;
using Messages.Server.LocalGuiMessages;

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
		private List<SpawnedAntag> ActiveAntags = new List<SpawnedAntag>();

		/// <summary>
		/// Keeps track of which players have already been targeted for objectives
		/// </summary>
		[NonSerialized] public List<PlayerScript> TargetedPlayers = new List<PlayerScript>();

		/// <summary>
		/// Keeps track of which items have already been targeted for objectives
		/// </summary>
		[NonSerialized] public List<GameObject> TargetedItems = new List<GameObject>();

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
			EventManager.AddHandler(EVENT.RoundEnded, OnRoundEnd);
		}

		void OnDisable()
		{
			EventManager.RemoveHandler(EVENT.RoundEnded, OnRoundEnd);
		}

		void OnRoundEnd()
		{
			ResetAntags();
		}

		/// <summary>
		/// Returns the number of active antags
		/// </summary>
		public int AntagCount => ActiveAntags.Count;

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
			var spawnedPlayer = chosenAntag.ServerSpawn(spawnRequest);

			var connectedPlayer = PlayerList.Instance.Get(spawnedPlayer);

			ServerFinishAntag(chosenAntag, connectedPlayer, spawnedPlayer);
		}

		public void ServerRespawnAsAntag(ConnectedPlayer connectedPlayer, Antagonist antagonist)
		{
			SetAntagDetails(antagonist, connectedPlayer);
			var antagOccupation = antagonist.AntagOccupation;

			if (antagOccupation != null)
			{
				connectedPlayer.Script.mind.occupation = antagonist.AntagOccupation;
			}

			ServerFinishAntag(antagonist, connectedPlayer, connectedPlayer.GameObject);
			PlayerSpawn.ServerRespawnPlayer(connectedPlayer.Script.mind);
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

		private void ServerFinishAntag(Antagonist chosenAntag, ConnectedPlayer connectedPlayer, GameObject spawnedPlayer)
		{
			var spawnedAntag = SetAntagDetails(chosenAntag, connectedPlayer);
			ActiveAntags.Add(spawnedAntag);
			ShowAntagBanner(spawnedPlayer, chosenAntag);

			Logger.Log(
				$"Created new antag. Made {connectedPlayer.Name} a {chosenAntag.AntagName} with objectives:\n{spawnedAntag.GetObjectivesForLog()}",
				Category.Antags);
		}



		/// <summary>
		/// Sends a message to the antag player and tells it to start the antag banner animation.
		/// </summary>
		/// <param name="player">Who</param>
		/// <param name="antag">What antag data</param>
		private static void ShowAntagBanner(GameObject player, Antagonist antag)
		{
			AntagBannerMessage.Send(
				player,
				antag.AntagName,
				antag.SpawnSound,
				antag.TextColor,
				antag.BackgroundColor,
				antag.PlaySound);
		}

		/// <summary>
		/// Remind all antagonists of their objectives.
		/// </summary>
		public void RemindAntags()
		{
			foreach (var activeAntag in ActiveAntags)
			{
				activeAntag.Owner?.ShowObjectives();
			}
		}

		/// <summary>
		/// Show the end of round antag status report with their objectives, grouped by antag type.
		/// </summary>
		public void ShowAntagStatusReport()
		{
			StringBuilder statusSB = new StringBuilder($"<color=white><size=30><b>End of Round Report</b></size></color>\n\n", 200);

			var message = $"End of Round Report on {ServerData.ServerConfig.ServerName}\n";

			if (ActiveAntags.Count > 0)
			{
				// Group all the antags by type and list them together
				foreach (var antagType in ActiveAntags.GroupBy(t => t.GetType()))
				{
					statusSB.AppendLine($"<size=24>The <b>{antagType.Key.Name}s</b> were:\n</size>");
					message += $"The {antagType.Key.Name}s were:\n";
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
				statusSB.AppendLine("<size=24>There were no antagonists!</size>");
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
			ActiveAntags.Clear();
			TargetedPlayers.Clear();
			TargetedItems.Clear();
		}

	}
}