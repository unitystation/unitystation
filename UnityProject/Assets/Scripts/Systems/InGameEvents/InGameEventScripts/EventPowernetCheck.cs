using System.Collections;
using UnityEngine;
using Managers;
using Strings;

namespace InGameEvents
{
	public class EventPowernetCheck : EventScriptBase
	{
		public override void OnEventStart()
		{
			if (AnnounceEvent)
			{
				var text = "Abnormal activity detected in the station's powernet." +
					"As a precautionary measure, the station's power will be shut off for an indeterminate duration.";

				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Alert);
				// TODO: Play specific announcement message sound instead of generic alert.
			}

			if (FakeEvent) return;

			base.OnEventStart();
		}

		public override void OnEventStartTimed()
		{
			foreach (var node in FindObjectsOfType<ElectricalNodeControl>())
			{
				node.UpTurnOffSupply();
				StartCoroutine(RestartPowerSupply(node));
			}
		}

		private IEnumerator RestartPowerSupply(ElectricalNodeControl node)
		{
			yield return WaitFor.Seconds(Random.Range(30, 120));
			node.UpTurnOnSupply();
		}
	}
}
