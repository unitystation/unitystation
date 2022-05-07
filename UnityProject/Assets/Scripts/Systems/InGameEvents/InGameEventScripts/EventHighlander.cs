using Antagonists;
using Managers;
using Strings;
using UnityEngine;

namespace InGameEvents
{
	public class EventHighlander : EventScriptBase
	{
		[SerializeField] private Antagonist highlanderAntag;
		public override void OnEventStart()
		{
			CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce,
				"Come 'n' git th' disk ye cunts. Ainlie yin kin win. ", CentComm.UpdateSound.Alert);
			if (FakeEvent) return;
			base.OnEventStart();
			AntagManager.Instance.ActiveAntags.Clear();
			foreach (var player in PlayerList.Instance.GetAlivePlayers())
			{
				StartCoroutine(AntagManager.Instance.ServerRespawnAsAntag(player, highlanderAntag));
			}
		}
	}
}