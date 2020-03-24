
using UnityEngine;

/// <summary>
/// Interaction logic for windows. Knocks on them when empty handed and non-harm.
/// </summary>
[CreateAssetMenu(fileName = "KnockWindow", menuName = "Interaction/TileInteraction/KnockWindow")]
public class KnockWindow : TileInteraction
{
	public override bool WillInteract(TileApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.Intent == Intent.Harm) return false;
		if (interaction.HandObject != null) return false;
		//don't allow spamming window knocks really fast
		return Cooldowns.TryStart(interaction, this, 1, side);
	}

	public override void ServerPerformInteraction(TileApply interaction)
	{
		Chat.AddActionMsgToChat(interaction.Performer,
			$"You knock on the {interaction.BasicTile.DisplayName}.", $"{interaction.Performer.ExpensiveName()} knocks on the {interaction.BasicTile.DisplayName}.");
		SoundManager.GlassknockAtPosition(interaction.WorldPositionTarget, interaction.Performer);
	}

}
