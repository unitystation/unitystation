using UnityEngine;

/// <summary>
/// Main component for Mop. Allows mopping to be done on tiles.
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class Mop : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{
	//server-side only, tracks if mop is currently cleaning.
	private bool isCleaning;

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//can only mop tiles
		if (!Validations.HasComponent<InteractableTiles>(interaction.TargetObject)) return false;
		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		//server is performing server-side logic for the interaction
		//do the mopping
		var progressFinishAction = new ProgressCompleteAction(() => CleanTile(interaction.WorldPositionTarget));
		//Start the progress bar:
		UIManager.ServerStartProgress(ProgressAction.Mop, interaction.WorldPositionTarget.RoundToInt(),
			5f, progressFinishAction, interaction.Performer);
	}

	public void CleanTile(Vector3 worldPos)
	{
		var worldPosInt = worldPos.CutToInt();
		var matrix = MatrixManager.AtPoint(worldPosInt, true);
		var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrix);
		matrix.MetaDataLayer.Clean(worldPosInt, localPosInt, true);
	}
}