using InGameEvents;
using Messages.Client;
using Messages.Server;
using TMPro;
using UnityEngine;

namespace UI
{
	public class HighlanderTimerUI : Managers.SingletonManager<HighlanderTimerUI>
	{
		[SerializeField] private TMP_Text timerText;
		[SerializeField] private GameObject content;
		private float timeLeft;

		public override void Awake()
		{
			base.Awake();
			content.SetActive(false);
			EventManager.AddHandler(Event.PlayerRejoined, Rejoin);
			EventManager.AddHandler(Event.LoggedOut, Hide);
			EventManager.AddHandler(Event.RoundEnded, Hide);
		}

		private void UpdateTimer()
		{
			timeLeft--;
			timerText.text = $"{Mathf.RoundToInt(timeLeft / 60)}:{(timeLeft % 60).RoundToLargestInt()}";
		}

		public void Rejoin()
		{
			RequestHighlanderTime.Send(PlayerManager.LocalPlayerScript.PlayerInfo);
		}

		public void Show(float time)
		{
			content.SetActive(true);
			timeLeft = time;
			UpdateManager.Add(UpdateTimer, 1f);
		}

		private void Hide()
		{
			content.SetActive(false);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateTimer);
		}
	}
}