
using UnityEngine;

/// <summary>
/// Allows object to be used as a pickaxe to mine rocks.
/// </summary>
public class Pickaxe : Interactable<PositionalHandApply>
{
	private const float PLASMA_SPAWN_CHANCE = 0.5f;

	protected override bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side)) return false;
		var interactableTiles = interaction.TargetObject.GetComponent<InteractableTiles>();
		if (interactableTiles == null) return false;

		return Validations.IsMineableAt(interaction.WorldPositionTarget, interactableTiles.MetaTileMap);
	}

	protected override void ServerPerformInteraction(PositionalHandApply interaction)
	{
		//server is performing server-side logic for the interaction
		//do the mining
		var progressFinishAction = new FinishProgressAction(
			reason =>
			{
				if (reason == FinishReason.COMPLETED)
				{
					FinishMine(interaction);
				}
			}
		);

		//Start the progress bar:
		UIManager.ServerStartProgress(interaction.WorldPositionTarget.RoundToInt(),
			5f, progressFinishAction, interaction.Performer, true);
		SoundManager.PlayNetworkedAtPos("pickaxe#", interaction.WorldPositionTarget);
	}

	private void FinishMine(PositionalHandApply interaction)
	{
		var interactableTiles = interaction.TargetObject.GetComponent<InteractableTiles>();
		if (interactableTiles == null)
		{
			Logger.LogError("No interactable tiles found, mining cannot be finished", Category.TileMaps);
		}
		else
		{
			SoundManager.PlayNetworkedAtPos("BreakStone", interaction.WorldPositionTarget);
			var cellPos = interactableTiles.MetaTileMap.WorldToCell(interaction.WorldPositionTarget);
			interactableTiles.TileChangeManager.RemoveTile(cellPos, LayerType.Walls);
			if (Random.value > PLASMA_SPAWN_CHANCE)
			{
				//spawn plasma
				ObjectFactory.SpawnPlasma(1, interaction.WorldPositionTarget.RoundToInt().To2Int());
			}
		}
	}
}
