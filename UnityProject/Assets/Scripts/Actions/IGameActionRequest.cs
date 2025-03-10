using Mirror;
using UnityEngine;

namespace UI.Action
{
	public interface IActionRequestMessage : NetworkMessage
	{
		/// <summary>
		/// The UUID of the action we are requesting
		/// </summary>
		public string RequestedActionGuid { get; set; }

		/// <summary>
		/// Should the requested action attempt to be triggered(usually yes)
		/// </summary>
		public bool AttemptTrigger { get; set; }
	}
}

