using UnityEngine;

/// <summary>
/// Main component for Mop. Allows mopping to be done on tiles.
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class Mop : Interactable<PositionalHandApply>
{
	//server-side only, tracks if mop is currently cleaning.
	private bool isCleaning;

	protected override InteractionValidationChain<PositionalHandApply> InteractionValidationChain()
	{
		return InteractionValidationChain<PositionalHandApply>.Create()
			.WithValidation(CanApply.ONLY_IF_CONSCIOUS)
			.WithValidation(DoesUsedObjectHaveComponent<Mop>.DOES)
			.WithValidation(DoesTargetObjectHaveComponent<InteractableTiles>.DOES);
	}

	protected override void ServerPerformInteraction(PositionalHandApply interaction)
	{
		if (!isCleaning)
		{
			//server is performing server-side logic for the interaction
			//do the mopping
			var progressFinishAction = new FinishProgressAction(
				reason =>
				{
					if (reason == FinishProgressAction.FinishReason.INTERRUPTED)
					{
						CancelCleanTile();
					}
					else if (reason == FinishProgressAction.FinishReason.COMPLETED)
					{
						CleanTile(interaction.WorldPositionTarget);
					}
				}
			);
			isCleaning = true;

			//Start the progress bar:
			UIManager.ProgressBar.StartProgress(interaction.WorldPositionTarget.RoundToInt(),
				5f, progressFinishAction, interaction.Performer);
		}
	}

	private void CleanTile (Vector3 worldPos)
    {
	    var worldPosInt = worldPos.CutToInt();
	    var matrix = MatrixManager.AtPoint( worldPosInt, true );
	    var localPosInt = MatrixManager.WorldToLocalInt( worldPosInt, matrix );
	    var floorDecals = MatrixManager.GetAt<FloorDecal>(worldPosInt, isServer: true);

	    for ( var i = 0; i < floorDecals.Count; i++ )
	    {
		    floorDecals[i].DisappearFromWorldServer();
	    }

	    if (!MatrixManager.IsSpaceAt(worldPosInt, true))
	    {
		    // Create a WaterSplat Decal (visible slippery tile)
		    EffectsFactory.Instance.WaterSplat(worldPosInt);

		    // Sets a tile to slippery
		    matrix.MetaDataLayer.MakeSlipperyAt(localPosInt);
	    }

	    isCleaning = false;
    }

	private void CancelCleanTile()
	{
		//stop the in progress cleaning
		isCleaning = false;
	}

}