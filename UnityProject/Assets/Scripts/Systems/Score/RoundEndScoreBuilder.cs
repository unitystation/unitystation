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


		private void CreateCommonScoreEntries()
		{
			ScoreMachine.AddNewScoreEntry("laborPoints", "Total Labor Points", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry("randomEventsTriggered", "Random Events Endured", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry("foodmade", "Meals Prepared", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry("roundLength", "Shift Length", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry("hostileNPCdead", "Hostile NPCs dead", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry("healing", "Healing Done", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry("foodeaten", "Food Eaten", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Weird);
			ScoreMachine.AddNewScoreEntry("happypill", "Happy Pills Ingested", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Weird);
			ScoreMachine.AddNewScoreEntry("clownBeaten", "Clown Abused", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Bad);
			ScoreMachine.AddNewScoreEntry("abandonedCrew", "Abandoned Crew", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Bad);
			ScoreMachine.AddNewScoreEntry("deadCrew", "Dead Crew", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Bad);
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
	}
}