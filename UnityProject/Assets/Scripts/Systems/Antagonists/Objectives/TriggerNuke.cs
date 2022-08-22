using System.Collections;
using System.Collections.Generic;
using Messages.Server;
using UnityEngine;
using Objects.Command;

namespace Antagonists
{
	/// <summary>
	/// An objective to set off the nuke on the station
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/TriggerNuke")]
	public class TriggerNuke : Objective
	{
		protected override void Setup()
		{
			UpdateChatMessage.Send(Owner.body.gameObject, ChatChannel.Syndicate, ChatModifier.None,
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