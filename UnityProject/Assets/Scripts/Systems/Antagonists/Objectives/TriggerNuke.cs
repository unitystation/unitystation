using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Antagonists
{
	/// <summary>
	/// An objective to set off the nuke on the station
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/Objectives/TriggerNuke")]
	public class TriggerNuke : Objective
	{
		private Nuke NukeTarget;
		protected override void Setup()
		{
			//Check to see if there is a nuke and communicate the nuke code:
			NukeTarget = FindObjectOfType<Nuke>();
			if (NukeTarget == null)
			{
				Logger.LogWarning("Unable to setup nuke objective, no nuke found in scene!", Category.Antags);
			}
			else
			{
				UpdateChatMessage.Send(Owner.body.gameObject, ChatChannel.Syndicate, ChatModifier.None,
					"We have intercepted the code for the nuclear weapon: " + NukeTarget.NukeCode);
				description += ". Intercepted nuke code is " + NukeTarget.NukeCode;
			}
		}

		/// <summary>
		/// Check if the nuke target was detonated
		/// </summary>
		protected override bool CheckCompletion()
		{
			return NukeTarget.IsDetonated;
		}
	}
}