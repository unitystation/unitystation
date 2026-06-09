using System.Collections.Generic;
using US13.Core.Addressables;
using US13.Managers;
using US13.Strings;

namespace US13.Systems.InGameEvents.InGameEventScripts
{
	public class EventsCombined : EventScriptBase
	{

		public List<EventScriptBase> EventsToTrigger = new List<EventScriptBase>();

		public string AnnouncementText;

		public override void OnEventStart()
		{
			if (AnnounceEvent)
			{
				var text = AnnouncementText;

				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.NoSound);

				_ = SoundManager.PlayNetworked(CommonSounds.Instance.SpanomaliesAnnouncement);
			}

			if (FakeEvent) return;

			base.OnEventStart();
		}


		public override void OnEventStartTimed()
		{
			foreach (var Event in EventsToTrigger)
			{
				Event.FakeEvent = FakeEvent;
				Event.AnnounceEvent = false;
				Event.TriggerEvent();
			}
		}

	}
}
