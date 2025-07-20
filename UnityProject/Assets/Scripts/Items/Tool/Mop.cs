using Chemistry.Components;
using System;
using Chemistry;
using UnityEngine;

/// <summary>
/// Main component for Mop. Allows mopping to be done on tiles.
/// </summary>
[RequireComponent(typeof(Pickupable))]
[RequireComponent(typeof(ReagentContainer))]
public class Mop : MonoBehaviour, ICheckedInteractable<PositionalHandApply>, IExaminable
{
	public Reagent Water;
	public Reagent SpaceCleaner;

	private static readonly StandardProgressActionConfig ProgressConfig =
		new StandardProgressActionConfig(StandardProgressActionType.Mop, true, false);

	private ReagentContainer reagentContainer;

	[SerializeField] [Range(1, 50)] private int reagentsPerUse = 5;

	[SerializeField] [Range(0.1f, 20f)] private float useTime = 5f;

	private void Awake()
	{
		if (!reagentContainer)
		{
			reagentContainer = GetComponent<ReagentContainer>();
		}
	}


	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		//can only mop tiles
		if (!Validations.HasComponent<InteractableTiles>(interaction.TargetObject)) return false;

		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		Vector3Int worldPos = interaction.WorldPositionTarget.RoundToInt();
		MatrixInfo matrixInfo = MatrixManager.AtPoint(worldPos, true);
		Vector3Int localPos = MatrixManager.WorldToLocalInt(worldPos, matrixInfo);

		if (matrixInfo.MetaDataLayer.Get(localPos).ReagentsOnTile.Total > 0)
		{
			if (reagentContainer.IsFull)
			{
				Chat.AddExamineMsg(interaction.Performer,
					"your mop is too wet to soak up any of the liquid on the floor");
				return;
			}
		}
		else
		{
			if (reagentContainer.ReagentMixTotal < 1)
			{
				if (matrixInfo.MetaDataLayer.Get(localPos).ReagentsOnTile.Total == 0)
				{
					Chat.AddExamineMsg(interaction.Performer, "Your mop is dry, and so is the floor!");
					return;
				}
			}
		}

		void CleanUpMess(bool slippery, MatrixInfo matrixInfo, Vector3Int localPos, Vector3Int worldPos)
		{
			matrixInfo.MetaDataLayer.Clean(worldPos, localPos, slippery);
			reagentContainer.TakeReagents(reagentsPerUse);
		}

		//server is performing server-side logic for the interaction
		//do the mopping
		void CompleteProgress()
		{
			Vector3Int worldPos = interaction.WorldPositionTarget.RoundToInt();
			MatrixInfo matrixInfo = MatrixManager.AtPoint(worldPos, true);
			Vector3Int localPos = MatrixManager.WorldToLocalInt(worldPos, matrixInfo);


			if (matrixInfo.MetaDataLayer.Get(localPos).ReagentsOnTile.Total > 0) //you need to check state could have changed while you are working
			{
				if (reagentContainer.IsFull)
				{
					Chat.AddExamineMsg(interaction.Performer,
						"your mob is too wet to soak up any of the liquid on the floor");
					return;
				}
			}
			else
			{
				if (reagentContainer.ReagentMixTotal < 1)
				{
					if (matrixInfo.MetaDataLayer.Get(localPos).ReagentsOnTile.Total == 0)
					{
						Chat.AddExamineMsg(interaction.Performer, "Your mop is dry, and so is the floor!");
						return;
					}
				}
			}

			var Liquid = matrixInfo.MetaDataLayer.Get(localPos).ReagentsOnTile;
			if (Liquid.Total > 0)
			{
				reagentContainer.Add(Liquid);
				matrixInfo.MetaDataLayer.RemoveLiquidOnTile(localPos, matrixInfo.Matrix.GetMetaDataNode(localPos));
			}
			else
			{
				if (reagentContainer)
				{
					if (reagentContainer.MajorMixReagent == Water)
					{
						CleanUpMess(true, matrixInfo, localPos, worldPos); //We can't spill the reagents because It has different behaviour than if you just Spilled directly
					}
					else if (reagentContainer.MajorMixReagent == SpaceCleaner)
					{
						CleanUpMess(false, matrixInfo, localPos, worldPos); //We can't spill the reagents because It has different behaviour than if you just Spilled directly
					}
					else
					{
						MatrixManager.ReagentReact(reagentContainer.TakeReagents(reagentsPerUse), worldPos);
					}
				}
			}

			Chat.AddExamineMsg(interaction.Performer, "You finish mopping.");
		}

		//Start the progress bar:
		var bar = StandardProgressAction.Create(ProgressConfig, CompleteProgress)
			.ServerStartProgress(interaction.WorldPositionTarget.RoundToInt(),
				useTime, interaction.Performer);
		if (bar)
		{
			Chat.AddActionMsgToChat(interaction.Performer,
				$"You begin to clean the floor with the {gameObject.ExpensiveName()}...",
				$"{interaction.Performer.name} begins to clean the floor with the {gameObject.ExpensiveName()}.");
		}
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