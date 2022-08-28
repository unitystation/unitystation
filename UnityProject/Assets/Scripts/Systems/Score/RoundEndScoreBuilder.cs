using System;
using System.Collections.Generic;
using Shared.Managers;

namespace Systems.Score
{
	public class RoundEndScoreBuilder : SingletonManager<RoundEndScoreBuilder>
	{
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

			foreach (var entry in ScoreMachine.Instance.Scores)
			{
				if(entry.Value.Category == ScoreCategory.StationScore) stationScoreEntries.Add(entry.Value);
				if(entry.Value.Category == ScoreCategory.AntagScore) antagScoreEntries.Add(entry.Value);
			}

			stationScores = ScoresInString(stationScoreEntries);
			antagScores = ScoresInString(antagScoreEntries);
			//Display UI code here
		}

		private List<String> ScoresInString(List<ScoreEntry> scoreEntries)
		{
			List<String> strings = new List<string>();
			foreach (var entry in scoreEntries)
			{
				var newString = $"{entry.ScoreName} : {GrabScoreTypeResult(entry)}";
				strings.Add(newString);
			}

			return strings;
		}

		private String GrabScoreTypeResult(ScoreEntry entry)
		{
			return entry switch
			{
				ScoreEntryInt i => i.Score.ToString(),
				ScoreEntryBool c => c.Score.ToString(),
				ScoreEntryString u => u.Score,
				_ => null
			};
		}
	}
}