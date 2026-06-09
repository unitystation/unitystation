using UnityEngine;
using US13.Core.Chat;
using US13.Messages.Server;
using US13.Objects;

namespace US13.Systems.Antagonists.Objectives
{
	/// <summary>
	/// An objective to set off the nuke on the station
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/TriggerNuke")]
	public class TriggerNuke : Objective
	{
		protected override void Setup()
		{
			UpdateChatMessage.Send(Owner.Body.gameObject, ChatChannel.Syndicate, ChatModifier.None,
				"We have intercepted the code for the nuclear weapon: " + AntagManager.SyndiNukeCode);
			description += ". Intercepted nuke code is " + AntagManager.SyndiNukeCode;
		}

		/// <summary>
		/// Check if the nuke target was detonated
		/// </summary>
		protected override bool CheckCompletion()
		{
			return Nuke.Detonated;
		}
	}
}