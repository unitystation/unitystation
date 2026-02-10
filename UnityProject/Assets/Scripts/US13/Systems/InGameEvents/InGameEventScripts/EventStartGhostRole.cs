using UnityEngine;
using US13.Managers;
using US13.ScriptableObjects;
using US13.Strings;
using US13.Systems.GameModes;
using US13.Systems.GhostRoles;

namespace US13.Systems.InGameEvents.InGameEventScripts
{
	public class EventStartGhostRole : EventScriptBase
	{
		[Tooltip("The ghost role to offer ghosts.")]
		[SerializeField]
		private GhostRoleData ghostRole = default;

		[Tooltip("The text to use for Central Command announcements. Leave empty to disable.")]
		[SerializeField]
		private string message = default;

		public GameMode DoNotTriggerOnAMode;

		public int IfGameModeRequiredPop = -1;

		public override bool CustomTriggerCriteria()
		{
			if (DoNotTriggerOnAMode == null) return true;
			if (GameManager.Instance.GameMode == DoNotTriggerOnAMode)
			{
				if ((IfGameModeRequiredPop >= PlayerList.Instance.InGamePlayers.Count) == false)
				{
					return false;
				}
			}
			return true;
		}

		public override void OnEventStart()
		{


			if (AnnounceEvent && string.IsNullOrEmpty(message) == false)
			{
				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, message, CentComm.UpdateSound.Alert);
			}

			if (FakeEvent) return;

			base.OnEventStart();
		}

		public override void OnEventStartTimed()
		{
			GhostRoleManager.Instance.ServerCreateRole(ghostRole);
		}
	}
}
