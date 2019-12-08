
using UnityEngine;

/// <summary>
/// Allows object to be used as a pickaxe to mine rocks.
/// </summary>
public class Pickaxe : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{
	private const float PLASMA_SPAWN_CHANCE = 0.5f;

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		var interactableTiles = interaction.TargetObject.GetComponent<InteractableTiles>();
		if (interactableTiles == null) return false;

		return Validations.IsMineableAt(interaction.WorldPositionTarget, interactableTiles.MetaTileMap);
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		//server is performing server-side logic for the interaction
		//do the mining
		var progressFinishAction = new ProgressCompleteAction(() => FinishMine(interaction));

		//Start the progress bar:
		//technically pickaxe is deconstruction, so it would interrupt any construction / deconstruction being done
		//on that tile
		var bar = UIManager.ServerStartProgress(ProgressAction.Construction, interaction.WorldPositionTarget.RoundToInt(),
			5f, progressFinishAction, interaction.Performer);
		if (bar != null)
		{
			SoundManager.PlayNetworkedAtPos("pickaxe#", interaction.WorldPositionTarget);
		}
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

			var ttile = interactableTiles.LayerTileAt(interaction.WorldPositionTarget) as BasicTile;
			Spawn.ServerPrefab(ttile.SpawnOnDeconstruct, interaction.WorldPositionTarget ,  count : ttile.SpawnAmountOnDeconstruct);
			interactableTiles.TileChangeManager.RemoveTile(cellPos, LayerType.Walls);
		}
	}
}
