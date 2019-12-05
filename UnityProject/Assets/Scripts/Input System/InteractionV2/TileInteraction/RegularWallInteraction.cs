
using UnityEngine;

/// <summary>
/// Tile interaction logic for regular (non-reinforced) walls.
/// </summary>
[CreateAssetMenu(fileName = "RegularWallInteraction", menuName = "Interaction/TileInteraction/RegularWallInteraction")]
public class RegularWallInteraction : TileInteraction
{
	public override bool WillInteract(TileApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		var welder = interaction.HandObject?.GetComponent<Welder>();
		return welder != null && welder.isOn;
	}

	public override void ServerPerformInteraction(TileApply interaction)
	{
		//unweld to a girder
		var progressFinishAction = new ProgressCompleteAction(
			() =>
			{
				SoundManager.PlayNetworkedAtPos("Weld", interaction.WorldPositionTarget, 0.8f);
				interaction.TileChangeManager.RemoveTile(interaction.TargetCellPos, LayerType.Walls);
				SoundManager.PlayNetworkedAtPos("Deconstruct", interaction.WorldPositionTarget, 1f);

				//girder / metal always appears in place of deconstructed wall
				Spawn.ServerPrefab(CommonPrefabs.Instance.Girder, interaction.WorldPositionTarget);
				Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, interaction.WorldPositionTarget, count: 2);
				interaction.TileChangeManager.SubsystemManager.UpdateAt(interaction.TargetCellPos);
				Chat.AddActionMsgToChat(interaction.Performer, "You remove the outer plating..",
					$"{interaction.Performer} removes the outer plating.");
			}
		);

		Chat.AddActionMsgToChat(interaction.Performer, "You begin slicing through the outer plating...",
			$"{interaction.Performer} begins slicing through the outer plating...");
		ToolUtils.UseTool(interaction, 10f, progressFinishAction);
	}
}
