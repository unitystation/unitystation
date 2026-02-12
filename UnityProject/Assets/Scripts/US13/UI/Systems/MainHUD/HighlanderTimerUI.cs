using Shared.Managers;
using TMPro;
using UnityEngine;
using US13.Managers;
using US13.Managers.UpdateManager;
using US13.Messages.Client;
using US13.Player;
using Util;
using Event = US13.Managers.Event;

namespace US13.UI.Systems.MainHUD
{
	public class HighlanderTimerUI : SingletonManager<HighlanderTimerUI>
	{
		[SerializeField] private TMP_Text timerText;
		[SerializeField] private GameObject content;
		private float timeLeft;

		public override void Awake()
		{
			base.Awake();
			content.SetActive(false);
		}

		public void OnEnable()
		{
			EventManager.AddHandler(Event.PlayerRejoined, Rejoin);
			EventManager.AddHandler(Event.ServerLoggedOut, Hide);
			EventManager.AddHandler(Event.RoundEnded, Hide);
		}

		public void OnDisable()
		{
			EventManager.RemoveHandler(Event.PlayerRejoined, Rejoin);
			EventManager.RemoveHandler(Event.ServerLoggedOut, Hide);
			EventManager.RemoveHandler(Event.RoundEnded, Hide);
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