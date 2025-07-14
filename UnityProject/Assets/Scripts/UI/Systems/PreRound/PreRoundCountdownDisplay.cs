using System;
using Logs;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Systems.PreRound
{
	public class PreRoundCountdownDisplay : MonoBehaviour
	{
		[SerializeField] private TMP_Text countdownText = null;
		[SerializeField] private TMP_Text statusText = null;

		private enum CountdownState
		{
			Inactive,
			CountingDown,
			Finished
		}

		private CountdownState currentState = CountdownState.Inactive;

		public bool doCountdown;
		private double countdownEndTime;
		public UnityEvent OnFinishedCountingDown;

		public bool IsCountingDown => currentState == CountdownState.CountingDown;
		private bool hasTriggeredCountdownEnd = false;

		private void OnEnable()
		{
			hasTriggeredCountdownEnd = true;
			EventManager.AddHandler(Event.PostRoundStarted, HandlePostRoundStarted);
			UpdateManager.Add(UpdateCountdownState, 1f);
		}

		private void OnDisable()
		{
			OnFinishedCountingDown?.Invoke();
			EventManager.RemoveHandler(Event.PostRoundStarted, HandlePostRoundStarted);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateCountdownState);
		}

		private void HandlePostRoundStarted()
		{
			currentState = CountdownState.Finished;
			OnFinishedCountingDown?.Invoke();
		}

		private void UpdateCountdownText(double seconds, string label)
		{
			statusText.text = label;
			countdownText.text = TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss");
		}

		public void SyncCountdown(bool started, double endTime)
		{
			Loggy.Info().Format("SyncCountdown called with: started={0}, endTime={1}, current NetworkTime={2}",
				Category.Round,
				started, endTime, NetworkTime.time);

			countdownEndTime = endTime;
			doCountdown = started;

			if (doCountdown && countdownEndTime > NetworkTime.time)
			{
				currentState = CountdownState.CountingDown;
			}
			else
			{
				// Already expired or invalid
				currentState = CountdownState.Finished;
				OnFinishedCountingDown?.Invoke();
			}

			UpdateCountdownState();
		}

		private void UpdateCountdownState()
		{
			if (countdownText == null || statusText == null)
			{
				Debug.LogError("Countdown text or status text is null");
				return;
			}

			if (!gameObject.activeSelf)
				gameObject.SetActive(true);

			double timeRemaining = countdownEndTime - NetworkTime.time;

			if (doCountdown && timeRemaining > 0)
			{
				// Still counting down
				if (currentState != CountdownState.CountingDown)
					currentState = CountdownState.CountingDown;

				UpdateCountdownText(timeRemaining, "Next Shift in:");
			}
			else
			{
				// Time has expired
				if (currentState == CountdownState.CountingDown)
				{
					currentState = CountdownState.Finished;
					OnFinishedCountingDown?.Invoke();
				}

				double elapsed = Math.Max(0, -timeRemaining);
				UpdateCountdownText(elapsed, "Time Since Shift Started:");
			}
		}
	}
}