using System.Collections;
using UnityEngine;
using Managers;
using Strings;

namespace InGameEvents
{
	public class EventCommsBlackout : EventScriptBase
	{
		/// <summary>
		/// A temporary solution to providing communications blackouts until telecomms is implemented.
		/// </summary>
		public static bool CommsDown = false;

		public override void OnEventStart()
		{
			if (AnnounceEvent)
			{
				var text = "Ionospheric anomalies dete'fZ\\kg5_0-BZZZZZT";

				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Alert);
			}

			if (FakeEvent) return;

			base.OnEventStart();
		}

		public override void OnEventStartTimed()
		{
			CommsDown = true;

			Invoke(nameof(RestoreComms), Random.Range(30, 120));
		}

		private void RestoreComms()
		{
			CommsDown = false;
		}
	}
}
