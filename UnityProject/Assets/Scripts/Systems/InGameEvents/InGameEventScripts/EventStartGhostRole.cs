using System.Collections;
using UnityEngine;
using Systems.GhostRoles;
using ScriptableObjects;
using Managers;
using Strings;

namespace InGameEvents
{
	public class EventStartGhostRole : EventScriptBase
	{
		[Tooltip("The ghost role to offer ghosts.")]
		[SerializeField]
		private GhostRoleData ghostRole = default;

		[Tooltip("The text to use for Central Command announcements. Leave empty to disable.")]
		[SerializeField]
		private string message = default;

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
