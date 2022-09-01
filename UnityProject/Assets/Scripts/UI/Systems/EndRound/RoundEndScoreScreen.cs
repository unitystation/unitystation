using System;
using System.Collections.Generic;
using System.Text;
using Systems.Score;
using TMPro;
using UnityEngine;

namespace UI.Systems.EndRound
{
	public class RoundEndScoreScreen : MonoBehaviour
	{
		[SerializeField] private TMP_Text scoreSummary;
		[SerializeField] private TMP_Text scoreResult;
		[SerializeField] private int NumberOfScoresToShow = 5;


		public void ShowScore(List<ScoreEntry> entries, int finalScore)
		{
			// NOTE //
			// Right now there is only one page so we can get away with putting all of the code here.
			// In the future when antags and other stuff receive their own page we should move this logic to different components that this class updates
			// Based on the context and content
			// NOTE //
			StringBuilder theGoodList = new StringBuilder();
			theGoodList.AppendLine("The Good:");
			StringBuilder theBadList = new StringBuilder();
			theBadList.AppendLine("The Bad:");
			StringBuilder theWeirdList = new StringBuilder();
			theWeirdList.AppendLine("The Weird");

			StringBuilder finalResult = new StringBuilder();

			entries.Shuffle(); //Randomize the positions of all entries.
			var numberOfGoodEntriesFound = 0;
			var numberOfBadEntriesFound = 0;
			var numberOfWeirdEntriesFound = 0;


			foreach (var Entry in entries)
			{
				if (Entry.Alignment == ScoreAlignment.Unspecified || Entry.Category == ScoreCategory.MiscScore) continue;
				var result = ScoreMachine.ScoreTypeResultAsString(Entry);
				if (result == "true") result = "<color=green> Success! </color>";
				if (result == "false") result = "<color=red> Failed! </color>";
				switch (Entry.Alignment)
				{
					case ScoreAlignment.Good:
						if(numberOfGoodEntriesFound >= NumberOfScoresToShow) break;
						theGoodList.AppendLine($"{Entry.ScoreName} :{result}");
						numberOfGoodEntriesFound++;
						break;
					case ScoreAlignment.Bad:
						if(numberOfBadEntriesFound >= NumberOfScoresToShow) break;
						theBadList.AppendLine($"{Entry.ScoreName} :{result}");
						numberOfBadEntriesFound++;
						break;
					case ScoreAlignment.Weird:
						if(numberOfWeirdEntriesFound >= NumberOfScoresToShow) break;
						theWeirdList.AppendLine($"{Entry.ScoreName} :{result}");
						numberOfWeirdEntriesFound++;
						break;
				}

				finalResult.Append(theGoodList);
				finalResult.Append(theBadList);
				finalResult.Append(theWeirdList);

				scoreSummary.text = finalResult.ToString();
				scoreResult.text = finalScore.ToString();

				this.SetActive(true);
			}
		}
	}
}