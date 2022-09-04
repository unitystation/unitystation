using System;
using System.Collections.Generic;
using System.Linq;
using Managers;
using NSubstitute.ReceivedExtensions;
using Shared.Managers;
using UnityEngine;

namespace Systems.Score
{
	public partial class RoundEndScoreBuilder : SingletonManager<RoundEndScoreBuilder>
	{
		public override void Start()
		{
			base.Start();
			EventManager.AddHandler(Event.RoundEnded, CalculateScoresAndShow);
			EventManager.AddHandler(Event.RoundStarted, CreateCommonScoreEntries);
		}

		private void CreateCommonScoreEntries()
		{
			ScoreMachine.AddNewScoreEntry(COMMON_SCORE_LABORPOINTS, "Total Labor Points", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry(COMMON_SCORE_RANDOMEVENTSTRIGGERED, "Random Events Endured", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry(COMMON_SCORE_FOODMADE, "Meals Prepared", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry(COMMON_SCORE_HOSTILENPCDEAD, "Hostile NPCs dead", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry(COMMON_SCORE_HEALING, "Healing Done", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry(COMMON_SCORE_FOODEATEN, "Food Eaten", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Weird);
			ScoreMachine.AddNewScoreEntry(COMMON_SCORE_EXPLOSION, "Number of explosions this round", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Weird);
			ScoreMachine.AddNewScoreEntry(COMMON_SCORE_CLOWNABUSE, "Clown Abused", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Bad);
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
			//Who's the crew member with the worst overall health?
			var lowestHealthCrewMemberNumber = 200f;
			var lowestHealthCrewMemberName = "";
			foreach (var alivePlayer in PlayerList.Instance.GetAlivePlayers())
			{
				if (alivePlayer.Script.playerHealth.OverallHealth >= lowestHealthCrewMemberNumber) continue;
				lowestHealthCrewMemberNumber = alivePlayer.Script.playerHealth.OverallHealth;
				lowestHealthCrewMemberName = alivePlayer.Script.playerName;
			}
			var lowestHealthWinner = $"{lowestHealthCrewMemberName} - {lowestHealthCrewMemberNumber}HP";
			ScoreMachine.AddNewScoreEntry("worstBatteredCrewMemeber", "Crewmember with the lowest health", ScoreMachine.ScoreType.String, ScoreCategory.StationScore, ScoreAlignment.Bad);
			ScoreMachine.AddToScoreString(lowestHealthWinner, "worstBatteredCrewMemeber");
		}

		public void CalculateScoresAndShow()
		{
			if(CustomNetworkManager.IsServer == false) return;
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