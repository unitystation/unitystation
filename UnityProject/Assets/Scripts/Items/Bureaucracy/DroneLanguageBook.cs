using UnityEngine;

namespace Items.Bureaucracy
{
	public class DroneLanguageBook : LanguageBook
	{
		[SerializeField]
		private bool overrideRequirements = false;

		public override void ServerPerformInteraction(HandApply interaction)
		{
			if (IsSilicon(interaction.TargetObject) == false)
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You beat {interaction.TargetObject.ExpensiveName()} over the head with {gameObject.ExpensiveName()}.",
					$"{interaction.PerformerPlayerScript.visibleName} beats {interaction.TargetObject.ExpensiveName()} over the head with {gameObject.ExpensiveName()}.");
				return;
			}

			base.ServerPerformInteraction(interaction);
		}

		private bool IsSilicon(GameObject targetObject)
		{
			if (overrideRequirements == false)
			{
				if (targetObject.TryGetComponent<PlayerScript>(out var playerScript) == false) return false;

				if (playerScript.PlayerType != PlayerTypes.Ai) return false;
			}

			return true;
		}

		public override void ServerPerformInteraction(HandActivate interaction)
		{
			if (IsSilicon(interaction.Performer) == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You beat yourself over the head with {gameObject.ExpensiveName()}!");
				return;
			}

			base.ServerPerformInteraction(interaction);
		}
	}
}