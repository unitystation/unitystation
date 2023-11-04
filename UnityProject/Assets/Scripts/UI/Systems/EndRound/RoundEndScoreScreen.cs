using System;
using System.Collections.Generic;
using System.Text;
using Logs;
using Messages.Server;
using Mirror;
using Systems.Score;
using TMPro;
using UnityEngine;

namespace UI.Systems.EndRound
{
	public class RoundEndScoreScreen : MonoBehaviour
	{
		[SerializeField] private TMP_Text scoreSummary;
		[SerializeField] private TMP_Text scoreResult;
		[SerializeField] private TMP_Text ratingResult;


		public void ShowScore(List<ScoreEntry> entries, int finalScore)
		{
			// NOTE //
			// Right now there is only one page so we can get away with putting all of the code here.
			// In the future when antags and other stuff receive their own page we should move this logic to different components that this class updates
			// Based on the context and content
			// NOTE //
			StringBuilder theGoodList = new StringBuilder();
			theGoodList.AppendLine("<align=\"center\"><i><u><b>The Good:</b></u></i></align>");
			StringBuilder theBadList = new StringBuilder();
			theBadList.AppendLine("<align=\"center\"><i><u><b>The Bad:</b></u></i></align>");
			StringBuilder theWeirdList = new StringBuilder();
			theWeirdList.AppendLine("<align=\"center\"><i><u><b>The Weird:</b></u></i></align>");

			StringBuilder finalResult = new StringBuilder();

			entries.Shuffle(); //Randomize the positions of all entries.

			foreach (ScoreEntry entry in entries)
			{
				if (entry.Alignment == ScoreAlignment.Unspecified || entry.Category == ScoreCategory.MiscScore) continue;
				var result = ScoreMachine.ScoreTypeResultAsString(entry);
				if (result == null)
				{
					Loggy.LogError("[ScoreMachine] - Unidentified score entry type detected while building UI text for round end.");
					continue;
				}
				if (result.ToLower().Contains("true")) result = $"<color=green>Success! ({entry.ScoreValue})</color>";
				if (result.ToLower().Contains("false")) result = $"<color=red>Failed! ({entry.ScoreValue})</color>";
				switch (entry.Alignment)
				{
					case ScoreAlignment.Good:
						theGoodList.AppendLine($"<b>{entry.ScoreName}</b> : {result}");
						break;
					case ScoreAlignment.Bad:
						theBadList.AppendLine($"<b>{entry.ScoreName}</b> : {result}");
						break;
					case ScoreAlignment.Weird:
						theWeirdList.AppendLine($"<b>{entry.ScoreName}</b> : {result}");
						break;
					default:
						Loggy.LogError("[RoundEndScoreScreen/ShowScore] - Undefined Alignment for score entry.");
						break;
				}
			}

			finalResult.Append(theGoodList);
			finalResult.AppendLine(" ");
			finalResult.Append(theBadList);
			finalResult.AppendLine(" ");
			finalResult.Append(theWeirdList);
			finalResult.AppendLine(" ");

			scoreSummary.text = finalResult.ToString();
			scoreResult.text = finalScore.ToString();
			this.SetActive(true);
			ServerShowUIToClients.NetMessage msg = new ServerShowUIToClients.NetMessage();
			msg.ScoreResult = scoreResult.text;
			msg.ScoreSummary = scoreSummary.text;
			ratingResult.text = RatePerformance(finalScore);
			ServerShowUIToClients.SendToAll(msg);
		}

		public void SyncScore(string finalResult, string finalScore, string finalRating)
		{
			scoreSummary.text = finalResult;
			scoreResult.text = finalScore;
			ratingResult.text = finalRating;
			this.SetActive(true);
		}

		private string RatePerformance(int finalScore)
		{
			if (finalScore.IsBetween(int.MinValue, -1500)) return "Expunge from records";
			if (finalScore.IsBetween(-1500, -1000)) return "Singularity Fodder";
			if (finalScore.IsBetween(-1000, -500)) return "Clown Station";
			if (finalScore.IsBetween(-500, 0)) return "Disaster";
			if (finalScore.IsBetween(0, 500)) return "Decent Shift";
			if (finalScore.IsBetween(500, 1000)) return "Net Profit";
			return finalScore.IsBetween(1000, int.MaxValue) ? "Robust Station" : "N/A";
		}
	}

	public class ServerShowUIToClients : ServerMessage<ServerShowUIToClients.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string ScoreSummary;
			public string ScoreResult;
			public string ScoreRating;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.ScoreScreen.SyncScore(msg.ScoreSummary, msg.ScoreResult, msg.ScoreRating);
		}
	}
}
