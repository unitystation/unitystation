using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using Chemistry.Components;
using HealthV2;
using Items;
using UnityEngine;

public class ChemicalPatch : MonoBehaviour, ICheckedInteractable<HandApply>
{

	public ReagentContainer AssociatedMix;

	private static readonly StandardProgressActionConfig applyProgressBar =
		new StandardProgressActionConfig(StandardProgressActionType.CPR);

	public void Awake()
	{
		AssociatedMix = this.GetComponent<ReagentContainer>();
	}

	public virtual bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (!Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject)) return false;
		if (interaction.TargetObject.GetComponent<Dissectible>().GetBodyPartIsopen &&
		    interaction.IsAltClick == false) return false;

		return interaction.Intent == Intent.Help;
	}

	public virtual void ServerPerformInteraction(HandApply interaction)
	{
		if (TryGetComponent<HandPreparable>(out var preparable))
		{
			if (preparable.IsPrepared == false)
			{
				Chat.AddExamineMsg(interaction.Performer, preparable.openingRequirementText);
				return;
			}
		}

		var LHB = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();

		if (interaction.TargetObject != interaction.Performer)
		{
			ServerOtherHeal(interaction.Performer, LHB, interaction);
		}
		else
		{
			ServerApplyChemicals(LHB, interaction);
		}
	}

	private void ServerApplyChemicals(LivingHealthMasterBase livingHealth, HandApply interaction)
	{
		if (livingHealth.RespiratorySystem.IsEVACompatible())
		{
			Chat.AddActionMsgToChat(interaction,
				$"  you failed to stick the" +  this.name +  $" to {livingHealth.gameObject.ExpensiveName()} due to their clothing",
				"{performer} fails to stick a " + this.name +  $" to {livingHealth.gameObject.ExpensiveName()} due to their clothing"  );
			return; //Blocking all the chemicals
		}


		livingHealth.ApplyReagentsToSurface(AssociatedMix.TakeReagents(AssociatedMix.ReagentMixTotal),
			interaction.TargetBodyPart);

		_ = Despawn.ServerSingle(this.gameObject);
	}

	private void ServerOtherHeal(GameObject originator, LivingHealthMasterBase livingHealth, HandApply interaction)
	{
		Chat.AddActionMsgToChat(interaction,
			$" You start to stick the " +  this.name +  $" to {livingHealth.gameObject.ExpensiveName()}",
			"{performer} starts to stick a " + this.name +  $" to {livingHealth.gameObject.ExpensiveName()}"  );

		void ProgressComplete()
		{
			ServerApplyChemicals(livingHealth, interaction);
		}

		StandardProgressAction.Create(applyProgressBar, ProgressComplete)
			.ServerStartProgress(originator.RegisterTile(), 3f, originator);
	}
}