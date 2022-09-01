using System;
using System.Collections.Generic;
using System.Linq;
using Managers;
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
		[SerializeField] private int negativeModifer = 5;
		[SerializeField] private int positiveModifer = 10;
		[SerializeField] private Occupation captainOccupation;

		public override void Awake()
		{
			base.Awake();
			EventManager.AddHandler(Event.RoundEnded, CalculateScoresAndShow);
			CreateCommonScoreEntries();
		}

		private void CreateCommonScoreEntries()
		{
			ScoreMachine.AddNewScoreEntry("laborPoints", "Total Labor Points", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry("randomEventsTriggered", "Random Events Endured", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry("foodmade", "Meals Prepared", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry("hostileNPCdead", "Hostile NPCs dead", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry("healing", "Healing Done", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry("foodeaten", "Food Eaten", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Weird);
			ScoreMachine.AddNewScoreEntry("explosions", "Number of explosions this round", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Weird);
			ScoreMachine.AddNewScoreEntry("clownBeaten", "Clown Abused", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Bad);
		}

		private void RoundEndChecks()
		{
			//Grab round length and make it a score
			ScoreMachine.AddNewScoreEntry("roundLength", "Shift Length", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddToScoreInt(GameManager.Instance.stationTime.Minute, "roundLength");
			//How many crew members are still on the station?
			ScoreMachine.AddNewScoreEntry("abandonedCrew", "Abandoned Crew", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Bad);
			ScoreMachine.AddToScoreInt(-MatrixManager.MainStationMatrix.Matrix.PresentPlayers.Count * negativeModifer, "abandonedCrew");
			//Is the captain still on his ship during a red alert or higher?
			if (GameManager.Instance.CentComm.CurrentAlertLevel >= CentComm.AlertLevel.Red)
			{
				ScoreMachine.AddNewScoreEntry("captainWithHisShip", "Captain goes down with his ship", ScoreMachine.ScoreType.Bool, ScoreCategory.StationScore, ScoreAlignment.Good);
				ScoreMachine.AddToScoreBool(MatrixManager.MainStationMatrix.Matrix.PresentPlayers.Any(crew =>
					crew.PlayerScript.mind.occupation == captainOccupation), "captainWithHisShip");
			}
			//How many dead crew are there?
			ScoreMachine.AddNewScoreEntry("deadCrew", "Dead Crew", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Bad);
			ScoreMachine.AddToScoreInt(-PlayerList.Instance.AllPlayers.Count(playerbody => playerbody.Script.playerHealth.IsDead) * negativeModifer, "deadCrew");
		}

		private void CalculateScoresAndShow()
		{
			RoundEndChecks();

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