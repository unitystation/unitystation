using Chemistry.Components;
using System;
using UnityEngine;

/// <summary>
/// Main component for Mop. Allows mopping to be done on tiles.
/// </summary>
[RequireComponent(typeof(Pickupable))]
[RequireComponent(typeof(ReagentContainer))]
public class Mop : MonoBehaviour, ICheckedInteractable<PositionalHandApply>, IExaminable
{
	private static readonly StandardProgressActionConfig ProgressConfig =
		new StandardProgressActionConfig(StandardProgressActionType.Mop, true, false);

	private ReagentContainer reagentContainer;

	[SerializeField]
	[Range(1,50)]
	private int reagentsPerUse = 5;

	[SerializeField]
	[Range(0.1f,20f)]
	private float useTime = 5f;

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
		if (reagentContainer.ReagentMixTotal < 1)
		{	//warning
			Chat.AddExamineMsg(interaction.Performer, "Your mop is dry!");
			return;
		}
		//server is performing server-side logic for the interaction
		//do the mopping
		void CompleteProgress()
		{
			CleanTile(interaction.WorldPositionTarget);
			Chat.AddExamineMsg(interaction.Performer, "You finish mopping.");
		}

		//Start the progress bar:
		var bar = StandardProgressAction.Create(ProgressConfig, CompleteProgress)
			.ServerStartProgress(interaction.WorldPositionTarget.RoundToInt(),
				useTime, interaction.Performer);
		if (bar)
		{
			Chat.AddActionMsgToChat(interaction.Performer,
				$"You begin to clean the floor with {gameObject.ExpensiveName()}...",
				$"{interaction.Performer.name} begins to clean the floor with {gameObject.ExpensiveName()}.");
		}
	}

	public void CleanTile(Vector3 worldPos)
	{
		MatrixManager.ReagentReact(reagentContainer.TakeReagents(reagentsPerUse), worldPos.CutToInt());
	}

	public string Examine(Vector3 worldPos = default)
	{
		string msg = null;
		if (reagentContainer)
		{
			msg = !reagentContainer.IsEmpty ? "It's wet." : "It's dry. Use bucket to wet it.";
		}

		return msg;
	}
}