
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
		return interaction.HandObject == null;
	}

	public override void ServerPerformInteraction(TileApply interaction)
	{
		Chat.AddActionMsgToChat(interaction.Performer,
			$"You knock on the {interaction.BasicTile.DisplayName}.", $"{interaction.Performer.ExpensiveName()} knocks on the {interaction.BasicTile.DisplayName}.");
		SoundManager.GlassknockAtPosition(interaction.WorldPositionTarget);
	}

}
