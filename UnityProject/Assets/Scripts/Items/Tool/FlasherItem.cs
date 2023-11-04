using Objects;

namespace Items.Tool
{
	public class FlasherItem : FlasherBase, ICheckedInteractable<HandApply>
	{
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			return gameObject.PickupableOrNull().ItemSlot != null;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (OnCooldown)
			{
				Chat.AddExamineMsg(interaction.Performer,"This flash seems to be recharging!");
				return;
			}
			if(interaction.TargetObject == null || interaction.TargetObject.TryGetComponent<RegisterPlayer>(out var player) == false) return;
			Chat.AddActionMsgToChat(interaction.Performer, $"You flash {player.PlayerScript.visibleName}",
				$"{interaction.PerformerPlayerScript.visibleName} flashes {player.PlayerScript.visibleName}!");
			if (stunsPlayers)
			{
				FlashTarget(player.gameObject, flashTime, flashTime + stunExtraTime);
			}
			else
			{
				FlashTarget(player.gameObject, flashTime, 0);
			}
		}
	}
}