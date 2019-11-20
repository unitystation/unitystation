using System;
using UnityEngine;

/// <summary>
/// Main component for Mop. Allows mopping to be done on tiles.
/// </summary>
[RequireComponent(typeof(Pickupable))]
[RequireComponent(typeof(ReagentContainer))]
public class Mop : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{
	private ReagentContainer reagentContainer;

	private void Awake()
	{
		if (!reagentContainer)
		{
			reagentContainer = GetComponent<ReagentContainer>();
		}
	}


	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//can only mop tiles
		if (!Validations.HasComponent<InteractableTiles>(interaction.TargetObject)) return false;

		//don't attempt to mop walls
		if (MatrixManager.IsWallAt(interaction.WorldPositionTarget.RoundToInt(), isServer: side == NetworkSide.Server))
		{
			return false;
		}

		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		if (reagentContainer.IsEmpty) //regs < 1
		{	//warning
			Chat.AddExamineMsg(interaction.Performer, "Your mop is dry!");
			return;
		}
		//server is performing server-side logic for the interaction
		//do the mopping
		var progressFinishAction = new ProgressCompleteAction(() =>
		{
			CleanTile(interaction.WorldPositionTarget);
			Chat.AddExamineMsg(interaction.Performer, "You finish mopping.");
		});

		//todo clean what exactly
		Chat.AddActionMsgToChat(interaction.Performer, $"You begin to clean the floor with {gameObject.ExpensiveName()}...",
			$"{interaction.Performer.ExpensiveName()} begins to clean the floor with {gameObject.ExpensiveName()}.");
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