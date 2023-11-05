using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text;
using System.Linq;
using DiscordWebhook;
using Logs;
using Messages.Server.LocalGuiMessages;
using Objects.Command;
using Strings;
using Player;
using Systems.Character;

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

		private readonly Dictionary<uint, Team> teams = new Dictionary<uint, Team>();
		public Dictionary<uint, Team> Teams => new (teams);

		private static uint teamIndex = 0;

		/// <summary>
		/// Keeps track of which players have already been targeted for objectives
		/// </summary>
		[NonSerialized] public List<Mind> TargetedPlayers = new List<Mind>();

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

		private void OnEnable()
		{
			EventManager.AddHandler(Event.ReadyToInitialiseMatrices, UpdateSyndiNukeCode);
			EventManager.AddHandler(Event.RoundEnded, OnRoundEnd);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.ReadyToInitialiseMatrices, UpdateSyndiNukeCode);
			EventManager.RemoveHandler(Event.RoundEnded, OnRoundEnd);
		}

		private void UpdateSyndiNukeCode()
		{
			SyndiNukeCode = Nuke.CodeGenerator();
		}

		private void OnRoundEnd()
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
			Mind SpawnMind = chosenAntag.ServerSpawn(spawnRequest);

			ServerFinishAntag(chosenAntag, SpawnMind);
		}

		public IEnumerator ServerRespawnAsAntag(PlayerInfo connectedPlayer, Antagonist antagonist)
		{
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

			Occupation Occupation = connectedPlayer.Mind.occupation;
			CharacterSheet CharacterSettings = connectedPlayer.Mind.CurrentCharacterSettings;

			if(antagonist.RandomizeCharacterForGhostRole == true)
			{
				CharacterSettings = CharacterSheet.GenerateRandomCharacter();
			}

			if (antagonist.AntagOccupation != null) // means it as a modifier such as a traitor Traitor CMO, traitor assistant for example
			{
				Occupation = antagonist.AntagOccupation;
			}
			else if(antagonist.GhostRoleOccupation != null)
			{
				Occupation = antagonist.GhostRoleOccupation;
			}
			var AntagonistsMind  = PlayerSpawn.NewSpawnPlayerV2(connectedPlayer,Occupation, CharacterSettings);
			ServerFinishAntag(antagonist, AntagonistsMind);
		}

		private SpawnedAntag SetAntagDetails(Antagonist chosenAntag, Mind Mind )
		{
			try
			{
				// Generate objectives for this antag
				List<Objective> objectives = new List<Objective>();

				try
				{
					objectives.AddRange(antagData.GenerateObjectives(Mind, chosenAntag));
				} catch (Exception e)
				{
					Loggy.LogError($"failed to create antagonist objectives {chosenAntag.OrNull()?.AntagName} " + e.ToString());
				}
				// Set the antag
				return Mind.InitAntag(chosenAntag, objectives);
			}
			catch (Exception e)
			{
				Loggy.LogError( $"failed to create antagonist {chosenAntag.OrNull()?.AntagName} "  + e.ToString());
				return null;
			}

		}

		private void SetTeamDetails(Team team)
		{
			try
			{
				// Generate objectives
				List<Objective> objectives = new List<Objective>();

				try
				{
					objectives.AddRange(team.Data.GenerateObjectives());
					team.AddTeamObjectives(objectives);
				} catch (Exception e)
				{
					Loggy.LogError($"failed to create team objectives {team.GetTeamName()} " + e.ToString());
				}
			}
			catch (Exception e)
			{
				Loggy.LogError( $"failed to create team {team.GetTeamName()} "  + e.ToString());
			}

		}

		public void ServerFinishAntag(Antagonist chosenAntag, Mind SpawnMind )
		{
			var spawnedAntag = SetAntagDetails(chosenAntag, SpawnMind);
			if (spawnedAntag == null) return;

			activeAntags.Add(spawnedAntag);
			ShowAntagBanner(SpawnMind, chosenAntag);

			SetTeamAntag(SpawnMind, chosenAntag);
			SpawnMind.ShowObjectives();
			chosenAntag.AfterSpawn(SpawnMind);

			Loggy.Log(
				$"Created new antag. Made {SpawnMind.name} a {chosenAntag.AntagName} with objectives:\n{spawnedAntag.GetObjectivesForLog()}",
				Category.Antags);
		}

		private void SetTeamAntag(Mind spawnMind, Antagonist chosenAntag)
		{
			if (chosenAntag.Team != null && chosenAntag.AddTeamAtInit)
				spawnMind.AntagPublic.CurTeam = GetFirstTeamOrCreate(chosenAntag.Team);
		}

		/// <summary>
		/// Searches for the first PDA on the given player and installs an uplink.
		/// </summary>
		/// <param name="player">The player that should receive an uplink in the first PDA found on them.</param>
		/// <param name="tcCount">The amount of telecrystals the uplink should be given.</param>
		public static void TryInstallPDAUplink(Mind player, int tcCount, bool isNukeOps)
		{
			foreach (ItemSlot slot in player.Body.DynamicItemStorage.GetItemSlotTree())
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
		private static void ShowAntagBanner(Mind SpawnMind, Antagonist antag)
		{
			SpawnBannerMessage.Send(
				SpawnMind.gameObject,
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

			var message = new StringBuilder();

			if (activeAntags.Count > 0)
			{
				// Group all the antags by type and list them together
				foreach (var antagType in activeAntags.GroupBy(t => t.GetType()))
				{
					if (antagType.First().CurTeam != null && antagType.First().CurTeam.Data.NeedToBeShownAtRoundEnd == true)
						continue;
					statusSB.AppendLine($"<size={ChatTemplates.LargeText}>The <b>{antagType.First().Antagonist.AntagName}s</b> were:\n</size>");
					message.Append($"The {antagType.First().Antagonist.AntagName}s were:\n");
					foreach (var antag in antagType)
					{
						message.AppendLine($"{antag.GetObjectiveStatusNonRich()}\n");
						statusSB.AppendLine(antag.GetObjectiveStatus());
					}
				}
			}
			else
			{
				message.AppendLine($"There were no antagonists!\n");
				statusSB.AppendLine($"<size={ChatTemplates.LargeText}>There were no antagonists!</size>");
			}

			foreach (var x in PlayerList.Instance.AllPlayers)
			{
				if (x.Mind.AntagPublic.Antagonist == null && x.Mind.AntagPublic.CurTeam == null && x.Mind.AntagPublic.Objectives.Count() > 0)
				{
					statusSB.AppendLine($"<size={ChatTemplates.LargeText}>The <b>{x.Name} objectives</b> were:\n</size>");
					message.AppendLine($"\n{x.Mind.AntagPublic.GetObjectiveStatusNonRich()}\n");
					statusSB.AppendLine(x.Mind.AntagPublic.GetObjectiveStatus());
				}
			}

			foreach (var x in teams)
			{
				if (x.Value.TeamMembers.Count > 0 && x.Value.Data.NeedToBeShownAtRoundEnd == true)
				{
					message.AppendLine(x.Value.GetObjectiveStatusNonRich());
					statusSB.AppendLine(x.Value.GetObjectiveStatus());
				}
			}

			if (PlayerList.Instance.ConnectionCount == 1)
			{
				message.AppendLine($" There is 1 player online.\n");
			}
			else
			{
				message.AppendLine($" There are {PlayerList.Instance.ConnectionCount} players online.\n");
			}

			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAnnouncementURL, message.ToString(), "");

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
			teams.Clear();
		}

		public void RemoveTeam(uint index)
		{
			teams.Remove(index);
		}

		public Team AddTeam(Team teamToAdd)
		{
			teams.Add(++teamIndex, teamToAdd);
			return teamToAdd;
		}

		public Team CreateTeam(TeamData teamToAdd)
		{
			var createdTeam = Team.CreateTeam(teamToAdd);
			teams.Add(++teamIndex, createdTeam);
			SetTeamDetails(createdTeam);
			return createdTeam;
		}

		public List<Team> GetTeamsByTeamData(TeamData teamToGet)
		{
			return teams.Values.Where(t => t.Data == teamToGet).ToList();
		}

		public Team GetFirstTeamOrCreate(TeamData teamToGet)
		{
			var teamsWithData = GetTeamsByTeamData(teamToGet);
			if (teamsWithData.Count > 0)
			{
				return teamsWithData.First();
			} else
			{
				return CreateTeam(teamToGet);
			}
		}

		public void ServerSpawnTeams()
		{
			foreach (var x in AntagData.Instance.TeamDatas)
			{
				if (x.AlwaysSpawnOnRoundStart)
				{
					CreateTeam(x);
				}
			}
		}
	}
}
