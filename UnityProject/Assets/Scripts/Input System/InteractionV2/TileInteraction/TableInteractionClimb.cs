using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TableInteractionClimb", menuName = "Interaction/TileInteraction/TableInteractionClimb")]
public class TableInteractionClimb : TileInteraction
{
	public override bool WillInteract(TileApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.TileApplyType != TileApply.ApplyType.MouseDrop) return false;
		return true;
	}

	public override void ServerPerformInteraction(TileApply interaction)
	{
		StandardProgressActionConfig cfg = new StandardProgressActionConfig(StandardProgressActionType.Construction, false, false, false);
		StandardProgressAction.Create(cfg, () =>
		{
			PlayerScript playerScript;
			if (interaction.UsedObject.TryGetComponent(out playerScript))
			{
				playerScript.PlayerSync.SetPosition(interaction.WorldPositionTarget);
			}
			else
			{
				var transformComp = interaction.UsedObject.GetComponent<CustomNetTransform>();
				if (transformComp != null)
				{
					transformComp.AppearAtPositionServer(interaction.WorldPositionTarget);
				}
			}
		}).ServerStartProgress(interaction.WorldPositionTarget, 3.0f, interaction.Performer);
	}
}
