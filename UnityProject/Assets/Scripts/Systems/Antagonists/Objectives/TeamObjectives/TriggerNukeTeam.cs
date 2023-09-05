using System.Collections;
using System.Collections.Generic;
using Messages.Server;
using UnityEngine;
using Objects.Command;

namespace Antagonists
{
	[CreateAssetMenu(menuName = "ScriptableObjects/AntagObjectives/TriggerNukeTeam")]
	public class TriggerNukeTeam : TeamObjective
	{
		protected override void Setup()
		{
			foreach (var x in team.TeamMembers)
			{
				UpdateChatMessage.Send(x.Owner.Body.gameObject, ChatChannel.Syndicate, ChatModifier.None,
					"We have intercepted the code for the nuclear weapon: " + AntagManager.SyndiNukeCode);
			}
			//UpdateChatMessage.Send(Owner.Body.gameObject, ChatChannel.Syndicate, ChatModifier.None,
			//	"We have intercepted the code for the nuclear weapon: " + AntagManager.SyndiNukeCode);
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