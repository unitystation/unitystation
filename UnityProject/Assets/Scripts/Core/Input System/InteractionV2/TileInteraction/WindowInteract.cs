using TileManagement;
using UnityEngine;

/// <summary>
/// Interaction logic for windows. Help intent knock while empty handed or repair when using a welder
/// </summary>
[CreateAssetMenu(fileName = "WindowInteract", menuName = "Interaction/TileInteraction/WindowInteract")]
public class WindowInteract : TileInteraction
{
	public override bool WillInteract(TileApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (interaction.Intent == Intent.Harm) return false;

		if (interaction.HandObject != null)
		{
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Welder) && Validations.HasUsedActiveWelder(interaction))
				return true;

			return false;
		}
		//don't allow spamming window knocks really fast
		return Cooldowns.TryStart(interaction, this, 1, side);
	}

	public override void ServerPerformInteraction(TileApply interaction)
	{
		if (interaction.HandObject == null)
		{
			Chat.AddActionMsgToChat(interaction.Performer,
				$"You knock on the {interaction.BasicTile.DisplayName}.", $"{interaction.Performer.ExpensiveName()} knocks on the {interaction.BasicTile.DisplayName}.");

			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.GlassKnock, interaction.WorldPositionTarget, sourceObj: interaction.Performer);
		}
		else
		{
			ToolUtils.ServerUseToolWithActionMessages(interaction, 4f,
				$"You begin repairing the {interaction.BasicTile.DisplayName}...",
				$"{interaction.Performer.ExpensiveName()} begins to repair the {interaction.BasicTile.DisplayName}...",
				$"You repair the {interaction.BasicTile.DisplayName}.",
				$"{interaction.Performer.ExpensiveName()} repairs the {interaction.BasicTile.DisplayName}.",
				() => RepairWindow(interaction));
		}
	}

	private void RepairWindow(TileApply interaction)
	{
		var tileMapDamage = interaction.TargetInteractableTiles.GetComponentInChildren<MetaTileMap>().Layers[LayerType.Windows].gameObject.GetComponent<TilemapDamage>();;
		tileMapDamage.RemoveTileEffects(interaction.TargetCellPos);
	}

}
