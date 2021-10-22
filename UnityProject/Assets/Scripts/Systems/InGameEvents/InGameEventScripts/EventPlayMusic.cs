using System.Collections.Generic;
using Managers;
using Strings;
using UnityEngine;

namespace InGameEvents
{
	public class EventPlayMusic : EventScriptBase
	{
		[SerializeField]
		private string announceText = "Music Event Started.";

		public override void OnEventStart()
		{
            if (AnnounceEvent)
			{
				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, announceText, CentComm.UpdateSound.Alert);
			}

			base.OnEventStart();
		}
	}
}