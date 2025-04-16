using System;
using Logs;
using Mirror;
using TMPro;
using UnityEngine;

namespace UI.Systems.PreRound
{
	public class PreRoundCountdownDisplay : MonoBehaviour
	{
		[SerializeField] private TMP_Text countdownText = null;

		public bool doCountdown;
		private double countdownEndTime;

		public System.Action OnFinishedCountingDown;
		public bool IsCountingDown { get; private set; } = false;

		private void OnEnable()
		{
			EventManager.AddHandler(Event.PostRoundStarted, OnCountdownEnd);
			UpdateManager.Add(UpdateCountdownText, 1f);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.PostRoundStarted, OnCountdownEnd);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateCountdownText);
		}

		public void UpdateCountdownText()
		{
			if (countdownText == null)
			{
				Debug.LogError("Countdown text is null");
				return;
			}
			if (gameObject.activeSelf == false)
			{
				gameObject.SetActive(true);
			}
			countdownText.text = TimeSpan.FromSeconds(countdownEndTime - NetworkTime.time).ToString(@"mm\:ss");
		}

		public void SyncCountdown(bool started, double endTime)
		{
			Loggy.Info().Format("SyncCountdown called with: started={0}, endTime={1}, current NetworkTime={2}",
				Category.Round,
				started, endTime, NetworkTime.time);
			countdownEndTime = endTime;
			doCountdown = started;
			if (countdownEndTime > NetworkTime.time)
			{
				IsCountingDown = true;
				UpdateCountdownText();
			}
			else
			{
				OnCountdownEnd();
			}
		}

		private void OnCountdownEnd()
		{
			gameObject.SetActive(false);
			IsCountingDown = false;
			OnFinishedCountingDown?.Invoke();
		}
	}
}