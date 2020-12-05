
using UnityEngine;
using AddressableReferences;

/// <summary>
/// Allows object to be used as a pickaxe to mine rocks.
/// </summary>
public class Pickaxe : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{
	[SerializeField] private AddressableAudioSource pickaxe = null;
	private static readonly StandardProgressActionConfig ProgressConfig =
		new StandardProgressActionConfig(StandardProgressActionType.Construction, true);

	private const float PLASMA_SPAWN_CHANCE = 0.5f;

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (!Validations.HasTarget(interaction)) return false;
		var interactableTiles = interaction.TargetObject.GetComponent<InteractableTiles>();
		if (interactableTiles == null) return false;

		return Validations.IsMineableAt(interaction.WorldPositionTarget, interactableTiles.MetaTileMap);
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		//server is performing server-side logic for the interaction
		//do the mining
		void ProgressComplete() => FinishMine(interaction);

		//Start the progress bar:
		//technically pickaxe is deconstruction, so it would interrupt any construction / deconstruction being done
		//on that tile
		//TODO: Refactor this to use ToolUtils once that's merged in

		var bar = StandardProgressAction.Create(ProgressConfig, ProgressComplete)
			.ServerStartProgress(interaction.WorldPositionTarget.RoundToInt(),
				5f, interaction.Performer);
		if (bar != null)
		{
			SoundManager.PlayNetworkedAtPos(pickaxe, interaction.WorldPositionTarget, sourceObj: interaction.Performer);
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
			SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.BreakStone, interaction.WorldPositionTarget, sourceObj: interaction.Performer);
			var cellPos = interactableTiles.MetaTileMap.WorldToCell(interaction.WorldPositionTarget);

			var tile = interactableTiles.LayerTileAt(interaction.WorldPositionTarget) as BasicTile;
			Spawn.ServerPrefab(tile.SpawnOnDeconstruct, interaction.WorldPositionTarget ,  count : tile.SpawnAmountOnDeconstruct);
			interactableTiles.TileChangeManager.RemoveTile(cellPos, LayerType.Walls);
			interactableTiles.TileChangeManager.RemoveOverlay(cellPos, LayerType.Effects);
		}
	}
}
