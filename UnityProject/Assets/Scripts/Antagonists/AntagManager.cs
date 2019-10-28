using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;

namespace Antagonists
{
	public class AntagManager : MonoBehaviour
	{
		/// <summary>
		/// The main static instance of this manager, use it for all operations
		/// </summary>
		public static AntagManager Instance;

		/// <summary>
		/// Stores all antag and objective data.
		/// </summary>
		[SerializeField]
		private AntagData AntagData = null;

		/// <summary>
		/// All active antagonists
		/// </summary>
		private List<Antagonist> ActiveAntags = new List<Antagonist>();

		/// <summary>
		/// Keeps track of which players have already been targeted for objectives
		/// </summary>
		public List<PlayerScript> TargetedPlayers = new List<PlayerScript>();

		/// <summary>
		/// Keeps track of which items have already been targeted for objectives
		/// </summary>
		public List<GameObject> TargetedItems = new List<GameObject>();

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
		/// Creates a new antagonist, defaults to a random player with a random antag type if null is passed.
		/// </summary>
		/// <param name="chosenAntag">The antag type to spawn</param>
		/// <param name="chosenPlayer">The player to make an antag</param>
		public void CreateAntag(Antagonist chosenAntag = null, ConnectedPlayer chosenPlayer = null)
		{
			// Choose a random non-antag player if one hasn't been provided
			ConnectedPlayer player = chosenPlayer;
			if (player == null)
			{
				if (PlayerList.Instance.NonAntagPlayers.Count == 0)
				{
					Logger.LogWarning("Unable to create new antag: No suitable candidates left.");
					return;
				}
				player = PlayerList.Instance.NonAntagPlayers.PickRandom();
			}
			// Choose a random antag type if one hasn't been provided
			Antagonist antag = chosenAntag ?? AntagData.GetRandomAntag();

			// Generate objectives for this antag
			List<Objective> objectives = AntagData.GenerateObjectives(player.Script, antag, 2);
			antag.GiveObjectives(objectives);

			// Set the antag
			player.Script.mind.SetAntag(antag);
			ActiveAntags.Add(antag);
			Logger.Log($"Created new antag. Made {player.Name} a {antag.AntagName} with objectives:\n{antag.GetObjectivesForLog()}", Category.Antags);
		}

		/// <summary>
		/// Show the end of round antag status report with their objectives, grouped by antag type.
		/// </summary>
		public void ShowAntagStatusReport()
		{
			StringBuilder statusSB = new StringBuilder($"<color=white><size=30><b>End of Round Report</b></size></color>\n\n", 200);

			if (ActiveAntags.Count > 0)
			{
				// Group all the antags by type and list them together
				foreach (var antagType in ActiveAntags.GroupBy(t => t.GetType()))
				{
					statusSB.AppendLine($"<size=24>The <b>{antagType.Key.Name}s</b> were:\n</size>");
					foreach (var antag in antagType)
					{
						statusSB.AppendLine(antag.GetObjectiveStatus());
					}
				}
			}
			else
			{
				statusSB.AppendLine("<size=24>There were no antagonists!</size>");
			}

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