using System.Collections.Generic;
using Systems.Score;
using TMPro;
using UnityEngine;

namespace UI.Systems.EndRound
{
	public class RoundEndScoreScreen : MonoBehaviour
	{
		[SerializeField] private TMP_Text scoreSummary;
		[SerializeField] private TMP_Text pageTitle;

		public System.Action OnPageUpdate;

		public void ShowScore(List<ScoreEntry> entries, int finalScore)
		{

		}
	}
}