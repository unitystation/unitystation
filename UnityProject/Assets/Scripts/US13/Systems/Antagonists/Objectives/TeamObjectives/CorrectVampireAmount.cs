using UnityEngine;
using US13.Core.Chat;
using US13.Managers;
using US13.Messages.Server;
using US13.Player;

namespace US13.Systems.Antagonists.Objectives.TeamObjectives
{
	[CreateAssetMenu(menuName = "ScriptableObjects/AntagObjectives/CorrectVampireAmount")]
	public class CorrectVampireAmount : TeamObjective
	{
		private int MaxNumberOfVampires=> Mathf.CeilToInt(1 + (PlayerList.Instance.InGamePlayers.Count * (maxAmountOfVampiresPercent / 100.0f)));
		[SerializeField] private string initialDescription = "";
		[SerializeField, Range(0, 100)] private int maxAmountOfVampiresPercent = 20;
		protected override void SetupInGame()
		{
			foreach (var x in team.TeamMembers)
			{
				UpdateChatMessage.Send(x.Owner.Body.gameObject, ChatChannel.System, ChatModifier.None, $"<color=red>The chaplain sees to your demise.</color>");
			}
			UpdateObjectiveDescription();
		}

		public void UpdateObjectiveDescription()
		{
			description = $"{initialDescription}{MaxNumberOfVampires}\n<color=red>The chaplain sees to your demise.</color>";
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