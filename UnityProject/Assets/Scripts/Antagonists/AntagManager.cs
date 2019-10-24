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
			// Choose a random non-antag player
			ConnectedPlayer player = chosenPlayer ?? PlayerList.Instance.NonAntagPlayers[Random.Range(0, PlayerList.Instance.InGamePlayers.Count)];
			var antag = chosenAntag ?? AntagData.GetRandomAntag();
			List<Objective> objectives = AntagData.GetRandomObjectives(3, false, antag);
			objectives.Add(AntagData.GetEscapeObjective());
			antag.GiveObjectives(objectives);
			ActiveAntags.Add(antag);
			player.Script.mind.SetAntag(antag);
			Logger.Log($"Created new antag. Made {player.Name} a {antag.AntagName}", Category.Antags);
		}

		/// <summary>
		/// Show the end of round antag status report with their objectives, grouped by antag type.
		/// </summary>
		public void ShowAntagStatusReport()
		{
			StringBuilder statusSB = new StringBuilder($"End of Round Report\n", 200);

			if (ActiveAntags.Count > 0)
			{
				// Group all the antags by type and list them together
				foreach (var antagType in ActiveAntags.GroupBy(t => t.GetType()))
				{
					statusSB.AppendLine($"The {antagType.Key.Name}s were:");
					foreach (var antag in antagType)
					{
						statusSB.AppendLine(antag.GetObjectiveStatus());
					}
				}
			}
			else
			{
				statusSB.AppendLine("There were no antagonists this round!");
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