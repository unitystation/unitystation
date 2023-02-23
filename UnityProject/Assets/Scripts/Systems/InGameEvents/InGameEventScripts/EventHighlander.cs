using System.Collections;
using Antagonists;
using Managers;
using Messages.Server;
using Strings;
using UI;
using UnityEngine;

namespace InGameEvents
{
	public class EventHighlander : EventScriptBase
	{
		[SerializeField] private Antagonist highlanderAntag;
		[SerializeField] private float eventTime = 900f;

		private float remainingTime = 0f;

		public float RemainingTime => remainingTime;

		public override void OnEventStart()
		{
			CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce,
				"Come 'n' git th' disk ye cunts. Ainlie yin kin win. ", CentComm.UpdateSound.Alert);
			if (FakeEvent) return;
			base.OnEventStart();
			AntagManager.Instance.ActiveAntags.Clear();
			GameManager.Instance.PrimaryEscapeShuttle.OnShuttleCalled += OnShuttleCalled;
			StartCoroutine(StartRoundTimer());
			foreach (var player in PlayerList.Instance.GetAlivePlayers())
			{
				StartCoroutine(AntagManager.Instance.ServerRespawnAsAntag(player, highlanderAntag));
				HighlanderTimerMessage.Send(player);
			}
		}

		private IEnumerator StartRoundTimer()
		{
			int Round = GameManager.RoundID;
			remainingTime = eventTime;
			while (remainingTime > 0f)
			{
				remainingTime -= 1f;
				yield return WaitFor.Seconds(1f);
				if (Round != GameManager.RoundID)
				{
					yield break; 
				}
			}
			GameManager.Instance.EndRound();
		}

		private void OnShuttleCalled()
		{
			StopCoroutine(StartRoundTimer());
			GameManager.Instance.PrimaryEscapeShuttle.blockCall = true;
			GameManager.Instance.PrimaryEscapeShuttle.blockRecall = true;
		}
	}
}