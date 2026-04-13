using UnityEngine;
using US13.Core.Chat;
using US13.Managers;
using US13.Messages.Server;

namespace US13.Systems.Antagonists.Objectives.TeamObjectives
{
	[CreateAssetMenu(menuName = "ScriptableObjects/AntagObjectives/CorrectVampireAmount")]
	public class CorrectVampireAmount : TeamObjective
	{
		private int MaxNumberOfVampires=> Mathf.CeilToInt(PlayerList.Instance.InGamePlayers.Count * 0.20f);

		protected override void Setup()
		{
			foreach (var x in team.TeamMembers)
			{
				UpdateChatMessage.Send(x.Owner.Body.gameObject, ChatChannel.System, ChatModifier.None, $"<color=red>The chaplain sees to your demise.</color>");
			}
			description += $"{MaxNumberOfVampires}\n<color=red>The chaplain sees to your demise.</color>";
		}

		protected override bool CheckCompletion()
		{
			var numberOfAliveVampires = team.TeamMembers.Count;
			foreach (var vamp in team.TeamMembers)
			{
				if (vamp.Owner?.CurrentPlayScript?.IsDeadOrGhost == true) numberOfAliveVampires--;
			}
			return numberOfAliveVampires >= 1 && numberOfAliveVampires <= MaxNumberOfVampires;
		}
	}

}