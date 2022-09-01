using System;
using System.Collections.Generic;
using NSubstitute.ReceivedExtensions;
using Shared.Managers;
using UnityEngine;

namespace Systems.Score
{
	public class RoundEndScoreBuilder : SingletonManager<RoundEndScoreBuilder>
	{
		/// <summary>
		/// How much does score entry that returns true or false score?
		/// </summary>
		[SerializeField] private int boolScore = 10;

		public override void Awake()
		{
			base.Awake();
			EventManager.AddHandler(Event.RoundEnded, CalculateScoresAndShow);
		}

		private void CalculateScoresAndShow()
		{

			List<String> stationScores = new List<String>();
			List<String> antagScores = new List<String>();

			List<ScoreEntry> stationScoreEntries = new List<ScoreEntry>();
			List<ScoreEntry> antagScoreEntries = new List<ScoreEntry>();

			var finalStationScore = 0;
			var finalAntagScore = 0;

			foreach (var entry in ScoreMachine.Instance.Scores)
			{
				if (entry.Value.Category == ScoreCategory.StationScore)
				{
					stationScoreEntries.Add(entry.Value);
					if (entry.Value is ScoreEntryInt a) finalStationScore += a.Score;
					if (entry.Value is ScoreEntryBool m) finalStationScore += m.Score ? boolScore : -boolScore;
				}

				if (entry.Value.Category == ScoreCategory.AntagScore)
				{
					antagScoreEntries.Add(entry.Value);
					if (entry.Value is ScoreEntryInt o) finalAntagScore += o.Score;
					if (entry.Value is ScoreEntryBool g) finalAntagScore += g.Score ? boolScore : -boolScore;
				}
			}
			
			UIManager.Instance.ScoreScreen.ShowScore(stationScoreEntries, finalStationScore);
		}

		public String ScoreTypeResultAsString(ScoreEntry entry)
		{
			return entry switch
			{
				ScoreEntryInt integerScore => integerScore.Score.ToString(),
				ScoreEntryBool booleanScore => booleanScore.Score.ToString(),
				ScoreEntryString stringScore => stringScore.Score,
				_ => null
			};
		}
	}
}