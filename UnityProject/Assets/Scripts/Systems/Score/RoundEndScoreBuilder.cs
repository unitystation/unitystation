using System.Collections.Generic;
using System.Linq;
using Doors;
using Doors.Modules;
using Logs;
using Managers;
using Objects.Construction;
using Shared.Managers;

namespace Systems.Score
{
	/// <summary>
	/// Here is where all common entries are initialised once and where we check for extra stuff when the round ends.
	/// </summary>
	public partial class RoundEndScoreBuilder : SingletonManager<RoundEndScoreBuilder>
	{
		public override void Start()
		{
			base.Start();
			EventManager.AddHandler(Event.RoundEnded, CalculateScoresAndShow);
			EventManager.AddHandler(Event.RoundStarted, CreateCommonScoreEntries);
		}

		public override void OnDestroy()
		{
			EventManager.RemoveHandler(Event.RoundEnded, CalculateScoresAndShow);
			EventManager.RemoveHandler(Event.RoundStarted, CreateCommonScoreEntries);
			base.OnDestroy();
		}
		private void CreateCommonScoreEntries()
		{
			ScoreMachine.AddNewScoreEntry(COMMON_SCORE_LABORPOINTS, "Total Labor Points", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry(COMMON_SCORE_SCIENCEPOINTS, "Total Science Points", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry(COMMON_SCORE_RANDOMEVENTSTRIGGERED, "Random Events Endured", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry(COMMON_SCORE_FOODMADE, "Meals Prepared", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry(COMMON_SCORE_HOSTILENPCDEAD, "Hostile NPCs dead", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry(COMMON_SCORE_HEALING, "Healing Done", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry(COMMON_HUG_SCORE_ENTRY, "Hugs Given", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddNewScoreEntry(COMMON_SCORE_FOODEATEN, "Food Eaten", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Weird);
			ScoreMachine.AddNewScoreEntry(COMMON_TAIL_SCORE_ENTRY, "Tails Pulled", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Weird);
			ScoreMachine.AddNewScoreEntry(COMMON_SCORE_EXPLOSION, "Number of explosions this round", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Weird);
			ScoreMachine.AddNewScoreEntry(COMMON_SCORE_CLOWNABUSE, "Clown Abused", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Bad);
		}

		private void RoundEndChecks()
		{
			//Grab round length and make it a score
			ScoreMachine.AddNewScoreEntry("roundLength", "Shift Length", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddToScoreInt(GameManager.Instance.RoundTime.Minute, "roundLength");
			//How many crew members are still on the station?
			ScoreMachine.AddNewScoreEntry("abandonedCrew", "Abandoned Crew Score", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Bad);
			ScoreMachine.AddToScoreInt(-MatrixManager.MainStationMatrix.Matrix.PresentPlayers.Count * negativeModifer, "abandonedCrew");
			//Is the captain still on his ship during a red alert or higher?
#if UNITY_EDITOR
			ScoreMachine.AddNewScoreEntry("captainWithHisShip", "Captain goes down with his ship", ScoreMachine.ScoreType.Bool, ScoreCategory.StationScore, ScoreAlignment.Good);
			ScoreMachine.AddToScoreBool(MatrixManager.MainStationMatrix.Matrix.PresentPlayers.Any(crew =>
				crew.OrNull()?.PlayerScript.OrNull()?.Mind.OrNull()?.occupation == captainOccupation), "captainWithHisShip");
#else
			if (GameManager.Instance.CentComm.CurrentAlertLevel >= CentComm.AlertLevel.Red)
			{
				ScoreMachine.AddNewScoreEntry("captainWithHisShip", "Captain goes down with his ship", ScoreMachine.ScoreType.Bool, ScoreCategory.StationScore, ScoreAlignment.Good);
				ScoreMachine.AddToScoreBool(MatrixManager.MainStationMatrix.Matrix.PresentPlayers.Any(crew =>
					crew.OrNull()?.PlayerScript.OrNull()?.Mind.OrNull()?.occupation == captainOccupation), "captainWithHisShip");
			}
#endif

			//How many dead crew are there if there are more than two crewmembers?
			if (PlayerList.Instance.NonAntagPlayers.Count > 2)
			{
				ScoreMachine.AddNewScoreEntry("deadCrew", "Dead Crew Score", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Bad);
				//We can get away with using expensive LINQ methods here at round end.
				foreach (var player in PlayerList.Instance.NonAntagPlayers.Where(player => player.Mind != null && player.Script != null))
				{
					if (player.Script.playerHealth.IsDead) ScoreMachine.AddToScoreInt(DEAD_CREW_SCORE * negativeModifer, "deadCrew");
				}
			}
			//Who's the crew member with the worst overall health?
			FindLowestHealthCrew();
			//Are there any electrified doors on the station?
			FindHarmfulDoors();
			CheckMainStationFilth();
		}

		private static void FindLowestHealthCrew()
		{
			var lowestHealthCrewMemberNumber = 200f;
			var lowestHealthCrewMemberName = "";
			foreach (var alivePlayer in PlayerList.Instance.GetAlivePlayers())
			{
				if (alivePlayer.Script == null || alivePlayer.Script.playerHealth == null) continue;
				if (alivePlayer.Script.playerHealth.OverallHealth > HURT_CREW_MINIMUM_SCORE) continue;
				if (alivePlayer.Script.playerHealth.OverallHealth >= lowestHealthCrewMemberNumber) continue;
				lowestHealthCrewMemberNumber = alivePlayer.Script.playerHealth.OverallHealth;
				lowestHealthCrewMemberName = alivePlayer.Script.playerName;
			}
			if (string.IsNullOrEmpty(lowestHealthCrewMemberName)) return;
			var lowestHealthWinner = $"{lowestHealthCrewMemberName} - {lowestHealthCrewMemberNumber}HP";
			ScoreMachine.AddNewScoreEntry("worstBatteredCrewMemeber", "Crewmember with the lowest health", ScoreMachine.ScoreType.String, ScoreCategory.StationScore, ScoreAlignment.Bad);
			ScoreMachine.AddToScoreString(lowestHealthWinner, lowestHealthCrewMemberNumber > 25f ? 100 : -5, "worstBatteredCrewMemeber");
		}

		private static void FindHarmfulDoors()
		{
			var numberOfDoors = 0;
			foreach (var door in MatrixManager.MainStationMatrix.Objects.GetComponentsInChildren<DoorMasterController>())
			{
				foreach (var module in door.ModulesList)
				{
					if (module is ElectrifiedDoorModule { IsElectrified: true }) numberOfDoors++;
				}
			}

			if (numberOfDoors == 0) return;
			ScoreMachine.AddNewScoreEntry(COMMON_DOOR_ELECTRIC_ENTRY, "Doors Electrified",
				ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Weird);
			ScoreMachine.AddToScoreInt(numberOfDoors, COMMON_DOOR_ELECTRIC_ENTRY);
		}

		private void CheckMainStationFilth()
		{
			var dirtyness = 0;
			foreach (var decal in MatrixManager.MainStationMatrix.Objects.GetComponentsInChildren<FloorDecal>())
			{
				if (decal.Cleanable) dirtyness++;
			}

			if (MatrixManager.MainStationMatrix.SubsystemManager
				    .TryGetComponent<FilthGenerator.FilthGenerator>(out var generator) == false)
			{
				Loggy.LogWarning("[RoundEndScoreBuilder] - Cannot find filth generator, skipping..", Category.Round);
				return;
			}
			if (generator.FilthCleanGoal >= dirtyness)
			{
				ScoreMachine.AddNewScoreEntry(FILTH_ENTRY, "Station Filth Score",
					ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Good);
				ScoreMachine.AddToScoreInt(CLEAN_STATION_SCORE, FILTH_ENTRY);
			}
			else
			{
				ScoreMachine.AddNewScoreEntry(FILTH_ENTRY, "Station Filth Score",
					ScoreMachine.ScoreType.Int, ScoreCategory.StationScore, ScoreAlignment.Bad);
				ScoreMachine.AddToScoreInt(DIRTY_STATION_SCORE, FILTH_ENTRY);
			}
		}

		public void CalculateScoresAndShow()
		{
			if(CustomNetworkManager.IsServer == false) return;
			RoundEndChecks();

			List<ScoreEntry> stationScoreEntries = new List<ScoreEntry>();
			List<ScoreEntry> antagScoreEntries = new List<ScoreEntry>();

			var finalStationScore = 0;
			var finalAntagScore = 0;

			foreach (var pair in ScoreMachine.Instance.Scores)
			{
				var entry = pair.Value;
				var score = entry switch
				{
					ScoreEntryInt a => a.Score,
					ScoreEntryBool m => m.Score ? boolScore : -boolScore,
					_ => 0,
				};
				entry.ScoreValue = score;
				switch (entry.Category)
				{
					case ScoreCategory.StationScore:
						stationScoreEntries.Add(entry);
						finalStationScore += score;
						break;
					//TODO: Add Antag Score UI.
					case ScoreCategory.AntagScore:
						antagScoreEntries.Add(entry);
						finalAntagScore += score;
						break;
				}
			}

			UIManager.Instance.ScoreScreen.ShowScore(stationScoreEntries, finalStationScore);
		}
	}
}