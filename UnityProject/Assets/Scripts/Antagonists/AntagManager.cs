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

		/// <summary>
		/// Creates a new antagonist, defaults to a random player with a random antag type if no arguments passed.
		/// </summary>
		/// <param name="chosenAntag">The antag type to spawn</param>
		/// <param name="chosenPlayer">The player to make an antag</param>
		public void CreateAntag(Antagonist chosenAntag = null, ConnectedPlayer chosenPlayer = null)
		{
			// Choose a random non-antag player if one hasn't been provided
			ConnectedPlayer player = chosenPlayer;
			if (chosenAntag == null)
			{
				if (PlayerList.Instance.NonAntagPlayers.Count == 0)
				{
					Logger.LogWarning("Unable to create new antag: No suitable candidates left.");
					return;
				}
				int randIndex = Random.Range(0, PlayerList.Instance.NonAntagPlayers.Count);
				player = PlayerList.Instance.NonAntagPlayers[randIndex];
			}
			// Choose a random antag type if one hasn't been provided
			var antag = chosenAntag ?? AntagData.GetRandomAntag();

			// Give the antag some objectives and one escape objective
			List<Objective> objectives = AntagData.GetRandomObjectives(3, false, antag);
			objectives.Add(AntagData.GetEscapeObjective());
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
		/// Clears all active antagonists
		/// </summary>
		public void ResetAntags()
		{
			ActiveAntags.Clear();
		}
	}
}