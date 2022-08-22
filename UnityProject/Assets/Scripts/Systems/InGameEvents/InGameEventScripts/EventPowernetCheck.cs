using System.Collections;
using UnityEngine;
using Managers;
using Strings;
using Systems.Electricity.NodeModules;

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

				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.NoSound);
				_ = SoundManager.PlayNetworked(CommonSounds.Instance.PowerOffAnnouncement);
			}

			if (FakeEvent) return;

			base.OnEventStart();
		}

		public override void OnEventStartTimed()
		{
			foreach (var node in FindObjectsOfType<ElectricalNodeControl>())
			{
				node.TurnOffSupply();
				StartCoroutine(RestartPowerSupply(node));
			}

			base.OnEventStartTimed();
		}

		public override void OnEventEndTimed()
		{
			_ = SoundManager.PlayNetworked(CommonSounds.Instance.PowerOnAnnouncement);
		}

		private IEnumerator RestartPowerSupply(ElectricalNodeControl node)
		{
			yield return WaitFor.Seconds(Random.Range(30, 120));
			node.TurnOnSupply();
		}
	}
}
