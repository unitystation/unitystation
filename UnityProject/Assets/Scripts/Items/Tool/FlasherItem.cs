using Objects;

namespace Items.Tool
{
	public class FlasherItem : FlasherBase, ICheckedInteractable<HandApply>
	{
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (OnCooldown)
			{
				Chat.AddExamineMsg(interaction.Performer,"This flash seems to be on cooldown!");
				return;
			}
			if(interaction.TargetObject == null || interaction.TargetObject.TryGetComponent<RegisterPlayer>(out var player) == false) return;
			Chat.AddActionMsgToChat(interaction.Performer, $"You flash {player.PlayerScript.visibleName}",
				$"{interaction.PerformerPlayerScript.visibleName} flashes {player.PlayerScript.visibleName}!");
			FlashTarget(player.gameObject);
		}
	}
}